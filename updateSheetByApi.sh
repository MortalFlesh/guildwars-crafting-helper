#!/bin/bash

set -e

#fake build

# --------------------

#export LIST_NAME="Shining blade (lengendary sword)"
#export CHECKLIST=".check-shining-blade.json"

#echo $LIST_NAME
#echo "=========================="
#echo

#dotnet src/App/bin/Release/netcoreapp2.1/App.dll
#php src/Sheets/Wrapper/app.php

# --------------------

export LIST_NAME="Bank"

echo $LIST_NAME
echo "=========================="
echo

dotnet src/App/bin/Release/netcoreapp2.1/App.dll bank
php src/Sheets/Wrapper/app.php

# --------------------

export LIST_NAME="Frostfang (legendary axe)"
export CHECKLIST=".check-frostfang.json"

echo $LIST_NAME
echo "=========================="
echo

dotnet src/App/bin/Release/netcoreapp2.1/App.dll
php src/Sheets/Wrapper/app.php

# --------------------

export LIST_NAME="Incinerator (legendary dagger)"
export CHECKLIST=".check-incinerator.json"

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
