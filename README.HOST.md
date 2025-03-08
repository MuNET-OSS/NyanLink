# Self Hosting worldlinkd

`worldlinkd` is the daemon or server component of WorldLink. The below guide will help you self host a server to play with your friend.

> [!WARNING]
> This mod does not support ipv6 yet, please use ipv4.

## Option 1: Run Natively

1. Install [Java 21](https://download.oracle.com/java/21/archive/jdk-21.0.5_windows-x64_bin.exe)
2. Download [worldlinkd.jar](https://github.com/MewoLab/worldlinkd/releases/latest/download/worldlinkd.jar)
3. Open command prompt in the folder where `worldlinkd.jar` is
4. Run `java -jar worldlinkd.jar`  
  (if you installed other java versions before, make sure this java command points to Java 21)

## Option 2: Using Docker Compose

1. Install [Docker](https://docs.docker.com/get-docker/)
2. Clone / download this repository
3. Open command prompt in this folder
4. Run `docker compose up`

## Option 3: Using Docker Run

1. Install [Docker](https://docs.docker.com/get-docker/)
2. Run `docker run -d --name worldlinkd --restart unless-stopped -p 20100:20100 -p 20101:20101 aquadx/worldlinkd`

## Connecting

Use the url `http://{your-ip}:20100` to connect.

Make sure you check you can open this url in your phone browser before trying to connect in game. If it can't open, check each of the following:

1. Windows Firewall isn't blocking port 20100 & 20101 (try turning it off).
2. The IP you entered is correct (don't just get any ip from your ipconfig, it should be your WiFi ip, it should most likely start with 192.168. or 10.0. or 100.)
3. The IP is ipv4 (sadly the game doesn't support ipv6 yet).
4. Your router isn't blocking LAN connections.
5. If you want to use public ip instead of LAN ip, make sure you setup port forward on your router.

## Setting Up Reverse Proxy

If you want HTTPS (e.g. `https://{your-domain}`) you can use a reverse proxy like Caddy. Below is an example Caddyfile:

```caddy
worldlinkd.example.com {
  reverse_proxy localhost:20100
}
```

