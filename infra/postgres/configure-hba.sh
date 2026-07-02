#!/bin/bash
set -e
dock
# Add replication entry to pg_hba.conf
echo "host replication replicator 0.0.0.0/0 md5" >> "$PGDATA/pg_hba.conf"
