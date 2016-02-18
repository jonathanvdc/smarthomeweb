# Functions for simulation
import random
from math import pow, log, exp

def CycleLength(sApplianceTypelocal,iMeanCycleLengthlocal):
     # Set the value to that provided in the configuration
     CycleLengthlocal = iMeanCycleLengthlocal
     # Use the TV watching length data approximation, derived from the TUS data
     if (sApplianceTypelocal == "TV1" or sApplianceTypelocal == "TV2" or sApplianceTypelocal == "TV3"):
         # The cycle length is approximated by the following function
         # The avergage viewing time is approximately 73 minutes
         CycleLengthlocal = int(70*pow(0-log(1-random.random()),1.1))
     elif (sApplianceTypelocal == "STORAGE_HEATER" or sApplianceTypelocal == "ELEC_SPACE_HEATING"):
         # Provide some variation on the cycle length of heating appliances
         CycleLengthlocal =  int(random.random()*iMeanCycleLengthlocal)
         #TODO Lighting_Model.GetMonteCarloNormalDistGuess(CDbl(iMeanCycleLength), iMeanCycleLength / 10)
         #print "ML", iMeanCycleLengthlocal, "CL", CycleLengthlocal
     return CycleLengthlocal
    

def GetMonteCarloNormalDistGuess(dMeanl,dSDl):
    
    if dMeanl==0:
        bOK=True
    else:
        bOK=False

    iGuess=0
    random.seed()
    
    while (bOK==False):
        iGuess = (random.random()*(dSDl* 8))-(dSDl*4) + dMeanl
        px = (1/(dSDl*(pow(2*3.14159,0.5))))*exp(-pow(iGuess-dMeanl,2)/(2*dSDl*dSDl))
        if (px >= random.random()):
            bOK = True

    return int(iGuess)

def StartAppliance(sApplianceTypelocall,iMeanCycleLengthlocall,iRestartDelaylocall,iRatedPowerlocall,iStandbyPowerlocall):
    # Determine how long this appliance is going to be on for
    iCycleTimeLeftlocall = CycleLength(sApplianceTypelocall,iMeanCycleLengthlocall)
    #print iCycleTimeLeftlocall
    # Determine if this appliance has a delay after the cycle before it can restart
    iRestartDelayTimeLeftlocall = iRestartDelaylocall
    # Set the power
    iPowerlocall = GetPowerUse(sApplianceTypelocall,iRatedPowerlocall,iCycleTimeLeftlocall,iStandbyPowerlocall)
    # Decrement the cycle time left
    iCycleTimeLeftlocall = iCycleTimeLeftlocall - 1
    return iCycleTimeLeftlocall, iRestartDelayTimeLeftlocall, iPowerlocall



def GetPowerUse(sApplianceTypel,iRatedPowerl,iCycleTimeLeftl,iStandbyPowerl):
    # Set the return power to the rated power
    GetPowerUsagel = iRatedPowerl
    #print GetPowerUsagel
    # Some appliances have a custom (variable) power profile depending on the time left
    if (sApplianceTypel == "WASHING_MACHINE" or sApplianceTypel == "WASHER_DRYER"):
        if (sApplianceTypel == "WASHING_MACHINE"):
            iTotalCycleTimel = 138
        else:
            iTotalCycleTimel = 198
        #' This is an example power profile for an example washing machine
        #' This simplistic model is based upon data from personal communication with a major washing maching manufacturer
        timel=iTotalCycleTimel-iCycleTimeLeftl+1
        #print timel
        if 1 <= timel <= 8:
            GetPowerUsagel = 73
            #         ' Start-up and fill
        elif 9 <= timel <= 29:
             GetPowerUsagel = 2056
             #     ' Heating
        elif 30 <= timel <= 81:
             GetPowerUsagel = 73
             #      ' Wash and drain
        elif 82 <= timel <= 92:
             GetPowerUsagel = 73
             #       ' Spin
        elif 93 <= timel <= 94:
             GetPowerUsagel = 250
             #      ' Rinse
        elif 95 <= timel <= 105:
             GetPowerUsagel = 73
             #      ' Spin
        elif 106 <= timel <= 107:
             GetPowerUsagel = 250
             #    ' Rinse
        elif 108 <= timel <= 118:
             GetPowerUsagel = 73
             #     ' Spin
        elif 119 <= timel <= 120:
             GetPowerUsagel = 250
             #    ' Rinse
        elif 121 <= timel <= 131:
             GetPowerUsagel = 73
             #     ' Spin
        elif 132 <= timel <= 133:
            GetPowerUsagel = 250
            #    ' Rinse
        elif 134 <= timel <= 138:
             GetPowerUsagel = 568
             #    ' Fast spin
        elif 139 <= timel <= 198:
             GetPowerUsagel = 2500
             #   ' Drying cycle
        else: 
            GetPowerUsagel = iStandbyPowerl
    return GetPowerUsagel
    
    