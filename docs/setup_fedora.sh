#!/usr/bin/env bash

# This script sets up a fresh Fedora installation for deployment.
# The user is assumed to be root, and the script is tested on Fedora 41.
# 1. Install zsh (These steps need to be done manually for now)
# dnf install git zsh curl -y
# curl -sL https://hydev.org/zshrc | bash
# update-ssh-keys.py

set -e
chsh -s "$(which zsh)"

# 2. Install JDK 21
dnf install java-21-openjdk ufw -y

# 3. Make the user
useradd --system --no-create-home --shell /usr/sbin/nologin worldlinkd
mkdir /app/wl -p
chown -R worldlinkd:worldlinkd /app/wl
chmod -R 750 /app/wl

# 4. Download update.sh
curl -sL https://raw.githubusercontent.com/MewoLab/worldlinkd/refs/heads/main/docs/update.sh > /app/wl/update.sh
chown worldlinkd:worldlinkd /app/wl/update.sh
chmod 750 /app/wl/update.sh

# 5. Install the worldlinkd service
curl -sL https://raw.githubusercontent.com/MewoLab/worldlinkd/refs/heads/main/docs/worldlinkd.service > /etc/systemd/system/worldlinkd.service
systemctl daemon-reload
systemctl enable worldlinkd --now

# 6. Setup tailscale
curl -fsSL https://tailscale.com/install.sh | sh
tailscale up

# 7. Setup firewall
ufw enable
ufw allow 20100
ufw allow 20101

# 8. Install ipv6 support
setup_ipv6() {
    addr="$1"
    gateway="$2"
    nmcli connection modify "cloud-init ens3" ipv6.address "$addr/64"
    nmcli connection modify "cloud-init ens3" ipv6.gateway "$gateway"
    nmcli connection modify "cloud-init ens3" ipv6.dns "2001:4860:4860::8844 2001:4860:4860::8888"
    nmcli connection up "cloud-init ens3"
}
# Prompt user to enter ipv6 address and gateway
echo "Enter your IPv6 address and gateway separated by a space:"
read -r addr gateway
setup_ipv6 "$addr" "$gateway"
