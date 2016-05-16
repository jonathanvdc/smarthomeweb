#!/usr/bin/env bash

python init-db.py 720
cp backend/database/smarthomeweb.db backend/database/smarthomeweb-720.db
python compact.py measurements $(date -I --date "14 days ago") $(date -I --date "7 days ago")
python compact.py hour-average $(date -I --date "2 months ago") $(date -I --date "14 days ago")
python compact.py day-average $(date -I --date "2 years ago") $(date -I --date "2 months ago")
python vacuum.py

python post-measurements.py $(date -I --date "3 days") 2
