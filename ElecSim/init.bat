python3 main.py --mode=configure --no_house_holds=1 --output=MY_CONFIGURE.json

ECHO off
FOR /L %%A IN (1,1,1) DO python3 main.py --mode=generate --config_file=MY_CONFIGURE.json --household=%%A --from=2016-03-24T16:00 --to=2016-03-24T16:30 --output=MY_FILE%%A.csv 
FOR /L %%A IN (1,1,1) DO python3 parsecsv.py -h %%A -i MY_FILE%%A.csv -o MY_FILE%%A.json

FOR /L %%A IN (1,1,1) DO del MY_FILE%%A.csv