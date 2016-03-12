create table Person (
	id integer primary key autoincrement,
	username text not null,
	name text,
	password text not null, --obviously niet plaintext
	--users require logging in, non-user persons shouldn't be stored so password and username are required.
	--username can differ from actual name, actual name can be null.
	--If no name is specified, username will be displayed in social aspects.

	--Assignment describes selecting users through address and such, so we will store additional info:
	birthdate integer not null, --in unixtime
	address text not null, --Street and number
	city text not null,
	zipcode text not null --Not all countries have number-only zip codes.


	--Can probably be expanded further, I don't know what other details would be useful though.

);
create table Friends (
	personOne integer not null references Person(id),
	personTwo integer not null references Person(id),
	primary key (personOne, personTwo) --How do we enforce that the reversed pair is not present?
);

create table Message (
	id integer primary key autoincrement,
	sender integer not null references Person(id),
	recipient integer not null references Person(id),
	message text not null
	--'usage messages' as described in assignment can simply insert a link into message,
	--Though perhaps allowing an attached link is better? (Optional, obviously)
	--link text
);

create table Location (
	id integer primary key autoincrement,
	name text not null
);

--Reverse index? See example @ hourAverages

create table HasLocation (
	personId integer not null references Person(id),
	locationId integer not null references Location(id),
	primary key (personId, locationId)
);

create table Sensor (
	id integer primary key autoincrement,
	locationid integer not null references Location(id),
	title text not null,
	description text not null,
	notes text
);

create table ElectricityPrice (
	locationId integer not null references Location(id), --One location has one price.
	--Restriction on users should be done through HasLocation table, no point doing that again here.
	price real
);

--Might wanna think about reverse index here too, see example @ hourAverage
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
	unixtime integer not null, --solves timezone issues etc
	measured real not null,
	notes text,
	primary key (sensorId, unixtime)
);

--Adding indexes on (unixtime, sensorId) would probably be useful in these tables, to optimize the other search case.
--example:
--	create unique index hourIndex ON hourAverage (unixtime, sensorId)
create table HourAverage (
	sensorId integer not null references Sensor(id),
	unixtime integer not null, --unix time for the hour, YYYY-MM-DD XX:00:00, where xx in 0-23 (inclusive).
	average real not null,
	notes text, --Notes carry over from measurements, will become CSV in case of multiple.
	--Each comment will be enclosed in double quotes for clear seperation in case commas are present.
	--Double quotes should become illegal characters in the comment though,
	--to ensure no double quotes are present any place but begin and end of a note.
	--Important for parsing the notes back into the frontend.
	--Enclosing in quotes is unnecessary  in the Measurements table they are listed per minute and every user
	--that writes multiple notes in a minute is just being unreasonable.
	primary key (sensorId, unixtime)

);

--Day averages are needed seperately because hours don't get the outliers pruned, day does.
--More than this shouldn't be needed, we can simply grab the records from day table and compute as needed.
create table DayAverages (
	sensorId integer not null references Sensor(id),
	unixtime integer not null, --unix time for YYYY-MM-DD 00:00. Identifies the day.
	average real not null,
	notes text,
	primary key (sensorId, unixtime)
);


--Adding extra for the longer time periods might be advisable though, to save on computing time.
--Requires extra storage, but seems reasonable to include yearly, for example.

create table YearAverages (
	sensorId integer not null references Sensor(id),
	unixtime integer not null, --unix time identifying the year; YYYY-01-01 00:00. only YYYY can vary.
	average real not null,
	notes text,
	primary key (sensorId, unixtime)
);
