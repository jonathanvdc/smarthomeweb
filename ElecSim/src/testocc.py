import occsimread

# set max number of residents, month, weekday or weekend
iResidents=5  # range 1-5
iMonth=11
#bWeekend = True
bWeekend = False

ResultofOccupancySim=occsimread.OccupanceSim(iResidents,bWeekend)

iMinute = 1
while iMinute < 1440:
        #print "Type"
        #print sApplianceType
        #print "Minute:"
        #print iMinute
        #
        #                ' Set the default (standby) power demand at this time step
            #' Get the ten minute period count
   iTenMinuteCount = int(((iMinute - 1) // 10))+1
            #print iTenMinuteCount, 10 + iTenMinuteCount
            #
            #    ' Get the number of current active occupants for this minute
            #    ' Convert from 10 minute to 1 minute resolution
            #            print 11 + ((iMinute - 1) // 10)
   iActiveOccupants = ResultofOccupancySim[iTenMinuteCount-1]
#    ' Now generate a key to get the activity statistics
   iMinute=iMinute+1

for i in range(0,144):
   print i+1, ResultofOccupancySim[i]
