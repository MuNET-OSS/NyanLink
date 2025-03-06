# Self Hosting worldlinkd

`worldlinkd` is the daemon or server component of WorldLink. The below guide will help you self host a server to play with your friend.

## Option 1: Run Natively

1. Install [Java 21](https://download.oracle.com/java/21/archive/jdk-21.0.5_windows-x64_bin.exe)
2. Download [worldlinkd.jar](https://github.com/MewoLab/worldlinkd/releases/latest/download/worldlinkd.jar)
3. Open command prompt in the folder where `worldlinkd.jar` is
4. Run `java -jar worldlinkd.jar`  
  (if you installed other java versions before, make sure this java command points to Java 21)

## Option 2: Using Docker

1. Install [Docker](https://docs.docker.com/get-docker/)
2. Clone / download this repository
3. Open command prompt in this folder
4. Run `docker compose up`
