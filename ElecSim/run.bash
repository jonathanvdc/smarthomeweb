#!/bin/bash
python3 main.py --mode=configure --no_house_holds=1 --output=MY_CONFIGURE.json
for i in `seq 1 1`; do
	python3 main.py --mode=generate --config_file=MY_CONFIGURE.json --household=$i --from=2016-03-24T16:00 --to=2016-03-24T16:30 --output=MY_FILE$i.csv
done
for i in `seq 1 1`; do
	python3 parsecsv.py -h $i -i MY_FILE$i.csv -o MY_FILE$i.json
done

# for i in `seq 1 1`; do
# 	rm MY_FILE$i.csv
# done

echo Generated JSON files!
