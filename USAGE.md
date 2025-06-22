# WorldLink Server Usage Guide

## Port Configuration

The WorldLink server uses two ports that can be configured in multiple ways:

### **Default Ports:**
- **Lobby Port**: 20100 (HTTP API server)
- **Relay Port**: 20101 (Game communication server)
- (Change default ports in Application.kt and FutariLobby.kt, I made these changes cuz the cloud provider is not really giving me 20100 and 20101)
- (NOT the best solution, still trying to make it pretty, but it is what it is for now)

## Configuration Methods

### **1. Command Line Arguments (Recommended)**

```bash
# Start with custom ports
java -jar build/libs/worldlinkd.jar --lobby-port 20100 --relay-port 20101

# Start with only lobby port (relay uses default)
java -jar build/libs/worldlinkd.jar --lobby-port 20100

# Start with only relay port (lobby uses default)
java -jar build/libs/worldlinkd.jar --relay-port 20101
```

### **2. Environment Variables**

```bash
# Set environment variables
export LOBBY_PORT=20100
export RELAY_PORT=20101

# Start the server
java -jar build/libs/worldlinkd.jar
```

### **3. Windows Environment Variables**

```cmd
# Set environment variables
set LOBBY_PORT=20100
set RELAY_PORT=20101

# Start the server
java -jar build\libs\worldlinkd.jar
```

## Priority Order

1. **Command line arguments** (highest priority)
2. **Environment variables** (medium priority)
3. **Default values** (lowest priority)

## Examples

### **Example 1: Quick Test with Default Ports**
```bash
java -jar build/libs/worldlinkd.jar
```

### **Example 2: Custom Ports for Cloud Server**
```bash
java -jar build/libs/worldlinkd.jar --lobby-port 11451 --relay-port 19198
```

### **Example 4: Using Environment Variables**
```bash
export LOBBY_PORT=20100
export RELAY_PORT=20101
java -jar build/libs/worldlinkd.jar
```

## Client Configuration

After starting the server with custom ports, update your `WorldLink.toml`:

```toml
# Point to your server with the lobby port
LobbyUrl="http://YOUR_SERVER_IP:YOUR_LOBBY_PORT"
```

## Troubleshooting

### **Port Already in Use**
If you get a "port already in use" error:
```bash
# Check what's using the port
netstat -an | grep :8080

# Use a different port
java -jar build/libs/worldlinkd.jar --lobby-port 8081 --relay-port 8082
```

### **Firewall Issues**
Make sure your firewall allows:
- TCP port for lobby server
- TCP port for relay server
- UDP ports (dynamic, for peer-to-peer)

### **Client Can't Connect**
1. Verify the server is running: `netstat -an | grep :YOUR_LOBBY_PORT`
2. Check your `WorldLink.toml` configuration
3. Ensure the lobby port in the URL matches your server configuration 