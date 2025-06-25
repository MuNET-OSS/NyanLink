# WorldLink

WorldLink maimai DX Online C2C Multiplayer Mod

<img width="613" alt="image" src="https://github.com/user-attachments/assets/bd53ab9d-49f8-46ac-b51b-9a5de00ef7e1" />

> [!WARNING]
> This mod is in public testing, please report any bugs you find.

## Setting Up WorldLink

### On PC & Controller

1. Install [MelonLoader](https://github.com/LavaGang/MelonLoader#install)
2. Download [WorldLink.dll](https://github.com/MewoLab/worldlinkd/releases/latest/download/WorldLink.dll), put it in Mods
3. Download [WorldLink.toml](https://github.com/MewoLab/worldlinkd/blob/main/mod/WorldLink.toml)
4. Edit `WorldLink.toml` to select a lobby server
   - **LobbyUrl**: Required. Choose from `{asia, euro, usw, use, cn}.link.aquadx.net`
   - **RelayUrl**: Optional. Custom relay server URL (e.g., `"myrelay.example.com:20101"`). If not specified, uses relay info from lobby server
5. Start the game

### On KanadeDX

1. Go to launcher settings in the top right
2. Click on the "mods" tab
3. Scroll down to find WorldLink, enable it
4. Fill in the WorldLink URL
5. Start the game

## How to Play

1. You and your friend both connect to the same lobby
2. One person select a song and difficulty, scroll to the left, and click recruit
3. The other person join in the song-select menu

## Running a Server

If you want to self-host a server to play with your friends, read the [Self Host Guide](README.HOST.md).

## Developer Guide

This project consists of two main components:
- **Server** (Kotlin/Ktor) - Handles multiplayer functionality
- **Client Mod** (C#) - DLL for the MAI2 game

### Prerequisites

- **Java 17+** for building the server
- **.NET Framework 4.7.2** for building the client mod
- **MAI2 game DLLs** (see below)

### Building the Server

The server is a Kotlin application using Ktor framework.

```bash
# Build the server (excluding tests)
./gradlew build -x test

# The JAR file will be created at:
# build/libs/worldlinkd.jar
```

### Building the Client Mod

The client mod requires game-specific DLLs that are not included in the repository.

#### 1. Required Game DLLs

You need to obtain these DLLs from your MAI2 installation:
- `mod/Libs/Assembly-CSharp.dll` - Main game assembly
- `mod/Libs/Assembly-CSharp-firstpass.dll` - Game assembly (first pass)
- `mod/Libs/AMDaemon.NET.dll` - AMDaemon library

#### 2. Build the Mod

```bash
# Navigate to the mod directory
cd mod

# Build the mod
dotnet build

# The DLL will be created at:
# bin/Debug/net472/WorldLink.dll
```

#### 3. Installation

Copy the built `WorldLink.dll` to your MAI2 game's `Mods` folder.

### Development Workflow

1. **Server Development:**
   - Modify server code in `src/main/kotlin/`
   - Run `./gradlew build -x test` to rebuild
   - Test with `java -jar build/libs/worldlinkd.jar`

2. **Client Mod Development:**
   - Modify mod code in `mod/WorldLink/`
   - Run `dotnet build` from the `mod/` directory
   - Copy the new DLL to your game's Mods folder
   - Restart the game to test changes

### Project Structure

```
worldlinkd/
├── src/main/kotlin/          # Server source code
│   ├── Application.kt        # Main server application
│   ├── FutariLobby.kt       # Lobby server logic
│   ├── FutariRelay.kt       # Relay server logic
│   └── FutariTypes.kt       # Data structures
├── mod/                     # Client mod source code
│   ├── WorldLink/           # Main mod code
│   │   ├── FutariPatch.cs   # Main patch logic
│   │   ├── FutariClient.cs  # Client communication
│   │   ├── FutariExt.cs     # Utility extensions
│   │   └── FutariTypes.cs   # Data structures
│   ├── Libs/                # Game DLLs (not included)
│   └── WorldLink.csproj     # Mod project file
└── build.gradle.kts         # Server build configuration
```

### Troubleshooting

#### Build Errors

- **Missing DLLs:** Ensure all required game DLLs are in `mod/Libs/`
- **Java version:** Make sure you're using Java 17 or higher
- **.NET Framework:** Ensure .NET Framework 4.7.2 is installed

#### Runtime Issues

- **Server won't start:** Check if port 20100 is available
- **Mod not loading:** Verify the DLL is in the correct Mods folder and do not use dirty packs(脏脏包)
- **Connection issues:** Check firewall settings and server configuration

For more detailed information about the modding process, see the [Self Host Guide](README.HOST.md).
