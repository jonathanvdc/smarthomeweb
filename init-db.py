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
        create_location('Kent Farmhouse', 'clark')
        create_location('Wayne Mansion', 'bruce')
        create_location('LexCorp Building', 'lex')

        log('Creating posts...')
        add_message('clark', 'bruce', 'Greetings, fellow citizen.')
        add_message('bruce', 'clark', 'Do you bleed?')
        add_message('lex', 'clark', '#VoteLex2016')
        add_message('diana', 'clark', 'Hi there... ;)')
        add_message('lois', 'clark', 'Top 100 Things I Love About Superman, You\'ll Never Believe #59!')
        add_message('jimmy', 'clark', 'Gee, Clark, you\'ll never believe what happened!')

        log('Creating friendships...')
        add_friend_request('bruce','clark')
        add_friend_request('diana','clark')
        add_friend_request('clark','lex')
        add_friendship('jimmy','clark')
        add_friendship('lois','clark')

        post_elecsim(arg)
        log('Success!')

    finally:
        server.kill()
        log('Server stopped.')
