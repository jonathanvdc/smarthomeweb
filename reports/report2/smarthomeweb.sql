-- These pragmas can improve write performance slightly.
-- They should also be used when opening a database connection.
PRAGMA journal_mode = MEMORY;
PRAGMA synchronous = OFF;

create table Person (
    guid text primary key not null,
    username text not null unique,
    name text,
    password text not null,
    -- In Unix time
    birthdate integer not null,
    -- Street and number
    address text not null,
    city text not null,
    zipcode text not null,
    -- A boolean value that tells whether
    -- this person is an administrator.
    isAdmin boolean not null
);
create table Friends (
    -- A tuple in this table is interpreted as follows:
    --
    --     "person one has sent person two a friend request"
    --
    -- The relation need therefore not be symmetric.
    -- Two people are "friends" if they have sent each
    -- other a friend request.
    personOne text not null references Person(guid)
        on delete cascade,
    personTwo text not null references Person(guid)
        on delete cascade,
    primary key (personOne, personTwo)
);

create table PersonGroup (
	id integer primary key autoincrement,
	name text not null,
	description text not null
);

create table BelongsTo (
	personGroup integer not null references PersonGroup(id),
	person text not null references Person(guid)
        on delete cascade,
	primary key (personGroup, person)
);

create table GroupInvite (
	personGroup integer not null references PersonGroup(id),
	person text not null references Person(guid)
        on delete cascade,
	primary key (personGroup, person)
);

-- (A, B) is in this table if and only if:
-- both (A, B) and (B, A) are in Friends.
create view TwoWayFriends as
    select * from Friends a where exists
        (select 1 from Friends b where
            a.personOne = b.personTwo
            and a.personTwo = b.personOne);

-- (A, B) is in this table if and only if:
-- A has sent a friend request to B, but B has not responded.
create view PendingFriendRequest as
    select * from Friends a where not exists
        (select 1 from Friends b where
            a.personOne = b.personTwo
            and a.personTwo = b.personOne);

create table Message (
    id integer primary key autoincrement,
    sender text not null references Person(guid)
        on delete cascade,
    recipient text not null references Person(guid)
        on delete cascade,
    message text not null
);

create table Location (
    id integer primary key autoincrement,
    name text not null unique,
    electricityPrice real,
    -- A location has exactly one owner, but there can be
    -- any number of persons who are interested in a location's
    -- energy consumption. (for example, family members may want
    -- to know if they left the lights on, etc.)
    owner text references Person(guid) on delete set null
);

-- "Person A has location B" is an N:N relation, so we
-- need a separate table to store its tuples.
create table HasLocation (
    personGuid text not null references Person(guid)
        on delete cascade,
    locationId integer not null references Location(id),
    primary key (personGuid, locationId)
);

create table Sensor (
    id integer primary key autoincrement,
    locationid integer not null references Location(id),
    title text not null,
    description text not null,
    notes text,
    unique (locationid, title)
);

create table ElectricityPrice (
    locationId integer not null references Location(id),
    price real
);

create table SensorTag (
    sensorId integer not null references Sensor(id),
    tag text not null,
    primary key (sensorId, tag)
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
--     create unique index hourIndex
--         ON hourAverage (unixtime, sensorId)

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

create table MonthAverage (
    sensorId integer not null references Sensor(id),
    -- Unix time for YYYY-MM-00 00:00. Identifies the month.
    unixtime integer not null,
    -- Note that this measurement can be null.
    -- This allows us to represent a lack
    -- of measurements to aggregate.
    measured real,
    -- Aggregate
    notes text,
    primary key (sensorId, unixtime)
);

create table YearAverage (
    sensorId integer not null references Sensor(id),
    -- Unix time for YYYY-00-00 00:00. Identifies the month.
    unixtime integer not null,
    -- Note that this measurement can be null.
    -- This allows us to represent a lack
    -- of measurements to aggregate.
    measured real,
    -- Aggregate
    notes text,
    primary key (sensorId, unixtime)
);

-- A table that describes periods of time during
-- which no more measurements can be inserted.
-- This is used to make sure that measurements
-- are not meddled with, once the original
-- measurements have been discarded in favor
-- of aggregate measurements.
-- These periods of time shouldn't overlap,
-- and the server makes sure they don't.
create table FrozenPeriod (
    -- The start-time of the period, in
    -- unix time.
    startTime integer not null,
    -- The end-time of the period, in
    -- unix time.
    endTime integer not null,
    -- An integer that identifies this period's
    -- "compaction level," i.e. the degree to which
    -- measurements have been compacted. The following
    -- compaction levels are legal:
    --
    --     0 - No compaction
    --     1 - Measurement compaction
    --     2 - Hour-average compaction
    --     3 - Day-average compaction
    compactionLevel integer not null,
    primary key (startTime, endTime)
);
