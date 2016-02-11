create table Person (
    id integer primary key autoincrement,
    name text not null
);

create table Location (
    id integer primary key autoincrement,
    name text not null
);

create table HasLocation (
    personId integer not null references Person(id),
    locationId integer not null references Location(id),
    primary key (personId, locationId)
);

create table Sensor (
    id integer primary key autoincrement,
    name text not null,
    locationId integer not null references Location(id)
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
