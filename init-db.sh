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
pushd example-files

curl -X POST -d @person-data.json localhost:8088/api/persons
curl -X POST -d @location-data.json localhost:8088/api/locations
curl -X POST -d @sensor-data.json localhost:8088/api/sensors
curl -X POST -d @measurement-data.json localhost:8088/api/measurements

chmod +x ./add-message.sh
./add-message.sh message1.json 0 1
popd

kill $serverid
