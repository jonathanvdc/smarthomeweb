#!/usr/bin/env python2.7

import csv
import errno
import json
import os
import platform
import requests
import time

from datetime import*
from subprocess import Popen
from os.path import join

######################################################################
### Helper functions
######################################################################

def log(s):
    if os.name == 'nt':
        formatter = '*** %s'
    else:
        formatter = '\x1b[36m*** %s\x1b[0m'
    print(formatter % s)

def remove(path):
    try:
        os.remove(path)
    except OSError as e:
        if e.errno != errno.ENOENT:
            raise

def popen_mono(path):
    log('Launching %s' % path)
    log('Platform: %s' % os.name)
    cmd = [path]
    if os.name != 'nt' and 'CYGWIN' not in platform.system():
        cmd.insert(0, 'mono')
    read, write = os.pipe()
    return Popen(cmd, stdin=read)

def size_format(num, suffix='B'):
    for unit in ['', 'Ki', 'Mi', 'Gi', 'Ti', 'Pi', 'Ei', 'Zi']:
        if abs(num) < 1024.0:
            return "%3.1f%s%s" % (num, unit, suffix)
        num /= 1024.0
    return "%.1f%s%s" % (num, 'Yi', suffix)

######################################################################
### API stuff
######################################################################

api = "http://localhost:8088/api/"

def post_file(where, filepath):
    log('Posting %s' % filepath)
    with open(filepath, 'rb') as f:
        requests.post(api + where, data=f)

def get_person_guid(username):
    persons = json.loads(requests.get(api + 'persons').text)
    try:
        return next(p['personGuid']
                    for p in persons
                    if p['data']['username'] == username)
    except StopIteration:
        raise KeyError('no user called %r' % username)

def get_location_id(name):
    locations = json.loads(requests.get(api + 'locations').text)
    try:
        return next(l['id']
                    for l in locations
                    if l['data']['name'] == name)
    except StopIteration:
        raise KeyError('no location called %r' % name)

def create_location(name, username):
    guid = get_person_guid(username)
    log('Creating location %s for user %s (guid: %s).' % (name, username, guid))
    j = [{'ownerGuid': guid, 'name': name}]
    requests.post(api + 'locations', json=j)
    location_id = get_location_id(name)
    requests.post(api + 'has-location', json=[{'personGuid': guid},
                                              {'locationId': location_id}])

def add_message(sender, recipient, body):
    sender_id = get_person_guid(sender)
    recipient_id = get_person_guid(recipient)
    j = [{'senderId': sender_id,
          'recipientId': recipient_id,
          'message': body}]
    requests.post(api + 'messages', json=j)

######################################################################
### ElecSim
######################################################################

def printDateTime(dt):
    return dt.strftime("%Y-%m-%dT%H:%M")

# Aggregates all measurements made during the given year by the given
# list of sensors.
def aggregateMeasurements(sensors, time):
    log('Aggregating data...')
    for s in sensors:
        log('Aggregating data for sensor %s' % s['data']['name'])
        requests.get(api + 'year-average/%d/%s' % (s['id'], time.isoformat()))

# Creates sensors with the given names for the location with the given
# identifier.
def createSensors(sensor_names, location_id):
    preexisting = set([s['data']['name'] for s in json.loads(requests.get(api + 'sensors/at-location/%d' % location_id).text)])

    j = [{'name': name,
          'description': name,
          'locationId': location_id}
         for name in sensor_names
         if name not in preexisting]

    if len(j) > 0:
        log('Creating %d sensors...' % len(j))
        requests.post(api + 'sensors', json=j)

def post_elecsim():
    locations = json.loads(requests.get(api + 'locations').text)
    num_locations = len(locations)
    now = datetime.now()

    log("Current time: " + printDateTime(now))

    Popen(
        ['python3', 'main.py',
         '--mode=configure',
         '--no_house_holds=' + str(num_locations),
         '--output=configuration.json'],
        cwd='ElecSim'
    ).wait()

    for i in range(1, num_locations + 1):
        name = locations[i - 1]['data']['name']
        location_id = locations[i - 1]['id']

        log('=' * 70)
        log('Generating ElecSim data for household %d (%s).' % (i, name))
        Popen(
            ['python3', 'main.py',
             '--mode=generate',
             '--config_file=configuration.json',
             '--household=%d' % i,
             '--from=' + printDateTime(now - timedelta(days = 15)),
             '--to=' + printDateTime(now),
             '--output=output.csv'],
            cwd='ElecSim'
        ).wait()

        # Maps a (SensorName, Time) to a measurement value.
        sensor_data = {}

        log('Processing sensor data.')
        with open(join('ElecSim', 'output.csv')) as f:
            reader = csv.reader(f, delimiter=';')
            top = next(reader)
            sensor_names = top[2:-1]

            createSensors(sensor_names, location_id)

            for line in reader:
                for i, name in enumerate(sensor_names, 2):
                    key = (name, line[0])
                    sensor_data[key] = float(line[i])

        sensors = json.loads(requests.get(api + 'sensors').text)
        sensor_data_to_id = {}

        for s in sensors:
            sensor_data_to_id[
                s['data']['name'],
                s['data']['locationId']] = s['id']

        measurements = []
        for (name, time), value in sensor_data.items():
            sensor_id = sensor_data_to_id[name, location_id]
            measurements.append({
                'sensorId': sensor_id,
                'timestamp': time.replace(' ', 'T') + 'Z',
                'measurement': value
            })

        log('Posting measurements to server.')
        log('Uploading %d measurements... (%s)' % (len(measurements), size_format(len(json.dumps(measurements)))))
        requests.post(api + 'measurements', json=measurements)

        aggregateMeasurements([s for s in sensors if s['data']['locationId'] == location_id], now)

######################################################################
### Main script
######################################################################

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
    create_location('De G.10', 'bgoethals')
    create_location('Functionele Huis', 'bennyvh')
    create_location('Slimme Huis', 'hans')
    post_file('sensors', join('example-files', 'sensor-data.json'))
    post_file('measurements', join('example-files', 'measurement-data.json'))

    add_message('bgoethals', 'jonsneyers', 'Hallo')
    add_message('jonsneyers', 'bgoethals', 'Dag dag')

    post_elecsim()
    log('Success!')

finally:
    server.kill()
    log('Server stopped.')
