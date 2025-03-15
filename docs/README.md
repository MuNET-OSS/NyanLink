These files are mostly for private use, and I put them there so that I don't forget how I deployed my servers :3

## Setup Debian

```sh
bash <(curl -sL https://raw.githubusercontent.com/MewoLab/worldlinkd/refs/heads/main/docs/setup_debian.sh)
```

## Setup Caddy for WorldLink HTTPS

Depends on Setup Debian

```sh
bash <(curl -sL https://raw.githubusercontent.com/MewoLab/worldlinkd/refs/heads/main/docs/setup_caddy.sh)
```

## Setup rathole for AquaDX reverse proxy

Depends on Setup Caddy

```sh
bash <(curl -sL https://raw.githubusercontent.com/MewoLab/worldlinkd/refs/heads/main/docs/setup_rathole.sh)
```

(Then you copy the token and write this client rathole toml)

```toml
[client]
remote_addr = "{host}:18199"
default_token = "{token}"

[client.services.allnet]
local_addr = "localhost:80"

[client.services.billing]
local_addr = "localhost:8443"

[client.services.aimedb]
local_addr = "localhost:22345"
```
