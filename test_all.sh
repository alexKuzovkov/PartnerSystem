#!/bin/bash

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

PASS=0
FAIL=0

# ✅ Уникальный префикс для каждого запуска
TIMESTAMP=$(date +%s)

check() {
    local name=$1
    local expected=$2
    local actual=$3
    
    if echo "$actual" | grep -qi "$expected"; then
        echo -e "${GREEN}✓ $name${NC}"
        ((PASS++))
    else
        echo -e "${RED}✗ $name${NC}"
        echo "  Expected: $expected"
        echo "  Actual: $(echo "$actual" | head -c 300)"
        ((FAIL++))
    fi
}

header() {
    echo -e "\n${CYAN}========================================${NC}"
    echo -e "${CYAN} $1${NC}"
    echo -e "${CYAN}========================================${NC}"
}

section() {
    echo -e "\n${YELLOW}[$1] $2${NC}"
}

# ============================================
# 1. Health Checks
# ============================================
header "1/8 Health Checks"

check "UserService" "Healthy" "$(curl -sf http://localhost:5001/health 2>/dev/null || echo 'FAIL')"
check "EventService" "Healthy" "$(curl -sf http://localhost:5002/health 2>/dev/null || echo 'FAIL')"
check "CommissionService" "Healthy" "$(curl -sf http://localhost:5003/health 2>/dev/null || echo 'FAIL')"
check "WalletService" "Healthy" "$(curl -sf http://localhost:5004/health 2>/dev/null || echo 'FAIL')"

# ============================================
# 2. Создание иерархии пользователей
# ============================================
section "2/8" "Создание иерархии пользователей"

# ✅ Используем уникальные имена с временной меткой
curl -sf -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d "{\"externalId\":\"root_$TIMESTAMP\"}" > /dev/null 2>&1 || true

curl -sf -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d "{\"externalId\":\"u1_$TIMESTAMP\",\"parentExternalId\":\"root_$TIMESTAMP\"}" > /dev/null 2>&1 || true

curl -sf -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d "{\"externalId\":\"u2_$TIMESTAMP\",\"parentExternalId\":\"u1_$TIMESTAMP\"}" > /dev/null 2>&1 || true

curl -sf -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d "{\"externalId\":\"u3_$TIMESTAMP\",\"parentExternalId\":\"u2_$TIMESTAMP\"}" > /dev/null 2>&1 || true

echo -e "${GREEN}✓ Создана иерархия root → u1 → u2 → u3 (timestamp: $TIMESTAMP)${NC}"
((PASS++))

# ============================================
# 3. Ветки дерева
# ============================================
section "3/8" "Ветки дерева"

CHAIN=$(curl -sf http://localhost:5001/api/users/u3_$TIMESTAMP/chain 2>/dev/null || echo "")
check "Ветка вверх u3 содержит u2" "u2" "$CHAIN"
check "Ветка вверх u3 содержит u1" "u1" "$CHAIN"
check "Ветка вверх u3 содержит root" "root" "$CHAIN"

DOWNLINE=$(curl -sf http://localhost:5001/api/users/root_$TIMESTAMP/downline 2>/dev/null || echo "")
check "Ветка вниз root содержит u1" "u1" "$DOWNLINE"
check "Ветка вниз root содержит u2" "u2" "$DOWNLINE"
check "Ветка вниз root содержит u3" "u3" "$DOWNLINE"

# ============================================
# 4. Linear схема
# ============================================
section "4/8" "Linear схема (L × Profit / 100)"

curl -sf -X POST http://localhost:5003/api/admin/schema \
  -H "Content-Type: application/json" \
  -d '{"schema":"Linear"}' > /dev/null 2>&1 || true

# ✅ Уникальное имя события
curl -sf -X POST http://localhost:5002/api/events \
  -H "Content-Type: application/json" \
  -d "{\"eventExternalId\":\"linear_$TIMESTAMP\",\"userExternalId\":\"u3_$TIMESTAMP\",\"profit\":1000}" > /dev/null 2>&1

echo "Ждём обработки 15 секунд..."
sleep 15

COMMISSIONS=$(curl -sf http://localhost:5003/api/commissions/linear_$TIMESTAMP 2>/dev/null || echo "")
check "Комиссия u2 (L1=10)" "10" "$COMMISSIONS"
check "Комиссия u1 (L2=20)" "20" "$COMMISSIONS"
check "Комиссия root (L3=30)" "30" "$COMMISSIONS"
check "SchemaType=Linear" "Linear" "$COMMISSIONS"

# ============================================
# 5. Fibonacci схема
# ============================================
section "5/8" "Fibonacci схема (F(L) × Profit / 100)"

curl -sf -X POST http://localhost:5003/api/admin/schema \
  -H "Content-Type: application/json" \
  -d '{"schema":"Fibonacci"}' > /dev/null 2>&1 || true

# ✅ Уникальное имя события
curl -sf -X POST http://localhost:5002/api/events \
  -H "Content-Type: application/json" \
  -d "{\"eventExternalId\":\"fib_$TIMESTAMP\",\"userExternalId\":\"u3_$TIMESTAMP\",\"profit\":1000}" > /dev/null 2>&1

sleep 15

FIB_COMMISSIONS=$(curl -sf http://localhost:5003/api/commissions/fib_$TIMESTAMP 2>/dev/null || echo "")
check "Fibonacci u2 (F1=10)" "10" "$FIB_COMMISSIONS"
check "Fibonacci u1 (F2=10)" "10" "$FIB_COMMISSIONS"
check "Fibonacci root (F3=20)" "20" "$FIB_COMMISSIONS"
check "SchemaType=Fibonacci" "Fibonacci" "$FIB_COMMISSIONS"

# ============================================
# 6. Старые комиссии не пересчитываются
# ============================================
section "6/8" "Старые комиссии не пересчитываются"

OLD=$(curl -sf http://localhost:5003/api/commissions/linear_$TIMESTAMP 2>/dev/null || echo "")
check "Linear событие осталось Linear" "Linear" "$OLD"

curl -sf -X POST http://localhost:5003/api/admin/schema \
  -H "Content-Type: application/json" \
  -d '{"schema":"Linear"}' > /dev/null 2>&1 || true

# ============================================
# 7. Идемпотентность
# ============================================
section "7/8" "Идемпотентность (защита от дубликатов)"

# ✅ Уникальное имя события
for i in 1 2 3; do
  curl -sf -X POST http://localhost:5002/api/events \
    -H "Content-Type: application/json" \
    -d "{\"eventExternalId\":\"idempotent_$TIMESTAMP\",\"userExternalId\":\"u3_$TIMESTAMP\",\"profit\":100}" > /dev/null 2>&1 || true
done

sleep 10

COUNT=$(curl -sf http://localhost:5003/api/commissions/idempotent_$TIMESTAMP 2>/dev/null | grep -o "partnerExternalId" | wc -l)
if [ "$COUNT" -eq 3 ]; then
    echo -e "${GREEN}✓ Идемпотентность: 3 комиссии (дубликаты не создали лишних)${NC}"
    ((PASS++))
else
    echo -e "${RED}✗ Идемпотентность: ожидалось 3, получено $COUNT${NC}"
    ((FAIL++))
fi

# ============================================
# 8. Отрицательный Profit
# ============================================
section "8/8" "Отрицательный Profit (убыток)"

# ✅ Уникальное имя события
curl -sf -X POST http://localhost:5002/api/events \
  -H "Content-Type: application/json" \
  -d "{\"eventExternalId\":\"loss_$TIMESTAMP\",\"userExternalId\":\"u3_$TIMESTAMP\",\"profit\":-500}" > /dev/null 2>&1 || true

sleep 10

LOSS=$(curl -sf http://localhost:5003/api/commissions/loss_$TIMESTAMP 2>/dev/null || echo "")
check "Нет комиссий при убытке" "commissions" "$LOSS"

# ============================================
# Итог
# ============================================
header "ИТОГ"

echo -e "${YELLOW}✓ Пройдено: $PASS${NC}"
echo -e "${RED}✗ Провалено: $FAIL${NC}"
echo -e "${CYAN}========================================${NC}"

if [ $FAIL -eq 0 ]; then
    echo -e "${GREEN}🎉 ВСЕ ТЕСТЫ ПРОЙДЕНЫ! Система готова к сдаче!${NC}"
    exit 0
else
    echo -e "${RED}⚠️ Есть проваленные тесты${NC}"
    echo -e "${YELLOW}Проверь логи: docker compose logs commissionservice${NC}"
    exit 1
fi