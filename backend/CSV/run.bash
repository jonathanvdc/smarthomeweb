#!/bin/bash
python3 main.py --mode=configure --no_house_holds=10 --output=MY_CONFIGURE.json
for i in `seq 1 10`; do
	python3 main.py --mode=generate --config_file=MY_CONFIGURE.json --household=$i --output=MY_FILE$i.csv
done
for i in `seq 1 10`; do
	python3 parsecsv.py -h $i -i MY_FILE$i.csv
done

echo Generated SQL files!
