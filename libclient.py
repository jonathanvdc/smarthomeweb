#!/usr/bin/env python2.7

import csv
import errno
import json
import os
import sys
import platform
import requests
import time

from datetime import*
from subprocess import Popen
from os.path import join

######################################################################
### Helper functions
######################################################################

def log(s, color_code = 36):
    if os.name == 'nt':
        formatter = '*** %s'
    else:
        formatter = '\x1b[' + str(color_code) + 'm*** %s\x1b[0m'
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

# Makes sure that the given response was not an error.
def checkResponse(response):
    if response.status_code < 200 or response.status_code >= 300:
        log("Error: %s %s" % (response.request.method, response.url), 31)
        log("Status code: %d (%s)" % (response.status_code, response.reason), 31)
        print(response.text)
        raise Exception()
    return response

# Performs a GET request, and checks the response.
def getChecked(url, **kwargs):
    return checkResponse(requests.get(url, **kwargs))

# Performs a POST request, and checks the response.
def postChecked(url, **kwargs):
    return checkResponse(requests.post(url, **kwargs))

# Performs a PUT request, and checks the response.
def putChecked(url, **kwargs):
    return checkResponse(requests.put(url, **kwargs))

######################################################################
### API stuff
######################################################################

api = "http://localhost:8088/api/"

def post_file(where, filepath):
    log('Posting %s' % filepath)
    with open(filepath, 'rb') as f:
        postChecked(api + where, data=f)

def get_person_guid(username):
    persons = json.loads(getChecked(api + 'persons').text)
    try:
        return next(p['personGuid']
                    for p in persons
                    if p['data']['username'] == username)
    except StopIteration:
        raise KeyError('no user called %r' % username)

def get_location_id(name):
    locations = json.loads(getChecked(api + 'locations').text)
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
    postChecked(api + 'locations', json=j)
    location_id = get_location_id(name)
    postChecked(api + 'has-location', json=[{'personGuid': guid},
                                              {'locationId': location_id}])

def add_message(sender, recipient, body):
    sender_id = get_person_guid(sender)
    recipient_id = get_person_guid(recipient)
    j = [{'senderId': sender_id,
          'recipientId': recipient_id,
          'message': body}]
    postChecked(api + 'messages', json=j)

def add_friend_request(sender, recipient):
    sender_id = get_person_guid(sender)
    recipient_id = get_person_guid(recipient)
    j = [{'personOneGuid': sender_id,
          'personTwoGuid': recipient_id}]
    postChecked(api + 'friends', json=j)

def add_friendship(person1, person2):
    add_friend_request(person1, person2)
    add_friend_request(person2, person1)

######################################################################
### ElecSim
######################################################################

def printDateTime(dt):
    return dt.strftime("%Y-%m-%dT%H:%M")

# Aggregates all measurements made by the given
# list of sensors.
def aggregateMeasurements(sensors, time, day_count):
    for s in sensors:
        log('Aggregating data for %s at location %d...' % (s['data']['name'], s['data']['locationId']))
        # First, aggregate days...
        url = api + 'day-average/%d/%s/%d' % (s['id'], (time - timedelta(days = day_count)).isoformat(), day_count)
        getChecked(url)
        # Next, aggregate thirteen months.
        url = api + 'month-average/%d/%s/13' % (s['id'], time.isoformat())
        getChecked(url)

# Compacts all measurements that were made during the given period of time.
def compactMeasurements(start_time, end_time, compaction_level = 'measurements'):
    log('Compacting measurements for time period %s - %s' % (start_time.isoformat(), end_time.isoformat()))
    putChecked(api + 'compact/%s/%s/%s' % (compaction_level, start_time.isoformat(), end_time.isoformat()))

# Gets all sensors at the given location if a location identifier is given,
# Otherwise, gets all sensors in the database.
def getSensors(location_id = None):
    if location_id is None:
        return json.loads(getChecked(api + 'sensors').text)
    else:
        return json.loads(getChecked(api + 'sensors/at-location/%d' % location_id).text)

# Creates sensors with the given names for the location with the given
# identifier.
def createSensors(sensor_names, location_id):
    preexisting = set([s['data']['name'] for s in getSensors(location_id)])

    j = [{'name': name,
          'description': name,
          'locationId': location_id}
         for name in sensor_names
         if name not in preexisting]

    if len(j) > 0:
        log('Creating %d sensors...' % len(j))
        postChecked(api + 'sensors', json=j)

# Generates a csv file filled with measurements for the given location object.
# Measurements are limited to the given timespan.
def generateData(location, startTime, endTime):
    name = location['data']['name']
    location_id = location['id']
    log('=' * 70)
    log('Generating ElecSim data for household %d (%s), timespan: %s - %s...' % (location_id, name, printDateTime(startTime), printDateTime(endTime)))
    Popen(
        ['python3', 'main.py',
         '--mode=generate',
         '--config_file=configuration.json',
         '--household=%d' % location_id,
         '--from=' + printDateTime(startTime),
         '--to=' + printDateTime(endTime),
         '--output=output.csv'],
        cwd='ElecSim'
    ).wait()

# Generates and processes data for the given location.
# A dictionary is returned that maps (SensorName, Time) tuples
# to measurement values.
def generateAndProcessData(location, startTime, endTime):
    location_id = location['id']
    generateData(location, startTime, endTime)

    # Maps a (SensorName, Time) to a measurement value.
    sensor_data = {}

    log('Processing sensor data...')
    with open(join('ElecSim', 'output.csv')) as f:
        reader = csv.reader(f, delimiter=';')
        top = next(reader)
        sensor_names = top[2:-1]

        createSensors(sensor_names, location_id)

        for line in reader:
            for i, name in enumerate(sensor_names, 2):
                key = (name, line[0])
                sensor_data[key] = float(line[i])

    return sensor_data

# Generates, processes, and POSTs data to the server.
# The set of sensors for which data was generated, is returned.
def generateAndUploadData(location, startTime, endTime):
    # Generate data.
    # sensor_data maps a (SensorName, Time) to a measurement value.
    sensor_data = generateAndProcessData(location, startTime, endTime)

    sensors = getSensors(location['id'])
    sensor_data_to_id = { s['data']['name'] : s['id'] for s in sensors }

    measurements = []
    for (name, time), value in sensor_data.items():
        sensor_id = sensor_data_to_id[name]
        measurements.append({
            'sensorId': sensor_id,
            'timestamp': time.replace(' ', 'T') + 'Z',
            'measurement': value
        })

    log('Posting measurements to server.')
    log('Uploading %d measurements... (%s)' % (len(measurements), size_format(len(json.dumps(measurements)))))
    postChecked(api + 'measurements', json=measurements)
    return sensors

def post_elecsim(day_count):
    locations = json.loads(getChecked(api + 'locations').text)
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

        # We will generate measurements for five days at a time.
        # I reckon this is a good compromise between database interaction
        # and memory use for measurements.
        data_size = 5
        j = 0
        endTime = now
        while j < day_count - day_count % data_size:
            # Generate data for five days at once.
            startTime = endTime - timedelta(days = data_size)
            sensors = generateAndUploadData(locations[i - 1], startTime, endTime)
            endTime = startTime
            j += data_size

        if j < day_count:
            # Generate data for the remaining days.
            startTime = endTime - timedelta(days = day_count - j)
            sensors = generateAndUploadData(locations[i - 1], startTime, endTime)

        aggregateMeasurements(sensors, now, day_count)

# Launches the server, and does not return until it is ready to handle
# requests.
def startServer():
    server_path = os.path.abspath(join(
        'backend',
        'SmartHomeWeb',
        'SmartHomeWeb',
        'bin',
        'Release',
        'SmartHomeWeb.exe'))

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
    return server
