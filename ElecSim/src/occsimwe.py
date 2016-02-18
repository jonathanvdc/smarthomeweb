# Occupants simulator

import random, occprob1
    
def OccupanceSim(iResidents,bWeekend):
    random.seed()
    fRand = random.random()
    #print fRand, bWeekend
    fCumulativeP = 0
    iCurrentState = 0
    while iCurrentState <=6 and fRand > fCumulativeP:
        if bWeekend==False:
            #print "I am true"
            fCumulativeP = fCumulativeP + occprob.wdini[iCurrentState][iResidents]
            #print iCurrentState, occprob.wdini[iCurrentState][iResidents]
            iCurrentState=iCurrentState
        elif bWeekend==True:
            #print "I am false"
            #print iCurrentState, occprob.weini[iCurrentState][iResidents]
            fCumulativeP = fCumulativeP + occprob.weini[iCurrentState][iResidents]
            #print fCumulative 
            iCurrentState=iCurrentState

    iCurrentState=iCurrentState
    
    #    print "Initial:", iCurrentState
    OccMat=[]
    for iTimeStep in range(1,145):
        fRand = random.random()
        fCumulativeP = 0
        iRow = ((iTimeStep - 1) * 7) + iCurrentState 
        #print iTimeStep, iRow, fRand
        for i in range(0,7):
              #            print iRow, i+2, wd1trans[iRow][i+2]
              if bWeekend==False:
                    #print "I am true"
                    if iResidents==1: fCumulativeP = fCumulativeP + occprob.wd1trans[iRow][i+2]
                    elif iResidents==2: fCumulativeP = fCumulativeP + occprob.wd2trans[iRow][i+2]
                    elif iResidents==3: fCumulativeP = fCumulativeP + occprob.wd3trans[iRow][i+2]
                    elif iResidents==4: fCumulativeP = fCumulativeP + occprob.wd4trans[iRow][i+2]
                    elif iResidents==5: 
                        fCumulativeP = fCumulativeP + occprob.wd5trans[iRow][i+2]
              #         print fCumulativeP, iRow, i+2, occprob.wd5trans[iRow][0], occprob.wd5trans[iRow][1], occprob.wd5trans[iRow][i+2]
              elif bWeekend==True:
                    # print "I am false"
                    if iResidents==1: fCumulativeP = fCumulativeP + occprob.we1trans[iRow][i+2]
                    elif iResidents==2: fCumulativeP = fCumulativeP + occprob.we2trans[iRow][i+2]
                    elif iResidents==3: fCumulativeP = fCumulativeP + occprob.we3trans[iRow][i+2]
                    elif iResidents==4: fCumulativeP = fCumulativeP + occprob.we4trans[iRow][i+2]
                    elif iResidents==5: 
                        fCumulativeP = fCumulativeP + occprob.we5trans[iRow][i+2]
              #print fCumulativeP, iRow, i+2, occprob.we5trans[iRow][0], occprob.we5trans[iRow][1], occprob.we5trans[iRow][i+2], fCumulativeP
              if fRand < fCumulativeP:
                    #print "test of random"
                    iCurrentState = i
                    #print "Next State:", iCurrentState
                    break
        OccMat.insert(iTimeStep,iCurrentState)
    #print len(OccMat)
    return OccMat
