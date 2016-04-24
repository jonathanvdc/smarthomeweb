#!/usr/bin/env python2.7

from libclient import *

# Parses command-line arguments.
# A period of time to compact is returned.
def parse_args():
    args = sys.argv
    if len(args) == 4:
        compactLevel = args[1]
        acceptableLevels = ['measurements', 'day-average', 'hour-average']
        if compactLevel not in acceptableLevels:
            log("Compaction level must be one of: %s" % repl(acceptableLevels), 31)
            sys.exit(1)
        dateformat = "%Y-%m-%d"
        return compactLevel, datetime.strptime(args[2], dateformat), datetime.strptime(args[3], dateformat)
    else:
        log("Invalid number of command-line arguments. Expected exactly three:", 31)
        log("the compaction level, followed by the start and end date-time to compact.", 31)
        sys.exit(1)

if __name__ == "__main__":
    compaction_level, start_time, end_time = parse_args()

    try:
        server = startServer()
        compactMeasurements(start_time, end_time, compaction_level)
        log('Success!')
    finally:
        server.kill()
        log('Server stopped.')
