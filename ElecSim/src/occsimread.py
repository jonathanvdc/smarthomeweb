# Occupants simulator
import random, csv, sys, os

# weekday initital
wdini=[[0.843713278495887,0.797619047619048,0.754002911208151,0.734090909090909,0.680000000000000,0.615384615384615],
       [0.156286721504113,0.144841269841270,0.189228529839884,0.181818181818182,0.176000000000000,0.205128205128205],
       [0.000000000000000,0.057539682539683,0.043668122270742,0.061363636363636,0.120000000000000,0.102564102564103],
       [0.000000000000000,0.000000000000000,0.013100436681223,0.020454545454546,0.008000000000000,0.051282051282051],
       [0.000000000000000,0.000000000000000,0.000000000000000,0.002272727272727,0.008000000000000,0.000000000000000],
       [0.000000000000000,0.000000000000000,0.000000000000000,0.000000000000000,0.008000000000000,0.025641025641026],
       [0.000000000000000,0.000000000000000,0.000000000000000,0.000000000000000,0.000000000000000,0.000000000000000]]

# weekend initital
weini=[[0.828924162257496,0.767661691542289,0.676342525399129,0.675675675675676,0.546875000000000,0.666666666666667],
       [0.171075837742504,0.161194029850746,0.210449927431060,0.207207207207207,0.250000000000000,0.138888888888889],
       [0.000000000000000,0.071144278606965,0.094339622641509,0.092342342342342,0.109375000000000,0.111111111111111],
       [0.000000000000000,0.000000000000000,0.018867924528302,0.015765765765766,0.078125000000000,0.027777777777778],
       [0.000000000000000,0.000000000000000,0.000000000000000,0.009009009009009,0.000000000000000,0.055555555555556],
       [0.000000000000000,0.000000000000000,0.000000000000000,0.000000000000000,0.015625000000000,0.000000000000000],
       [0.000000000000000,0.000000000000000,0.000000000000000,0.000000000000000,0.000000000000000,0.000000000000000]]


def OccupanceSim(iResidents,bWeekend):
    random.seed()
    fRand = random.random()
    #print fRand, bWeekend
    fCumulativeP = 0
    iCurrentState = 0
    while iCurrentState <=6 and fRand > fCumulativeP:
        if bWeekend==False:
            #print "I am true"
            fCumulativeP = fCumulativeP + wdini[iCurrentState][iResidents]
            #print iCurrentState, occprob.wdini[iCurrentState][iResidents]
            iCurrentState=iCurrentState+1
        elif bWeekend==True:
            #print "I am false"
            #print iCurrentState, occprob.weini[iCurrentState][iResidents]
            fCumulativeP = fCumulativeP + weini[iCurrentState][iResidents]
            #print fCumulative 
            iCurrentState=iCurrentState+1
    
    iCurrentState=iCurrentState-1

    datadir = os.path.dirname(__file__) + '/../data'

    if bWeekend==False:
      #print "I am true"
        if iResidents==1: transprob= datadir + '/wd1.csv'
        elif iResidents==2: transprob= datadir + '/wd2.csv'
        elif iResidents==3: transprob= datadir + '/wd3.csv'
        elif iResidents==4: transprob= datadir + '/wd4.csv'
        elif iResidents==5: transprob= datadir + '/wd5.csv'
    elif bWeekend==True:
        if iResidents==1: transprob=  datadir + '/we1.csv'
        elif iResidents==2: transprob= datadir + '/we2.csv'
        elif iResidents==3: transprob= datadir + '/we3.csv'
        elif iResidents==4: transprob= datadir + '/we4.csv'
        elif iResidents==5: transprob= datadir + '/we5.csv'

    trans =[[0 for j in range(9)] for i in range(1009)]

    f = open(transprob, 'rt')

    reader = csv.reader(f,quoting=csv.QUOTE_NONNUMERIC)

    i=0

    for row in reader:
        #       print row
       trans[i][0]=int(row[0])
       trans[i][1]=int(row[1])
       trans[i][2]=float(row[2])
       trans[i][3]=float(row[3])
       trans[i][4]=float(row[4])
       trans[i][5]=float(row[5])
       trans[i][6]=float(row[6])
       trans[i][7]=float(row[7])
       trans[i][8]=float(row[8])
       #print trans[i]
       i=i+1

    f.close()



#    print "Initial:", iCurrentState
    OccMat=[]
    for iTimeStep in range(1,145):
        fRand = random.random()
        fCumulativeP = 0
        iRow = ((iTimeStep - 1) * 7) + iCurrentState 
        #print iTimeStep, iRow, fRand
        for i in range(0,7):
            fCumulativeP = fCumulativeP + trans[iRow][i+2]
            if fRand < fCumulativeP:
                    #print "test of random"
               iCurrentState = i
                    #print "Next State:", iCurrentState
               break
        OccMat.insert(iTimeStep,iCurrentState)
    del trans

    return OccMat
