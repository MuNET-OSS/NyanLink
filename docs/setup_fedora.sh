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
dnf install java-21-openjdk -y

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
