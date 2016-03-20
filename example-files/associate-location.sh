#!/usr/bin/env bash

jsonfield="../json-utils/json-field/bin/Release/json-field.exe"
jsonadd="../json-utils/json-add/bin/Release/json-add.exe"
jsonpack="../json-utils/json-pack/bin/Release/json-pack.exe"
jsonfind="../json-utils/json-find/bin/Release/json-find.exe"
jsonflatten="../json-utils/json-flatten/bin/Release/json-flatten.exe"

curl -X GET localhost:8088/api/persons | $jsonflatten > tmp/persons.json
curl -X GET localhost:8088/api/locations | $jsonflatten > tmp/locations.json
guid="$(cat tmp/persons.json | $jsonfind username "$1" | $jsonfield personGuid)"
id="$(cat tmp/locations.json | $jsonfind name "$2" | $jsonfield id)"
echo "{}" | $jsonadd personGuid "$guid" | $jsonadd locationId "$id" | $jsonpack - > tmp/person-location.json
curl -X POST -d @tmp/person-location.json localhost:8088/api/has-location
