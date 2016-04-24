#!/usr/bin/env python2.7

from libclient import *

if __name__ == "__main__":
    try:
        server = startServer()
        vacuum()
        log('Success!')
    finally:
        server.kill()
        log('Server stopped.')
