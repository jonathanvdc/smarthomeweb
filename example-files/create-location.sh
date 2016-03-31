#!/usr/bin/env bash

jsonfield="mono ../json-utils/json-field/bin/Release/json-field.exe"
jsonadd="mono ../json-utils/json-add/bin/Release/json-add.exe"
jsonpack="mono ../json-utils/json-pack/bin/Release/json-pack.exe"
jsonfind="mono ../json-utils/json-find/bin/Release/json-find.exe"
jsonflatten="mono ../json-utils/json-flatten/bin/Release/json-flatten.exe"

curl -X GET localhost:8088/api/persons | $jsonflatten > tmp/persons.json
guid="$(cat tmp/persons.json | $jsonfind username "$2" | $jsonfield personGuid)"
echo "{}" | $jsonadd ownerGuid "$guid" | $jsonadd name "$1" | $jsonpack - > tmp/location.json
curl -X POST -d @tmp/location.json localhost:8088/api/locations
