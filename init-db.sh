#!/usr/bin/env bash

# Create the database
pushd backend/database
chmod +x ./create-db.sh
./create-db.sh
popd

# Start the server (assuming that it has been compiled already)
mono ./backend/SmartHomeWeb/SmartHomeWeb/bin/Debug/SmartHomeWeb.exe &
serverid=$!

sleep 1

# Post some same data
curl -X POST -d @example-files/personData.json localhost:8088/api/persons

kill $serverid
