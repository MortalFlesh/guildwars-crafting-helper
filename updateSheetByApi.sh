#!/bin/bash

set -e

fake build target run

php src/Sheets/Wrapper/app.php
