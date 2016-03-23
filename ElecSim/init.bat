python main.py --mode=configure --no_house_holds=10 --output=MY_CONFIGURE.json

ECHO off
FOR /L %%A IN (1,1,10) DO main.py --mode=generate --config_file=MY_CONFIGURE.json --household=%%A --output=MY_FILE%%A.csv 
FOR /L %%A IN (1,1,10) DO parsecsv.py -h %%A -i MY_FILE%%A.csv -o MY_FILE%%A.json

FOR /L %%A IN (1,1,10) DO del MY_FILE%%A.csv