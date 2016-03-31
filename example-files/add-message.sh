#!/usr/bin/env bash

jsonfield="mono ../json-utils/json-field/bin/Release/json-field.exe"
jsonadd="mono ../json-utils/json-add/bin/Release/json-add.exe"
jsonpack="mono ../json-utils/json-pack/bin/Release/json-pack.exe"
jsonfind="mono ../json-utils/json-find/bin/Release/json-find.exe"
jsonflatten="mono ../json-utils/json-flatten/bin/Release/json-flatten.exe"

curl -X GET localhost:8088/api/persons | $jsonflatten > tmp/persons.json
guid1="$(cat tmp/persons.json | $jsonfind username $1 | $jsonfield personGuid)"
guid2="$(cat tmp/persons.json | $jsonfind username $2 | $jsonfield personGuid)"
echo "{}" | $jsonadd senderId "$guid1" | $jsonadd recipientId "$guid2" | $jsonadd message "$3" | $jsonpack - > tmp/message.json
curl -X POST -d @tmp/message.json localhost:8088/api/messages
