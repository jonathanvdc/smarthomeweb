import optparse
import csv
import sys
import getopt
from datetime import datetime

def main(argv):
	usage="parsecsv.py -h <household number> -i <input csv file> -o <output json file>"
	opts, args = getopt.getopt(argv, "h:i:o:")
	for opt, arg in opts:
		if (opt == '-h'):
			household=arg
		elif (opt == '-i'):
			inputfile=arg
		elif (opt == '-o'):
			outputfile=arg
		else:
			raise Exception("Incorrect args")
	
	csvfile = open(inputfile, 'r')
	jsonfile = open(outputfile, 'w')
	
	reader = csv.reader(csvfile, delimiter=';')
	counter = 0
	finalOutput = ["["]
	fieldnames = []
	for line in reader:
		if (counter == 0):
			fieldnames = line
		else:
			
			counter2 = 0
			unixtime = 0
			for data in line:
				if (data != ""):
					if (counter2 == 0):
						dt = datetime(int(data[:4]), int(data[5:7]), int(data[8:10]), int(data[11:13]), int(data[14:16]), int(data[17:19]))
						unixtime = dt.timestamp()
					else:
						if (fieldnames[counter2] != "Total"):
							if (finalOutput[-1] != '[' and finalOutput[-1] != ', '):
								finalOutput.append(', ')
							finalOutput.append("""{}{}, {}, {}, {}{}""".format('{', fieldnames[counter2], household, unixtime, data, '}'))
				counter2 += 1
		counter += 1
	finalOutput.append("]")
	csvfile.close()
	jsonfile.write("".join(finalOutput))
	jsonfile.close()
	
	
	
#	
#
#	with open(inputfile, "r") as f:
#		reader=csv.reader(f, delimiter=';')
#		ctr = 0
#		output = open("".join((inputfile, ".sql")), 'w', newline='\n')
#		for i, line in enumerate(reader):
#			if (ctr == 0):
#				first = line #contains the names of our sensors, need to keep this to ensure our data is linked correctly in the DB
#			else:
#				ctr2 = 0
#				final = [] #list of strings for final inserts
#				for data in line:
#					if (data != ""):
#						if (ctr2 == 0):
#							dt = datetime(int(data[:4]), int(data[5:7]), int(data[8:10]), int(data[11:13]), int(data[14:16]), int(data[17:19]))
#							unixtime = dt.timestamp()
#						else:
#							if (first[ctr2] != "Total"): #Exclude "total" - We compute this as needed, it's not a sensor datapoint
#								final.append("""
#insert into Measurement(time, sensorId, usage) values (
#\tselect Sensor.id, {}, {}\n\tfrom Sensor\n\twhere Sensor.name='{}' and Sensor.locationId={}
#);
#""".format(unixtime, data, first[ctr2], household))
#					ctr2 += 1
#					
#				
#				output.write(''.join(final))
#				
#			ctr += 1
#		output.close()
#	
	
if __name__ == "__main__":
	main(sys.argv[1:])
