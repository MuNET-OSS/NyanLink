#!/usr/bin/env bash
sudo apt install -y debian-keyring debian-archive-keyring apt-transport-https curl
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | sudo gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | sudo tee /etc/apt/sources.list.d/caddy-stable.list
sudo apt update
sudo apt install caddy

systemctl enable caddy --now
ufw allow 80 443

# Ask user to input domain name
echo "Enter your domain name (e.g. example.com):"
read DOMAIN_NAME

# Create Caddyfile
echo "Creating Caddyfile..."
cat <<EOF > /etc/caddy/Caddyfile
${DOMAIN_NAME} {
    reverse_proxy localhost:20100
}
EOF
