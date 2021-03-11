#!/usr/bin/env bash

set -e

dist/osx-x64/GuildWarsConsole gw:bank

dist/osx-x64/GuildWarsConsole gw:check \
    configuration/.check-pvp-armor.json \
    configuration/.check-winters-presence.json
