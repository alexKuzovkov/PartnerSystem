#!/usr/bin/env bash
set -u

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

PASS=0
FAIL=0
RUN_ID=$(date +%s)
START_SERVICES=false

if [[ "${1:-}" == "--start" ]]; then
  START_SERVICES=true
fi

pass() {
  echo -e "${GREEN}[PASS] $1${NC}"
  PASS=$((PASS + 1))
}

fail() {
  echo -e "${RED}[FAIL] $1${NC}"
  echo "       $2"
  FAIL=$((FAIL + 1))
}

section() {
  echo -e "\n${YELLOW}[$1] $2${NC}"
}

get() {
  curl -sS --max-time 10 "$1" 2>/dev/null || true
}

post_json() {
  curl -sS --max-time 10 -X POST "$1" \
    -H "Content-Type: application/json" \
    -d "$2" 2>/dev/null || true
}

check_contains() {
  local name="$1"
  local expected="$2"
  local actual="$3"

  if grep -Fqi -- "$expected" <<< "$actual"; then
    pass "$name"
  else
    fail "$name" "Expected '$expected', actual: $(echo "$actual" | head -c 300)"
  fi
}

wait_for_contains() {
  local url="$1"
  local expected="$2"
  local timeout_seconds="${3:-30}"
  local deadline=$((SECONDS + timeout_seconds))
  local response=""

  while (( SECONDS < deadline )); do
    response=$(get "$url")
    if grep -Fqi -- "$expected" <<< "$response"; then
      echo "$response"
      return 0
    fi
    sleep 1
  done

  echo "$response"
  return 1
}

wait_for_health() {
  local name="$1"
  local url="$2"
  local response

  if response=$(wait_for_contains "$url" "Healthy" 90); then
    pass "$name health check"
  else
    fail "$name health check" "Last response: $response"
  fi
}

if $START_SERVICES; then
  echo -e "${CYAN}Building and starting Docker services...${NC}"
  docker compose up -d --build || exit 1
fi

ROOT="root_$RUN_ID"
U1="u1_$RUN_ID"
U2="u2_$RUN_ID"
U3="u3_$RUN_ID"
LINEAR_EVENT="linear_$RUN_ID"
FIB_EVENT="fib_$RUN_ID"
IDEMPOTENT_EVENT="idempotent_$RUN_ID"
LOSS_EVENT="loss_$RUN_ID"

section "1/8" "Health checks"
wait_for_health "UserService" "http://localhost:5001/health"
wait_for_health "EventService" "http://localhost:5002/health"
wait_for_health "CommissionService" "http://localhost:5003/health"
wait_for_health "WalletService" "http://localhost:5004/health"

section "2/8" "Create partner hierarchy"
post_json "http://localhost:5001/api/users" "{\"externalId\":\"$ROOT\"}" >/dev/null
post_json "http://localhost:5001/api/users" "{\"externalId\":\"$U1\",\"parentExternalId\":\"$ROOT\"}" >/dev/null
post_json "http://localhost:5001/api/users" "{\"externalId\":\"$U2\",\"parentExternalId\":\"$U1\"}" >/dev/null
post_json "http://localhost:5001/api/users" "{\"externalId\":\"$U3\",\"parentExternalId\":\"$U2\"}" >/dev/null

CHAIN=$(get "http://localhost:5001/api/users/$U3/chain")
check_contains "Upward chain contains level-1 partner" "$U2" "$CHAIN"
check_contains "Upward chain contains level-2 partner" "$U1" "$CHAIN"
check_contains "Upward chain contains level-3 partner" "$ROOT" "$CHAIN"

section "3/8" "Downline projection"
DOWNLINE=$(get "http://localhost:5001/api/users/$ROOT/downline")
check_contains "Root downline contains first child" "$U1" "$DOWNLINE"
check_contains "Root downline contains second-level child" "$U2" "$DOWNLINE"
check_contains "Root downline contains third-level child" "$U3" "$DOWNLINE"

section "4/8" "Linear commission calculation and multi-partner payout"
post_json "http://localhost:5003/api/admin/schema" '{"schema":"Linear"}' >/dev/null
post_json "http://localhost:5002/api/events" "{\"eventExternalId\":\"$LINEAR_EVENT\",\"userExternalId\":\"$U3\",\"profit\":1000,\"occurredAt\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"}" >/dev/null

if COMMISSIONS=$(wait_for_contains "http://localhost:5003/api/commissions/$LINEAR_EVENT" '"amount":30' 30); then
  check_contains "Linear level 1 amount is 10" '"amount":10' "$COMMISSIONS"
  check_contains "Linear level 2 amount is 20" '"amount":20' "$COMMISSIONS"
  check_contains "Linear level 3 amount is 30" '"amount":30' "$COMMISSIONS"
  check_contains "Commission scheme is persisted as Linear" '"schemaType":"Linear"' "$COMMISSIONS"
else
  fail "Linear commissions become available" "Last response: $COMMISSIONS"
fi

# This specifically guards the former bug where EventExternalId alone was unique in PendingPayouts,
# which allowed only the first partner payout for a multi-level commission event.
if BALANCE_U2=$(wait_for_contains "http://localhost:5004/api/wallets/$U2/balance" '"balance":10' 30); then
  pass "Level-1 partner payout is deposited"
else
  fail "Level-1 partner payout is deposited" "Last response: $BALANCE_U2"
fi
if BALANCE_U1=$(wait_for_contains "http://localhost:5004/api/wallets/$U1/balance" '"balance":20' 30); then
  pass "Level-2 partner payout is deposited"
else
  fail "Level-2 partner payout is deposited" "Last response: $BALANCE_U1"
fi
if BALANCE_ROOT=$(wait_for_contains "http://localhost:5004/api/wallets/$ROOT/balance" '"balance":30' 30); then
  pass "Level-3 partner payout is deposited"
else
  fail "Level-3 partner payout is deposited" "Last response: $BALANCE_ROOT"
fi

section "5/8" "Fibonacci commission scheme"
post_json "http://localhost:5003/api/admin/schema" '{"schema":"Fibonacci"}' >/dev/null
post_json "http://localhost:5002/api/events" "{\"eventExternalId\":\"$FIB_EVENT\",\"userExternalId\":\"$U3\",\"profit\":1000,\"occurredAt\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"}" >/dev/null

if FIB_COMMISSIONS=$(wait_for_contains "http://localhost:5003/api/commissions/$FIB_EVENT" '"amount":20' 30); then
  check_contains "Fibonacci event uses Fibonacci scheme" '"schemaType":"Fibonacci"' "$FIB_COMMISSIONS"
  LEVEL_ONE_COUNT=$(grep -o '"amount":10' <<< "$FIB_COMMISSIONS" | wc -l | tr -d ' ')
  if [[ "$LEVEL_ONE_COUNT" -ge 2 ]]; then
    pass "Fibonacci levels 1 and 2 both produce amount 10"
  else
    fail "Fibonacci levels 1 and 2 both produce amount 10" "Response: $FIB_COMMISSIONS"
  fi
else
  fail "Fibonacci commissions become available" "Last response: $FIB_COMMISSIONS"
fi

section "6/8" "Historical scheme immutability"
OLD_LINEAR=$(get "http://localhost:5003/api/commissions/$LINEAR_EVENT")
check_contains "Existing Linear commissions remain Linear" '"schemaType":"Linear"' "$OLD_LINEAR"
post_json "http://localhost:5003/api/admin/schema" '{"schema":"Linear"}' >/dev/null

section "7/8" "Idempotency"
PAYLOAD="{\"eventExternalId\":\"$IDEMPOTENT_EVENT\",\"userExternalId\":\"$U3\",\"profit\":100,\"occurredAt\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"}"
for _ in 1 2 3; do
  post_json "http://localhost:5002/api/events" "$PAYLOAD" >/dev/null
 done

if IDEMPOTENT=$(wait_for_contains "http://localhost:5003/api/commissions/$IDEMPOTENT_EVENT" '"partnerExternalId"' 30); then
  COUNT=$(grep -o '"partnerExternalId"' <<< "$IDEMPOTENT" | wc -l | tr -d ' ')
  if [[ "$COUNT" -eq 3 ]]; then
    pass "Duplicate requests create exactly one commission per partner level"
  else
    fail "Duplicate requests create exactly one commission per partner level" "Expected 3 commissions, got $COUNT"
  fi
else
  fail "Idempotent event becomes available" "Last response: $IDEMPOTENT"
fi

section "8/8" "Negative profit"
post_json "http://localhost:5002/api/events" "{\"eventExternalId\":\"$LOSS_EVENT\",\"userExternalId\":\"$U3\",\"profit\":-500,\"occurredAt\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"}" >/dev/null
sleep 2
LOSS=$(get "http://localhost:5003/api/commissions/$LOSS_EVENT")
check_contains "Loss event produces no commissions" '"commissions":[]' "$LOSS"

echo -e "\n${CYAN}========================================${NC}"
echo -e "${CYAN} Test summary${NC}"
echo -e "${CYAN}========================================${NC}"
echo -e "${GREEN}Passed: $PASS${NC}"
echo -e "${RED}Failed: $FAIL${NC}"

if [[ "$FAIL" -eq 0 ]]; then
  echo -e "${GREEN}All end-to-end checks passed.${NC}"
  exit 0
fi

echo -e "${RED}Some checks failed.${NC}"
echo "Inspect logs with: docker compose logs --tail=200"
exit 1
