import random

#Result=[]
Result=OccupanceSim(Mat)
#for i in range(0,144):
#   print i+1,Result[i]

Dim oActivityStatistics As Collection

' Declare an object to store activity statistics
Dim oActivityStatsItem As ProbabilityModifier

' Declare the appliance description variables
Dim sApplianceType As String
Dim iMeanCycleLength As Integer
Dim iCyclesPerYear As Integer
Dim iStandbyPower As Integer
Dim iRatedPower As Integer
Dim dCalibration As Double
Dim dOwnership As Double
Dim iTargetAveragekWhYear As Integer
Dim sUseProfile As String
Dim iRestartDelay As Integer
Dim bHasAppliance As Boolean


iCycleTimeLeft
iRestartDelayTimeLeft
iRestartDelay
iPower
CycleLength
iMeanCycleLength


    
    
def ConfigureAppliancesInDwelling():
    # Vertical offset
    iOffset = 12
    # For each appliance
    for  i in range(1,34):
        # Get a random number
        dRan = random.random()
        # Get the proportion of houses with this appliance
        dProportion = appliances[i + iOffset][E]
        # Determine if this simulated house has this appliance
        Range("appliances!D" + CStr(i + iOffset)).Value = IIf(dRan < dProportion, "YES", "NO")
        
