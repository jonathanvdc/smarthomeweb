-- In the database, everything is stored in kW and kWh.

create table Person (
    id integer primary key autoincrement,
    name text not null
);

create table Friendship (
    person1 integer not null references Person(id),
    person2 integer not null references Person(id),
    primary key (person1, person2)
);

create table Location (
    id integer primary key autoincrement,
    name text not null,
    electricityCost real -- in EUR/(kWh)
);

create table HasLocation (
    personId integer not null references Person(id),
    locationId integer not null references Location(id),
    primary key (personId, locationId)
);

create table Sensor (
    id integer primary key autoincrement,
    name text not null,
    description text,
    notes text,
    locationId integer not null references Location(id)
);

create table SensorTag (
    sensorId integer not null references Sensor(id),
    tag text not null,
    primary key (sensorId, tag)
);

create table SensorDataPoint (
    unixTime integer not null,
    dataValue integer not null,
    sensorId integer not null references Sensor(id),
    primary key (sensorId, unixTime)
);

create table SensorEvent (
    unixTime integer not null,
    description text not null,
    sensorId integer not null references Sensor(id),
    primary key (sensorId, unixTime)
);

insert into Person(name) values
    ("Jon Sneyers"), ("Bart Goethals");

insert into Location(name) values
    ("De G0.10");

insert into HasLocation(personId, locationId)
    select Person.id, Location.id
    from Person, Location;

insert into Sensor(name, locationId)
    select "Sensor 1", id
    from Location
    where name = "De G0.10";
