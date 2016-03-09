#!/usr/bin/env bash
rm -f smarthomeweb.db
sqlite3 smarthomeweb.db < smarthomeweb.sql
