#!/bin/bash

set -e

#fake build

# --------------------

export LIST_NAME="Bank"

echo "=========================="
echo $LIST_NAME
echo "=========================="
echo

dotnet src/App/bin/Release/netcoreapp2.1/App.dll bank
php src/Sheets/Wrapper/app.php

# --------------------

# export LIST_NAME="Inventory Bags"
# export CHECKLIST=".inventory-bags.json"
#
# echo "=========================="
# echo $LIST_NAME
# echo "=========================="
# echo
#
# dotnet src/App/bin/Release/netcoreapp2.1/App.dll
# php src/Sheets/Wrapper/app.php

# --------------------

export LIST_NAME="PVP armor (legendary armor)"
export CHECKLIST=".check-pvp-armor.json"

echo "=========================="
echo $LIST_NAME
echo "=========================="
echo

dotnet src/App/bin/Release/netcoreapp2.1/App.dll
php src/Sheets/Wrapper/app.php

# --------------------

export LIST_NAME="Winters Presence"
export CHECKLIST=".check-winters-presence.json"

echo "=========================="
echo $LIST_NAME
echo "=========================="
echo

dotnet src/App/bin/Release/netcoreapp2.1/App.dll
php src/Sheets/Wrapper/app.php

# --------------------

export LIST_NAME="Vision (legendary trinket)"
export CHECKLIST=".check-vision.json"

echo "=========================="
echo $LIST_NAME
echo "=========================="
echo

dotnet src/App/bin/Release/netcoreapp2.1/App.dll
php src/Sheets/Wrapper/app.php
