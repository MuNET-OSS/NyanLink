#!/usr/bin/env python3
import yaml
import ipaddress
import sys
import os

FILE_PATH = '/etc/netplan/50-cloud-init.yaml'

def load_yaml_file(path):
    with open(path, 'r') as f:
        return yaml.safe_load(f)

def save_yaml_file(path, data):
    with open(path, 'w') as f:
        yaml.dump(data, f, default_flow_style=False)

def check_ipv6_exists(addresses):
    """Return True if any address in the list is an IPv6 address."""
    for addr in addresses:
        try:
            # Parse the network; note: strict=False accepts host bits.
            net = ipaddress.ip_network(addr, strict=False)
            if net.version == 6:
                return True
        except ValueError:
            continue
    return False

def main():
    # Ensure the file exists
    if not os.path.exists(FILE_PATH):
        print(f"Error: File {FILE_PATH} does not exist.")
        sys.exit(1)

    # Load the existing YAML configuration
    try:
        data = load_yaml_file(FILE_PATH)
    except Exception as e:
        print(f"Error loading YAML file: {e}")
        sys.exit(1)

    # Navigate to the eth0 configuration block
    try:
        eth0 = data['network']['ethernets']['eth0']
    except KeyError:
        print("Error: Cannot find 'eth0' configuration in the YAML file.")
        sys.exit(1)

    # Check if an IPv6 address is already present in the addresses list
    addresses = eth0.get('addresses', [])
    if check_ipv6_exists(addresses):
        print("An IPv6 address is already configured. Exiting.")
        sys.exit(0)

    # Prompt the user for IPv6 address and gateway
    ipv6_addr = input("Enter the IPv6 address (without /64): ").strip()
    gateway = input("Enter the IPv6 gateway: ").strip()

    # Append the new IPv6 address to the addresses list (using /64)
    new_ipv6 = f"{ipv6_addr}/64"
    addresses.insert(0, new_ipv6)
    eth0['addresses'] = addresses

    # Add a new IPv6 default route (destination ::/0)
    # Create the routes list if it does not exist
    routes = eth0.get('routes', [])
    new_route = {'to': '::/0', 'via': gateway}
    routes.insert(0, new_route)
    eth0['routes'] = routes

    # Save the modified configuration back to the file
    try:
        save_yaml_file(FILE_PATH, data)
        print("IPv6 configuration added successfully.")
    except Exception as e:
        print(f"Error saving YAML file: {e}")
        sys.exit(1)

if __name__ == '__main__':
    main()
