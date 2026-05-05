#!/bin/bash
set -e

echo "Waiting for SQL Server to be ready..."

for i in {1..60}; do
  /opt/mssql-tools18/bin/sqlcmd \
    -S localhost \
    -U sa \
    -P "$MSSQL_SA_PASSWORD" \
    -C \
    -Q "SELECT 1" >/dev/null 2>&1

  if [ $? -eq 0 ]; then
    echo "SQL Server is ready"
    break
  fi

  echo "Waiting..."
  sleep 3
done

echo "Running init.sql..."
/opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P "$MSSQL_SA_PASSWORD" \
  -C \
  -i /docker-entrypoint-initdb.d/init.sql

echo "Database initialized."

# keep container alive
tail -f /dev/null