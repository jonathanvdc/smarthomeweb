import probmod.py


# Load the activity statistics into a collection
def LoadActivityStatistics():
    for i in range(7,79):
        #        ' Create a new probability modifier
        Set oActivityStatsItem = New ProbabilityModifier
    
        ' Read in the data
        oActivityStatsItem.IsWeekend = IIf(Range("'activity_stats'!B" + CStr(i)).Value = 1, True, False)
        oActivityStatsItem.ActiveOccupantCount = Range("'activity_stats'!C" + CStr(i)).Value
        oActivityStatsItem.ID = Range("'activity_stats'!D" + CStr(i)).Value
        
        ' Get the hourly modifiers
        For j = 0 To 143
        
            ' Get the column reference
            sCell = Cells(i, j + 5).Address(True, False, xlA1)
        
            ' Read the values
            oActivityStatsItem.Modifiers(j) = Range("'activity_stats'!" + sCell).Value
            
        Next j

        ' Now generate a key
        sKey = IIf(oActivityStatsItem.IsWeekend, "1", "0") + "_" + CStr(oActivityStatsItem.ActiveOccupantCount) + "_" + oActivityStatsItem.ID
        
        ' Add this object to the collection
        oActivityStatistics.Add Item:=oActivityStatsItem, Key:=sKey
    
    Next i

End Sub