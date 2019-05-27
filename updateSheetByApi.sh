#!/bin/bash

set -e

#fake build

# --------------------

export LIST_NAME="Bank"

echo $LIST_NAME
echo "=========================="
echo

dotnet src/App/bin/Release/netcoreapp2.1/App.dll bank
php src/Sheets/Wrapper/app.php

# --------------------

export LIST_NAME="Ascension (legendary backpack)"
export CHECKLIST=".check-ascension.json"

echo $LIST_NAME
echo "=========================="
echo

dotnet src/App/bin/Release/netcoreapp2.1/App.dll
php src/Sheets/Wrapper/app.php

# --------------------

export LIST_NAME="Aurora (legendary trinket)"
export CHECKLIST=".check-aurora.json"

echo $LIST_NAME
echo "=========================="
echo

dotnet src/App/bin/Release/netcoreapp2.1/App.dll
php src/Sheets/Wrapper/app.php

# --------------------

export LIST_NAME="PVP armor (legendary armor)"
export CHECKLIST=".check-pvp-armor.json"

echo $LIST_NAME
echo "=========================="
echo

dotnet src/App/bin/Release/netcoreapp2.1/App.dll
php src/Sheets/Wrapper/app.php
