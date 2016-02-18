import appsimfun, occsim, random, bulbdat, irridiancedat
from math import log

mean=60
std=10
# set max number of residents, month, weekday or weekend
iResidents=5  # range 1-5
iMonth=9
bWeekend = False


ResultofOccupancySim=occsim.OccupanceSim(iResidents,bWeekend)

#def RunLightingSimulation():
random.seed()
#' Determine the irradiance threshold of this house
iIrradianceThreshold = appsimfun.GetMonteCarloNormalDistGuess(mean,std)
#Range("light_sim_data!D4").Value = iIrradianceThreshold
#
#' Clear the target area
#Range("light_sim_data!E9:BZ1500").Clear
# the output should be written out!

#' Choose a random house from the list of 100 provided in the bulbs sheet
iRandomHouse = int((100*random.random())+1)
#Range("light_sim_data!D5").Value = iRandomHouse
#
#' Declare an array to store the lighting unit configuration
#' vBulbArray            (1,1)   Example dwelling number
#'                       (1,2)   Number of lighting units
#'                       (1,3..)   Lighting unit ratings
#Dim vBulbArray As Variant
#
#
#' Get the bulb data
#vBulbArray = Range("bulbs!A" + CStr(iRandomHouse + 10) + ":BI" + CStr(iRandomHouse + 10))
#
#' Get the number of bulbs
iNumBulbs = bulbdat.bulbs[iRandomHouse-1][1]
#Range("light_sim_data!D3").Value = iNumBulbs
#
#' Declare an array to store the simulation data
#' vSimulation array     (1,n)       Bulb number header
#'                       (2,n)       Rating
#'                       (3,n)       Relative use
#'                       (4-1443,n)  Demand
#Dim vSimulationArray(1 To 1443, 1 To 60) As Variant
LSimulationArray =[[0 for j in range(60)] for i in range(1444)]

OccLights=[ 0.000, 1.000, 1.52814569536424, 1.69370860927152, 1.98344370860927, 2.09437086092715]
CummDur= [0.111111111, 0.222222222, 0.333333333, 0.444444444, 0.555555556, 0.666666667, 0.777777778, 0.888888889, 1]
LowDur= [1,2,3,5,9,17,28,50,92]
UppDur=[1,2,4,8,16,27,49,91,259]

#
#' Load the occupancy array
#Dim vOccupancyArray As Variant
#vOccupancyArray = Range("occ_sim_data!C11:C154")
#ResultofOccupancySim
#
#' Load the irradiance array
#Dim vIrradianceArray As Variant
#vIrradianceArray = Range("irradiance!C12:C1451")

#' Get the calibration scalar
fCalibrationScalar = 0.00815368639667705
#Range("light_config!F24").Value

#ocs' For each bulb
for i in range(1,iNumBulbs+1):
    #        ' Get the bulb rating
    iRating = bulbdat.bulbs[iRandomHouse-1][i + 1]
#print iRandomHouse, i, iNumBulbs, iRating
#    
#    ' Store the bulb number
    LSimulationArray[0][i] = i
#    
#    ' Store the rating
    LSimulationArray[1][i] = iRating
#    print LSimulationArray
#            
#    ' Assign a random bulb use weighting to this bulb
#    ' Note that the calibration scalar is multiplied here to save processing time later
    fCalibratedRelativeUseWeighting = -fCalibrationScalar * log(random.random())
    LSimulationArray[2][i]= fCalibratedRelativeUseWeighting
    #    print i, fCalibratedRelativeUseWeighting
#    
#    ' Calculate the bulb usage at each minute of the day
    iTime = 1
    while (iTime <= 1440):
#        #            ' Is this bulb switched on to start with?
#        # This concept is not implemented in this example.
#        #' The simplified assumption is that all bulbs are off to start with.
#        #
#        #' Get the irradiance for this minute
        iIrradiance = irridiancedat.irrdat[iTime-1][iMonth]
        #        print i, iTime, iIrradiance
#iTime=iTime+1
#        #
#        #' Get the number of current active occupants for this minute
#        #' Convert from 10 minute to 1 minute resolution
#        #iActiveOccupants = vOccupancyArray(((iTime - 1) \ 10) + 1, 1)
        iTenMinuteCount = int(((iTime - 1) // 10))+1
#        #print iTenMinuteCount, 10 + iTenMinuteCount
#        #
#        #    ' Get the number of current active occupants for this minute
#        #    ' Convert from 10 minute to 1 minute resolution
#        #            print 11 + ((iMinute - 1) // 10)
        iActiveOccupants = ResultofOccupancySim[iTenMinuteCount-1]
#        #' Determine if the bulb switch-on condition is passed
#        #' ie. Insuffient irradiance and at least one active occupant
#        #' There is a 5% chance of switch on event if the irradiance is above the threshold
#        #Dim bLowIrradiance As Boolean
        if (iIrradiance < iIrradianceThreshold) or (random.random() < 0.05):
          bLowIrradiance=True
        else:
          bLowIrradiance=False
#        #bLowIrradiance = ((iIrradiance < iIrradianceThreshold) Or (Rnd() < 0.05))
#        #
#        #' Get the effective occupancy for this number of active occupants to allow for sharing

        fEffectiveOccupancy = OccLights[iActiveOccupants]#
#        print i, iTime, iActiveOccupants, fEffectiveOccupancy
#        iTime=iTime+1
#        ' Check the probability of a switch on at this time
        if (bLowIrradiance and (random.random() < (fEffectiveOccupancy * fCalibratedRelativeUseWeighting))): #Then
#            #
#            #    ' This is a switch on event
#            #
#            #' Determine how long this bulb is on for
            r1 = random.random()
            cml = 0
            for j in range(1,10):
#                #' Get the cumulative probability of this duration
                cml = CummDur[j]#
#                #' Check to see if this is the type of light
#                print r1, cml
                if r1 < cml:
#                    #' Get the durations
#                    iLowerDuration = Range("light_config!C" + CStr(54 + j)).Value
#                    iUpperDuration = Range("light_config!D" + CStr(54 + j)).Value
#                    #
#                    #' Get another random number
                     r2 = random.random()
#                    #
#                    #' Guess a duration in this range
                     iLightDuration = int((r2 * (UppDur[j] - LowDur[j])) + LowDur[j])
                     break
#
            for j in range(1,iLightDuration):
                 if iTime > 1440: break
#            
#                #' Get the number of current active occupants for this minute
                 iActiveOccupants = ResultofOccupancySim[int(((iTime - 1)// 10) + 1)]
#                
#                #' If there are no active occupants, turn off the light
                 if iActiveOccupants == 0: break
#                
#                #' Store the demand
#print i, 2+iTime
                 LSimulationArray[2 + iTime][i] = iRating
                 #                 print iTime, "on"
#                    #' Increment the time
                 iTime = iTime + 1
#            
        else:
#            #' The bulb remains off
            LSimulationArray[2 + iTime][i]= 0
#print iTime, "off"
#            #' Increment the time
            iTime = iTime + 1
#        
#
#' Write the simulation data to the sheet
#Range("light_sim_data!E9:BL1451") = vSimulationArray
#
LSimRes =[0 for i in range(1,1441)]
for i in range(3,1443):
    res=0
    for j in range(1,iNumBulbs+1):
      res=res+ LSimulationArray[i][j]
    print i, res
    LSimRes[i-3]=res

