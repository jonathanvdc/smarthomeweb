#!/usr/bin/env python2.7

from libclient import *

# Parses command-line arguments.
# The number of days to generate measurements for
# is returned.
def parse_args():
    args = sys.argv
    if len(args) == 1:
        return 30
    elif len(args) == 2:
        return int(args[1])
    else:
        log("Invalid number of command-line arguments. Expected at most one: the number of days to generate measurements for.", 31)
        sys.exit(1)

######################################################################
### Main script
######################################################################

if __name__ == "__main__":
    arg = parse_args()

    sql_path = join('backend', 'database', 'smarthomeweb.sql')

    with open(sql_path) as f:
        log('Renewing database...')
        remove(join('backend', 'database', 'smarthomeweb.db'))
        Popen(
            ['sqlite3',  'smarthomeweb.db'],
            cwd=join('backend', 'database'),
            stdin=f
        ).wait()

    log('Done.')

    try:
        server = startServer()

        post_file('persons', join('example-files', 'person-data.json'))

        log('Creating locations...')
        create_location('Serverroom', 'bgoethals')
        create_location('Graafschap van Houdt', 'bennyvh')
        create_location('Casa de Hans', 'hans')

        log('Creating posts...')
        add_message('bgoethals', 'jonsneyers', 'Hallo')
        add_message('jonsneyers', 'bgoethals', 'Dag dag')
        add_message('jonsneyers', 'hans', 'Hallo, ik ben een test post!')
        add_message('bgoethals', 'hans', 'Goedendag, ik POST graag posts!')

        log('Creating friendships...')
        add_friend_request('bgoethals','hans')
        add_friend_request('jonsneyers','hans')
        add_friendship('lilferemans','hans')

        post_elecsim(arg)
        log('Success!')

    finally:
        server.kill()
        log('Server stopped.')
