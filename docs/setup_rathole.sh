#!/usr/bin/env bash
# This script is used to setup rathole

set -e

# Check if running as root
if [ "$(id -u)" -ne 0 ]; then
    echo "Please run as root"
    exit 1
fi

# Make dir /app/rathole if doesn't exist
mkdir -p /app/rathole
cd /app/rathole

# Install wget and unzip if not found
if ! command -v wget &> /dev/null; then
    apt-get update && apt-get install -y wget
fi

if ! command -v unzip &> /dev/null; then
    apt-get update && apt-get install -y unzip
fi

# Download binary
wget "https://github.com/rapiz1/rathole/releases/latest/download/rathole-x86_64-unknown-linux-gnu.zip" -O rathole.zip
unzip rathole.zip
rm rathole.zip

# Add systemd service
cat <<EOF > /etc/systemd/system/ratholes@.service
[Unit]
Description=Rathole Server Service
After=network.target

[Service]
Type=simple
Restart=on-failure
RestartSec=5s
LimitNOFILE=1048576
ExecStart=/app/rathole/rathole -s /app/rathole/%i.toml

DynamicUser=yes
NoNewPrivileges=yes
PrivateTmp=yes
PrivateDevices=yes
DevicePolicy=closed
ProtectSystem=strict
ProtectHome=read-only
ProtectControlGroups=yes
ProtectKernelModules=yes
ProtectKernelTunables=yes
RestrictAddressFamilies=AF_UNIX AF_INET AF_INET6 AF_NETLINK
RestrictNamespaces=yes
RestrictRealtime=yes
RestrictSUIDSGID=yes
# MemoryDenyWriteExecute=yes
LockPersonality=yes
AmbientCapabilities=CAP_NET_BIND_SERVICE

[Install]
WantedBy=multi-user.target
EOF

# If the config doesn't exist, generate a new one
if [ ! -f /app/rathole/aquadx.toml ]; then
    # Generate a len-64 random string for the token
    token=$(head -c 64 /dev/urandom | base64 | tr -d '\n')
    echo "Generated token: $token"
    
    # Write the config file
    cat <<EOF > /app/rathole/aquadx.toml
[server]
bind_addr = "0.0.0.0:18199"
default_token = "$token"

[server.services.allnet]
bind_addr = "127.0.0.1:8092"

[server.services.billing]
bind_addr = "0.0.0.0:8443"

[server.services.aimedb]
bind_addr = "0.0.0.0:22345"
EOF
fi

# Reload systemd to apply the new service
sudo systemctl daemon-reload
sudo systemctl enable ratholes@aquadx --now
sudo systemctl restart ratholes@aquadx

# Add to caddyfile if the "# Managed by setup_rathole.sh script" line is found
if grep -q "# Managed by setup_rathole.sh script" /etc/caddy/Caddyfile; then
    cat <<EOF >> /etc/caddy/Caddyfile
# Managed by setup_rathole.sh script
http:// {
    reverse_proxy  localhost:8092
}
# !Managed
EOF

