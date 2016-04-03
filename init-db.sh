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

mkdir -p tmp

curl -X POST -d @person-data.json localhost:8088/api/persons

./create-location.sh "De G.10" bgoethals
./create-location.sh "M.H. 143" bennyvh
./create-location.sh "V. 009" hans

curl -X POST -d @sensor-data.json localhost:8088/api/sensors
curl -X POST -d @measurement-data.json localhost:8088/api/measurements


chmod +x ./add-message.sh
./add-message.sh bgoethals jonsneyers "Hallo"
./add-message.sh jonsneyers bgoethals "Goede morgen"

chmod +x ./associate-location.sh
./associate-location.sh bgoethals "De G.10"
popd

pushd ElecSim
./run.bash
curl -X POST -d @MY_FILE1.json localhost:8088/api/measurements
popd

kill $serverid
