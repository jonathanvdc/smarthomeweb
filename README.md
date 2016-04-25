# SmartHome website
Project for the 2015–2016 *Programming Project Databases* course at the University of Antwerp.

## Build Status

[![Build Status](https://travis-ci.org/jonathanvdc/smarthomeweb.svg?branch=master)](https://travis-ci.org/jonathanvdc/smarthomeweb)

## Team
* Sibert Aerts
* Ken Bauwens
* Pieter Hendriks
* Jonathan Van der Cruysse
* Mauris Van Hauwe

## Getting SmartHomeWeb up-and-running

### Prerequisites

To get SmartHomeWeb to work, you'll need the following:

* A CLR implementation. We recommend .NET on Windows and Mono on Linux/Mac.
* A C# 6.0 compiler: a recent version of `csc` for the .NET framework, or `mcs` for Mono.
* An MSBuild-compatible build system: `msbuild` for the .NET framework, or `xbuild` for Mono.
* A Python interpreter.
* The `requests` Python module.
* SQLite3

#### Windows

Recent versions of Windows ship with the .NET framework pre-installed. `csc` and `msbuild` are bundled with Visual Studio.

#### Linux

On Debian-based Linux distributions, punching in the following command should install Mono, `xbuild`, `mcs`, `sqlite3` and the `requests` module:

```bash
$ sudo apt-get install mono-complete mono-devel sqlite3 python-requests
```

[Install Mono on Linux](http://www.mono-project.com/docs/getting-started/install/linux/) provides a more detailed guide on how to install Mono.

#### Mac OS X

The server has not been tested on Mac OS X, so we can't certify it for that platform. Due to OS X's similarity to Linux, however, running it on Mac OS X may be worth a try.

### Step one: compiling SmartHomeWeb

#### Windows

One option is opening SmartHomeWeb `backend/SmartHomeWeb/SmartHomeWeb.sln` in the Visual Studio GUI, and compiling it in _Release_ mode from there. Alternatively, you can issue the following command.

```bash
$ msbuild /p:Configuration=Release backend\SmartHomeWeb\SmartHomeWeb.sln
```

#### Mono

Analogous to the Windows shell command:

```bash
$ xbuild /p:Configuration=Release backend/SmartHomeWeb/SmartHomeWeb.sln
```

### Step two: initializing the database

The following command should fill the database nicely, where `30` is the number of days you'd like to generate measurements for:

```bash
$ python init-db.py 30
```

### Step three: running the server

The server can be started with the following command:

Windows:

```bash
$ backend\SmartHomeWeb\SmartHomeWeb\bin\Release\SmartHomeWeb.exe
```

Mono:

```bash
$ mono ./backend/SmartHomeWeb/SmartHomeWeb/bin/Release/SmartHomeWeb.exe
```

__Pro tip__: the server uses port TCP port _8088_ for all of its communication, so it can be killed by the command below if its process is accidentally detached from the current terminal.

```bash
$ fuser -k -n tcp 8088
```
