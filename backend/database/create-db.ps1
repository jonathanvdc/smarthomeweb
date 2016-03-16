$db = "smarthomeweb.db"
If (Test-Path $db) { Remove-Item $db }
Get-Content smarthomeweb.sql | sqlite3 $db
