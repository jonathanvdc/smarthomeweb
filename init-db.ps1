# Create the database
pushd backend/database
cmd /R create-db.bat
popd

$app = Start-Process ./backend/SmartHomeWeb/SmartHomeWeb/bin/Release/SmartHomeWeb.exe -passthru

Start-Sleep -s 2
$body = Get-Content ./example-files/person-data.json
Invoke-WebRequest -Uri http://localhost:8088/api/persons -Method POST -Body $body

Stop-Process $app
