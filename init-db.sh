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
curl -X POST -d @example-files/person-data.json localhost:8088/api/persons
curl -X POST -d @example-files/location-data.json localhost:8088/api/locations
curl -X POST -d @example-files/sensor-data.json localhost:8088/api/sensors
curl -X POST -d @example-files/measurement-data.json localhost:8088/api/measurements
# curl -X POST -d @example-files/message-data.json localhost:8088/api/messages

kill $serverid
