#!/usr/bin/env python2.7

from libclient import *

# Parses command-line arguments.
# The number of days to generate measurements for
# is returned.
def parse_args():
    args = sys.argv
    if len(args) == 3:
        dateformat = "%Y-%m-%d"
        return datetime.strptime(args[1], dateformat), int(args[2])
    else:
        log("Invalid number of command-line arguments. Expected exactly two: the end time and the number of days to generate measurements for.", 31)
        sys.exit(1)

######################################################################
### Main script
######################################################################

if __name__ == "__main__":
    end_time, day_count = parse_args()
    try:
        server = startServer()

        post_elecsim(end_time, day_count, get_locations(), False)
        log('Success!')

    finally:
        server.kill()
        log('Server stopped.')
