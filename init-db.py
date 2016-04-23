#!/usr/bin/env python2.7

from libclient import *

######################################################################
### Main script
######################################################################

if __name__ == "__main__":
    arg = parse_args()

    sql_path = join('backend', 'database', 'smarthomeweb.sql')
    server_path = os.path.abspath(join(
        'backend',
        'SmartHomeWeb',
        'SmartHomeWeb',
        'bin',
        'Release',
        'SmartHomeWeb.exe'))

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
        log('Launching server...')
        server = popen_mono(server_path)

        # Wait for the server to come alive.
        while True:
            try:
                requests.head('http://localhost:8088/', timeout=3.05)
                log('Server launched (PID=%d).' % server.pid)
                break
            except requests.exceptions.RequestException:
                # log('Waiting for server...')
                pass

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
