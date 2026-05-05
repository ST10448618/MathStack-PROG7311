#!/bin/bash
set -e

/opt/mssql/bin/sqlservr &
sql_pid=$!

echo "Waiting for SQL Server..."

for i in {1..60}; do
  /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" \
    -C -Q "SELECT 1" >/dev/null 2>&1

  if [ $? -eq 0 ]; then
    echo "SQL Server is ready"
    break
  fi

  echo "Waiting for database..."
  sleep 2
done

echo "Running init.sql..."

/opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" \
  -C -i /docker-entrypoint-initdb.d/init.sql

wait $sql_pid