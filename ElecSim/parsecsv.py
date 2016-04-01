import optparse
import csv
import sys
import getopt
from datetime import datetime
import sqlite3

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
	finalOutput = ["[\n"]
	fieldnames = []
	sensorids = []
	for line in reader:
		if (counter == 0):
			fieldnames = line
			conn = sqlite3.connect('../backend/database/smarthomeweb.db')
			c = conn.cursor()
			sensorcounter = 2
			for value in fieldnames:
				if (value != ""):
					if value != "Timestamp" and value != "Total":
						c.execute("INSERT INTO Sensor(locationid, title, description) VALUES ({},\"{}\",\"{}\");".format(household, value, sensorcounter))
						sensorcounter += 1
			conn.commit()
			# Fetch all sensors that have been installed in the current
			# household, from database.
			c.execute("SELECT id, description FROM Sensor WHERE Locationid == " + household + ";")
			sensorids = c.fetchall()
			# Sort sensors by key
			sensorids.sort(key = lambda xs: int(xs[1]))
			conn.close()
		else:
			counter2 = 0
			unixtime = 0
			for data in line:
				if (data != ""):
					if (counter2 == 0):
						dt = datetime(int(data[:4]), int(data[5:7]), int(data[8:10]), int(data[11:13]), int(data[14:16]), int(data[17:19]))
						unixtime = dt
					else:
						if (fieldnames[counter2] != "Total"):
							finalOutput.append("\t{")
							finalOutput.append("\n\t\t\"sensorId\" : {0},\n".format(sensorids[counter2-2][0]))
							finalOutput.append("\t\t\"timestamp\" : \"{0}\", \n".format(unixtime))
							finalOutput.append("\t\t\"measurement\" : {0},\n".format(data))
							finalOutput.append("\t\t\"notes\" : \"\"\n\t},\n")
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
