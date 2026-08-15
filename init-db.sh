#!/bin/bash
set -e
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
    CREATE DATABASE users_db;
    CREATE DATABASE events_db;
    CREATE DATABASE commissions_db;
    CREATE DATABASE wallets_db;
EOSQL