create table Person (
    guid text primary key not null,
    username text not null,
    name text,
    password text not null, -- We store plaintext for now
    birthdate integer not null, -- In Unix time
    address text not null, -- Street and number
    city text not null,
    zipcode text not null
);

create table Friends (
    -- A tuple in this table is interpreted as follows:
    --
    --     "person one has added person two as a friend"
    --
    -- The relation need therefore not be symmetric
    -- (like Google+ Circles)
    personOne text not null references Person(guid),
    personTwo text not null references Person(guid),
    primary key (personOne, personTwo)
);

create table Message (
    id integer primary key autoincrement,
    sender text not null references Person(guid),
    recipient text not null references Person(guid),
    message text not null
    -- 'usage messages' as described in assignment can simply insert a link into message.
);

create table Location (
    id integer primary key autoincrement,
    name text not null,
    -- A location has exactly one owner, but there can be any number of
    -- persons who are interested in a location's energy consumption.
    -- (for example, family members may want to know if they left the
    -- lights on, etc)
    owner text not null references Person(guid)
);

-- "Person A has location B" is an N:N relation, so we need a separate table
-- to store its tuples.
create table HasLocation (
    personGuid text not null references Person(guid),
    locationId integer not null references Location(id),
    primary key (personGuid, locationId)
);

create table Sensor (
    id integer primary key autoincrement,
    locationid integer not null references Location(id),
    title text not null,
    description text not null,
    notes text
);

create table ElectricityPrice (
    locationId integer not null references Location(id),
    price real
);

create table Tag (
    id integer primary key autoincrement,
    name text not null
);

create table SensorTag (
    sensorId integer not null references Sensor(id),
    tagId integer not null references Tag(id)
);

create table Measurement (
    sensorId integer not null references Sensor(id),
    unixtime integer not null,
    measured real not null,
    notes text,
    primary key (sensorId, unixtime)
);

create table HourAverage (
    sensorId integer not null references Sensor(id),
    -- Unix time for YYYY-MM-DD XX:00. Identifies the hour.
    unixtime integer not null,
    -- Note that this measurement can be null.
    -- This allows us to represent a lack
    -- of measurements to aggregate.
    measured real,
    -- Aggregate
    notes text,
    primary key (sensorId, unixtime)
);

-- We might want indices like this:
--
--     create unique index hourIndex ON hourAverage (unixtime, sensorId)

-- Separately track day averages after removing outliers from the HourAverage results.
create table DayAverage (
    sensorId integer not null references Sensor(id),
    -- Unix time for YYYY-MM-DD 00:00. Identifies the day.
    unixtime integer not null,
    -- Note that this measurement can be null.
    -- This allows us to represent a lack
    -- of measurements to aggregate.
    measured real,
    notes text,
    primary key (sensorId, unixtime)
);
