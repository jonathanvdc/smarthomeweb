#!/usr/bin/env python2.7

from libclient import *

# Parses command-line arguments.
# A period of time to compact is returned.
def parse_args():
    args = sys.argv
    if len(args) == 2:
        return time.strftime("%Y-%m-%d", args[0]), time.strftime("%Y-%m-%d", args[1])
    else:
        log("Invalid number of command-line arguments. Expected exactly two: the start and end date-time to compact.", 31)
        sys.exit(1)

if __name__ == "__main__":
    start_time, end_time = parse_args()

    try:
        server = startServer()
        compactMeasurements(start_time, end_time)
        log('Success!')
    finally:
        server.kill()
        log('Server stopped.')
