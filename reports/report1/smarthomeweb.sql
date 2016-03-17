create table Person (
    guid text primary key not null,
    username text not null,
    name text,
    password text not null, -- Plaintext for now
    birthdate integer not null, -- In Unix time
    address text not null, -- Street and number
    city text not null,
    zipcode text not null
);

create table Friends (
    personOne text not null references Person(guid),
    personTwo text not null references Person(guid),
    primary key (personOne, personTwo)
);

create table Message (
    id integer primary key autoincrement,
    sender text not null references Person(guid),
    recipient text not null references Person(guid),
    message text not null
    -- 'usage messages' as described in assignment
    -- can simply insert a link into message.
);

create table Location (
    id integer primary key autoincrement,
    name text not null
);

-- "Person A has location B" is an N:N relation, so
-- we need a separate table to store its tuples.
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
    -- Unix time for YYYY-MM-DD XX:00.
    -- Identifies the hour.
    unixtime integer not null,
    average real not null,
    notes text,
    primary key (sensorId, unixtime)

);

-- We might want indices like this:
--
--     create unique index hourIndex
--         on hourAverage (unixtime, sensorId)

-- Separately track day averages after removing outliers
-- from the HourAverage results.
create table DayAverage (
    sensorId integer not null references Sensor(id),
    -- Unix time for YYYY-MM-DD 00:00.
    -- Identifies the day.
    unixtime integer not null,
    average real not null,
    notes text,
    primary key (sensorId, unixtime)
);
