# generate_date, was before in main.py
#Author: Len Feremans 8-Feb-2016
import os, csv, sys, random
import datetime
from datetime import timedelta
import math
from math import log
import occsimread, appliance, activitydat, appsimfun, bulbdat


def generate_data_range(iResidents, Dwell,iIrradianceThreshold,iRandomHouse,from_date, to_date):
    if to_date < from_date:
        raise ValueError("from_date > to_date!")
    days = []
    start_date = from_date
    while(start_date < to_date):
        days.append(start_date)
        start_date = start_date + timedelta(days=1)
    first = True
    data = []
    for day in days:
        addHeader = False
        if first:
            addHeader = True
            first = False
        bWeekend = day.weekday() == 5 or day.weekday() == 6
        #Generate each day...
        ResultofOccupancySim = occsimread.OccupanceSim(iResidents,bWeekend)
        iMonth = day.month
        day_data = generate_date_single_day(Dwell, ResultofOccupancySim, bWeekend, iMonth, iIrradianceThreshold, iRandomHouse, addHeader)
        datetime = day
        datetime.replace(hour=0,minute=0,second=0)
        #copy day_data, but add timestamp
        if addHeader:
            first_row = day_data.pop(0)
            first_row.insert(0,'Timestamp')
            data.append(first_row)
        for row in day_data:
            if datetime < from_date or datetime > to_date: #check for hour-range
                continue
            row.insert(0,datetime)
            data.append(row)
            datetime = datetime + timedelta(minutes=1)
        print("Generated %s" % day)
    return data

def generate_date_single_day(Dwell, ResultofOccupancySim, bWeekend, iMonth, iIrradianceThreshold, iRandomHouse, addHeader):
    #print("generate_date_single_day(%s,%s,%s,%s,%s,%s,%s" % (Dwell, ResultofOccupancySim, bWeekend, iMonth, iIrradianceThreshold, iRandomHouse, addHeader))


    oMonthlyRelativeTemperatureModifier = [0, 1.63, 1.821, 1.595, 0.867, 0.763, 0.191, 0.156, 0.087, 0.399, 0.936, 1.561, 1.994]

    vSimulationArray =[[0 for j in range(33)] for i in range(1442)]
    # This holds result of computation: fore ach appliance power consumption for each minute of the day

    for iAppliance in range(33):
        testapp=100
    #    singleapp=9
        iCycleTimeLeft = 0
        iRestartDelayTimeLeft = 0
        sApplianceType=appliance.appliances[iAppliance][17]
        #if iAppliance==testapp: print "Appliance", sApplianceType, "is: ",
        iMeanCycleLength = appliance.appliances[iAppliance][5]
        #print iMeanCycleLength
        iCyclesPerYear = appliance.appliances[iAppliance][4]
        iStandbyPower = appliance.appliances[iAppliance][7]
        iRatedPower = appliance.appliances[iAppliance][6]
        dCalibration = appliance.appliances[iAppliance][20]
        dOwnership = appliance.appliances[iAppliance][2]
        iTargetAveragekWhYear = appliance.appliances[iAppliance][23]
        sUseProfile = appliance.appliances[iAppliance][18]
        iRestartDelay = appliance.appliances[iAppliance][8]
        if Dwell[iAppliance] == 'NO':
            bHasAppliance=False
        else:
            bHasAppliance=True
        #bHasAppliance = IIf(Range("'appliances'!D" + CStr(iAppliance + iApplianceSourceCellOffsetY)).Value = "YES", True, False)
        #if iAppliance==testapp: print "Appliance", sApplianceType, "Mean Cycle", iMeanCycleLength, "Cycles/Year ", iCyclesPerYear, "Stand By", iStandbyPower, "Rated Power", iRatedPower, "Calib", dCalibration, "UseProfile", sUseProfile, "Restart Delay", iRestartDelay

        #    ' Write the appliance type into result
        vSimulationArray[0][iAppliance] = sApplianceType
        #    ' Write the units into result
        vSimulationArray[1][iAppliance] = "(W)"
        #
        #    ' Check if this appliance is assigned to this dwelling
        if bHasAppliance==False:

    #            ' This appliance is not applicable, so write zeros to the power demand
            #for iCount in range(2,1441):
    #   vSimulationArray[iCount][iAppliance] = 0
            if iAppliance==testapp: print("ABSENT")
    #        for iCount in range(0,1440):
    #            print vSimulationArray[iCount][iAppliance]

        else:
            #' Randomly delay the start of appliances that have a restart delay (e.g. cold appliances with more regular intervals)
            iRestartDelayTimeLeft = int(random.random()*iRestartDelay*2)
            #' Weighting is 2 just to provide some diversity
            #
            #   ' Make the rated power variable over a normal distribution to provide some variation
            iRatedPower = appsimfun.GetMonteCarloNormalDistGuess(iRatedPower,iRatedPower/10)
            #if iAppliance==testapp: print "PRESENT", "Restart Delay Left: ", iRestartDelayTimeLeft, "Rated Power", iRatedPower

            # Lighting_Model.GetMonteCarloNormalDistGuess(Val(iRatedPower), iRatedPower / 10)
            #
            #
            #    ' Loop through each minute of the day
            iMinute = 1
            while iMinute < 1440:
                #print "Type"
                #print sApplianceType
                #print "Minute:"
                #print iMinute
                #
                #                ' Set the default (standby) power demand at this time step
                iPower = iStandbyPower
                #' Get the ten minute period count
                iTenMinuteCount = int(((iMinute - 1) // 10))+1
                #print iTenMinuteCount, 10 + iTenMinuteCount
                #
                #    ' Get the number of current active occupants for this minute
                #    ' Convert from 10 minute to 1 minute resolution
                #            print 11 + ((iMinute - 1) // 10)
                iActiveOccupants = ResultofOccupancySim[iTenMinuteCount-1]
                #    ' Now generate a key to get the activity statistics
                #    sKey = IIf(bWeekend, "1", "0") + "_" + CStr(iActiveOccupants) + "_" + sUseProfile
                #
                if bWeekend == False:
                    bweek=0
                else:
                    bweek=1
                #print bweek, iActiveOccupants, sUseProfile
                sKey=activitydat.KeyRowId(bweek,iActiveOccupants,sUseProfile)
                #print sKey
                #    ' If this appliance is off having completed a cycle (ie. a restart delay)
                #if iAppliance==testapp: print "Time (min)", iMinute, "Ten Min", iTenMinuteCount, "Occ: ", iActiveOccupants, "key to stats", sKey
                if (iCycleTimeLeft <=0 and iRestartDelayTimeLeft > 0):
                    #      ' Decrement the cycle time left
                    iRestartDelayTimeLeft = iRestartDelayTimeLeft - 1
                    #if iAppliance==testapp: print "CASE A: on", iCycleTimeLeft, "power", iPower, "(restart delay ", iRestartDelayTimeLeft, ")"
                    #print "starts on ", iRestartDelayTimeLeft, " Cycles left ", iCycleTimeLeft
                elif iCycleTimeLeft <= 0:
                    #      ' There must be active occupants, or the profile must not depend on occupancy for a start event to occur
                    if (iActiveOccupants > 0 and sUseProfile != "CUSTOM") or (sUseProfile == "LEVEL"):
                        #    ' Variable to store the event probability (default to 1)
                        dActivityProbability = 1
                        #    ' For appliances that depend on activity profiles and is not a custom profile ...
                        if (sUseProfile != "LEVEL") and (sUseProfile != "ACTIVE_OCC") and (sUseProfile != "CUSTOM"):
                            #    ' Get the activity statistics for this profile
                            #oActivityStatsItem = activitydat.actid[sKey][
                            #oActivityStatistics.Item(sKey)
                            #    ' Get the probability for this activity profile for this time step
                            # TODO dActivityProbability = oActivityStatistics(sKey).Modifiers(iTenMinuteCount)
                            dActivityProbability=activitydat.actid[sKey-1][2+iTenMinuteCount]
                            #if (iAppliance==testapp): print "CASE B: column act", iTenMinuteCount, "value", dActivityProbability
                            #dActivityProbability=random.random()
                            #print iMinute, dActivityProbability
                        elif sApplianceType == "ELEC_SPACE_HEATING":
                            #' For electric space heaters ... (excluding night storage heaters)
                            #    ' If this appliance is an electric space heater, then then activity probability is a function of the month of the year
                            dActivityProbability = oMonthlyRelativeTemperatureModifier[iMonth]
                            if (iAppliance==testapp): print("CASE C:")
                            #print dActivityProbability
                        else:
                            if (iAppliance==testapp): print("CASE D:")
                            pass

                        #' Check the probability of a start event
                        if random.random() < (dCalibration*dActivityProbability):
                            #StartAppliance(sApplianceType,iMeanCycleLength,iRestartDelay):
                            a=[]
                            a=appsimfun.StartAppliance(sApplianceType,iMeanCycleLength,iRestartDelay,iRatedPower,iStandbyPower)
                            #                        print a
                            iCycleTimeLeft=int(a[0])
                            iRestartDelayTimeLeft=int(a[1])
                            iPower = int(a[2])
                            #if (iAppliance==testapp):
                            #    print "********************Starting app:"
                            #    print sApplianceType, " on ", iRestartDelayTimeLeft, "cycles", iCycleTimeLeft, "power", iPower
                    #' Custom appliance handler: storage heaters have a simple representation
                    elif (sUseProfile == "CUSTOM" and sApplianceType == "STORAGE_HEATER"):
                        pass
                        # ' The number of cycles (one per day) set out in the calibration sheet
                        #                    ' is used to determine whether the storage heater is used
                        #
                        #                    ' This model does not account for the changes in the Economy 7 time
                        #                    ' It assumes that the time starts at 00:30 each day
    #                    if (iTenMinuteCount == 4):
    #                        oDate = "1/14/97
    #                        #  ' Get the month and day when the storage heaters are turned on and off, using the number of cycles per year
    #                        oDateOff = DateAdd("d", iCyclesPerYear / 2, oDate)
    #                        oDateOn = DateAdd("d", 0 - iCyclesPerYear / 2, oDate)
    #                        iMonthOff = DatePart("m", oDateOff)
    #                        iMonthOn = DatePart("m", oDateOn)
    #                        # If this is a month in which the appliance is turned on of off
    #                        if (iMonth == iMonthOff) or (iMonth == iMonthOn):
    #                            #            ' Pick a 50% chance since this month has only a month of year resolution
    #                            dProbability = 0.5 / 10 # ' (since there are 10 minutes in this period)
    #                        elif (iMonth > iMonthOff) and (iMonth < iMonthOn):
    #                            # ' The appliance is not used in summer
    #                             dProbability = 0
    #                        else:
    #                            # ' The appliance is used in winter
    #                            dProbability = 1
    #                        if (iAppliance==testapp): print "CASE E:"
    #                        dProbability=0.4
    ##                        #' Determine if a start event occurs
    #                        if (random.random() <= dProbability):
    #                            #  ' This is a start event
    #                            #StartAppliance
    #                            if (iAppliance==testapp): print "CASE F:"
    #                            a=[]
    #                            a=appsimfun.StartAppliance(sApplianceType,iMeanCycleLength,iRestartDelay,iRatedPower,iStandbyPower)
    #                            iCycleTimeLeft=a[1]
    #                            iRestartDelayTimeLeft=a[0]
    #                            iPower = a[2]
    #                            #iCycleTimeLeft,iRestartDelayTimeLeft,iPower=appsimfun.StartAppliance(sApplianceType,iRestartDelay,iRatedPower,iCycleTimeLeft,iStandbyPower)
    #                            #print iCycleTimeLeft
                    else:
                        if (iAppliance==testapp): print("Case Z", iPower)
                else:
                        #if (iAppliance==testapp): print "Case X"
                        # ' The appliance is on - if the occupants become inactive, switch off the appliance
                        if (iActiveOccupants == 0) and (sUseProfile != "LEVEL") and (sUseProfile != "ACT_LAUNDRY") and (sUseProfile != "CUSTOM"):
                            if (iAppliance==testapp): print ("CASE G:", iPower)
                        else:
                            #if (iAppliance==testapp): print "CASE H:"
                            #' Set the power
                            #GetPowerUsage(sApplianceType,iRatedPower,iCycleTimeLeft,iStandbyPower)
                            #print iCycleTimeLeft
                            iPower = appsimfun.GetPowerUse(sApplianceType,iRatedPower,iCycleTimeLeft,iStandbyPower)
                            #if (iAppliance==testapp): print "CASE H: App is on ", iPower
                               #print "Wash"
                            #print iPower
                            #print iCycleTimeLeft
                            #' Decrement the cycle time left
                            iCycleTimeLeft = iCycleTimeLeft-1

                #' Set the appliance power at this time step
                vSimulationArray[1 + iMinute][iAppliance] = iPower
                #    ' Increment the time
                iMinute = iMinute + 1
                #print "Power", iPower

    #' Write the data back to the simulation sheet
    #print vSimulationArray


    #def RunLightingSimulation():

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

    OccLights=[ 0.000, 1.000, 1.52814569536424,1.69370860927152,1.98344370860927,2.09437086092715]
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


    irrdat =[0 for i in range(1440)]

    datadir = os.path.dirname(__file__) + '/../data'
    f = open(datadir + '/irradiance.csv', 'rt')

    reader = csv.reader(f,quoting=csv.QUOTE_NONNUMERIC)

    i=0

    for row in reader:
       #       print row
        irrdat[i]=float(row[iMonth-1])
        i=i+1

    f.close()

    #print irrdat

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
            #iIrradiance = irridiancedat.irrdat[iTime-1][iMonth]
            iIrradiance = irrdat[iTime-1]
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
                        #                iActiveOccupants = vOccupancyArray(((iTime - 1) \ 10) + 1, 1)
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
        LSimRes[i-3]=res
    #    print i-2, LSimRes[i-3]


    #make output
    #array for each minute of day starting from 00.00 until 23.59 (24 * 60 = 1440)
    #for each device that is active in household + for total lights
    header = ["Total","Lights"]
    active_devices = []
    for j in range(0,33):
        if Dwell[j] == "YES":
            header.append(appliance.appliances[j][0])
            active_devices.append(j)
    start = 0
    end = 1440
    TotalArray = [[0 for j in range(len(header))] for i in range(1440)]
    if addHeader:
        TotalArray = [[0 for j in range(len(header))] for i in range(1441)]
        TotalArray[0] = header
        start = 1
        end = 1441
    for i in range(start,end):
        sim_lights_idx = i-start
        sim_device_idx = i+2-start
        TotalArray[i][1] = LSimRes[sim_lights_idx] #lights
        for idx in range(len(active_devices)): #active devices
            device_use = vSimulationArray[sim_device_idx][active_devices[idx]]
            TotalArray[i][idx+2] = device_use
        total=LSimRes[sim_lights_idx]
        for idx in range(len(active_devices)): #active devices
            total += vSimulationArray[sim_device_idx][active_devices[idx]]
        TotalArray[i][0]=total

    foefel_factor = 1.0/20.0
    #waarom? Nu gemiddelde per maand rond de 20.000kwh, we willen rond de 1000kwh
    for i in range(start,end):
        for j in range(len(TotalArray[i])):
            TotalArray[i][j]=TotalArray[i][j] * foefel_factor
    return TotalArray