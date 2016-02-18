import csv
import sys

f = open('we1.txt', 'rt')

reader = csv.reader(f,quoting=csv.QUOTE_NONNUMERIC)

we1trans =[[0 for j in range(9)] for i in range(1009)]
i=0
for row in reader:
    we1trans[i][0]=int(row[0])
    we1trans[i][1]=int(row[1])
    we1trans[i][2]=float(row[2])
    we1trans[i][3]=row[3]
    we1trans[i][4]=row[4]
    we1trans[i][5]=row[5]
    we1trans[i][6]=row[6]
    we1trans[i][7]=row[7]
    we1trans[i][8]=row[8]
    i=i+1
    
f.close()

print we1trans