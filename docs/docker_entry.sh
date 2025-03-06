#!/usr/bin/env bash

set -e
bash /app/wl/update.sh
java -jar /app/wl/worldlinkd.jar
