import optparse
import csv
import sys
import getopt
from datetime import datetime

def main(argv):
	usage="parsecsv.py -h <household number> -i <input csv file>"
	opts, args = getopt.getopt(argv, "h:i:")
	for opt, arg in opts:
		if (opt == '-h'):
			household=arg
		elif (opt == '-i'):
			inputfile=arg
		else:
			raise Exception("Incorrect args")
	
	with open(inputfile, "r") as f:
		reader=csv.reader(f, delimiter=';')
		ctr = 0
		output = open("".join((inputfile, ".sql")), 'w', newline='\n')
		for i, line in enumerate(reader):
			if (ctr == 0):
				first = line #contains the names of our sensors, need to keep this to ensure our data is linked correctly in the DB
			else:
				ctr2 = 0
				final = [] #list of strings for final inserts
				for data in line:
					if (data != ""):
						if (ctr2 == 0):
							dt = datetime(int(data[:4]), int(data[5:7]), int(data[8:10]), int(data[11:13]), int(data[14:16]), int(data[17:19]))
							unixtime = dt.timestamp()
						else:
							if (first[ctr2] != "Total"): #Exclude "total" - We compute this as needed, it's not a sensor datapoint
								final.append("""
insert into Data(time, sensorId, usage) values (
\tselect {}, Sensor.id, {}\n\tfrom Sensor\n\twhere Sensor.name='{}' and Sensor.locationId={}
);
""".format(unixtime, data, first[ctr2], household))
					ctr2 += 1
					
				
				output.write(''.join(final))
				
			ctr += 1
		output.close()
	
	
if __name__ == "__main__":
	main(sys.argv[1:])
