#!/usr/bin/env bash

# Create the database
pushd backend/database
chmod +x ./create-db.sh
./create-db.sh
popd

# Start the server (assuming that it has been compiled already)
mono ./backend/SmartHomeWeb/SmartHomeWeb/bin/Release/SmartHomeWeb.exe &
serverid=$!

sleep 2

# Post some sample data
curl -X POST -d @example-files/personData.json localhost:8088/api/persons

kill $serverid
