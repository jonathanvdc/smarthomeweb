# Create the database
pushd backend/database
./create-db.ps1
popd
pushd ElecSim
cmd.exe /c "init.bat"
popd

$app = Start-Process ./backend/SmartHomeWeb/SmartHomeWeb/bin/Release/SmartHomeWeb.exe -passthru

Start-Sleep -s 2

$body = Get-Content ./example-files/person-data.json
Invoke-WebRequest -Uri http://localhost:8088/api/persons -Method POST -Body $body

$body = Get-Content ./example-files/location-data.json
Invoke-WebRequest -Uri http://localhost:8088/api/locations -Method POST -Body $body

#$body = Get-Content ./example-files/sensor-data.json
#Invoke-WebRequest -Uri http://localhost:8088/api/sensors -Method POST -Body $body

# $body = Get-Content ./example-files/measurement-data.json
# Invoke-WebRequest -Uri http://localhost:8088/api/measurements -Method POST -Body $body

$body = Get-Content ./ElecSim/MY_FILE1.json
Invoke-WebRequest -Uri http://localhost:8088/api/measurements -Method POST -Body $body

# $body = Get-Content ./example-files/message-data.json
# Invoke-WebRequest -Uri http://localhost:8088/api/messages -Method POST -Body $body

Stop-Process $app
