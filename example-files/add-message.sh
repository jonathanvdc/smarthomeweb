#!/usr/bin/env bash

jsonfield="mono ../json-utils/json-field/bin/Release/json-field.exe"
jsonadd="mono ../json-utils/json-add/bin/Release/json-add.exe"
jsonpack="mono ../json-utils/json-pack/bin/Release/json-pack.exe"

curl -X GET localhost:8088/api/persons > tmp/persons.json
guid1="$($jsonfield --index=$2 personGuid < tmp/persons.json)"
guid2="$($jsonfield --index=$3 personGuid < tmp/persons.json)"
cat $1 | $jsonadd senderId "$guid1" | $jsonadd recipientId "$guid2" | $jsonpack - > tmp/message.json
curl -X POST -d @tmp/message.json localhost:8088/api/messages
