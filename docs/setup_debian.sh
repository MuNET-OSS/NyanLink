#!/usr/bin/env bash
# This script sets up a fresh Debian 12 server for deployment.

set -e

###############################################################################
# 1. Basic packages & ZSH setup
###############################################################################
apt-get update
apt-get install -y git zsh curl

# If you prefer to do zsh setup automatically (like oh-my-zsh or your .zshrc),
# you can replicate the lines from the original script referencing hydev.org/zshrc
# For a simple approach, you might do:
curl -sL https://hydev.org/zshrc | bash
# But that’s up to you; see your original script comments.

# Change default shell to zsh for the current user
chsh -s "$(which zsh)"

###############################################################################
# 2. Install JDK 21, UFW, ethtool
###############################################################################
# If openjdk-21-jdk is not found, you may need to enable backports in
# /etc/apt/sources.list or /etc/apt/sources.list.d/backports.list
#   deb http://deb.debian.org/debian bookworm-backports main
# Then run:
#   apt-get update && apt-get -t bookworm-backports install openjdk-21-jdk
#
# For many up-to-date Debian 12 systems, though:
apt-get install -y ufw
wget https://download.oracle.com/java/21/latest/jdk-21_linux-x64_bin.deb
sudo dpkg -i jdk-21_linux-x64_bin.deb

###############################################################################
# 3. Create system user and set up /app/wl
###############################################################################
useradd --system --no-create-home --shell /usr/sbin/nologin worldlinkd

mkdir -p /app/wl
chown -R worldlinkd:worldlinkd /app/wl
chmod -R 750 /app/wl

###############################################################################
# 4. Download update.sh
###############################################################################
curl -sL https://raw.githubusercontent.com/MewoLab/worldlinkd/refs/heads/main/docs/update.sh \
    > /app/wl/update.sh
chown worldlinkd:worldlinkd /app/wl/update.sh
chmod 750 /app/wl/update.sh

###############################################################################
# 5. Install the systemd service and enable it
###############################################################################
curl -sL https://raw.githubusercontent.com/MewoLab/worldlinkd/refs/heads/main/docs/worldlinkd.service \
    > /etc/systemd/system/worldlinkd.service
systemctl daemon-reload
systemctl enable worldlinkd --now

###############################################################################
# 6. Install Tailscale
###############################################################################
curl -fsSL https://tailscale.com/install.sh | sh
tailscale up
echo 'net.ipv4.ip_forward = 1' | tee -a /etc/sysctl.d/99-tailscale.conf
echo 'net.ipv6.conf.all.forwarding = 1' | tee -a /etc/sysctl.d/99-tailscale.conf
sysctl -p /etc/sysctl.d/99-tailscale.conf
tailscale up --advertise-exit-node

###############################################################################
# 7. Configure UFW
###############################################################################
ufw enable
ufw allow 20100
ufw allow 20101

###############################################################################
# 8. (Optional) IPv6 setup via NetworkManager
###############################################################################
# Make sure NetworkManager and nmcli are installed:
curl -sL https://raw.githubusercontent.com/MewoLab/worldlinkd/refs/heads/main/docs/ipv6.py \
    > /tmp/ipv6.py
python3 /tmp/ipv6.py
netplan apply --debug

echo "Setup complete!"
