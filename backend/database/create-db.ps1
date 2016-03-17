$db = "smarthomeweb.db"
If (Test-Path $db) { Remove-Item $db }
Get-Content smarthomeweb.sql | cmd /c sqlite3 $db
