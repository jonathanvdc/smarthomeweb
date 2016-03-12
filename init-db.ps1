# Create the database
pushd backend/database
cmd /R create-db.bat
popd

$app = Start-Process ./backend/SmartHomeWeb/SmartHomeWeb/bin/Release/SmartHomeWeb.exe -passthru

Start-Sleep -s 3
$body = Get-Content ./example-files/personData.json
Invoke-WebRequest -Uri http://localhost:8088/api/persons -Method POST -Body $body

Stop-Process $app
