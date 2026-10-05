# Avalox

> **Project Status:** Completed academic coursework project. Archived and no longer actively maintained.

A lightweight cross-platform desktop TCP chat client for local networks, developed as the client application for a college coursework project.

## Overview

Avalox connects to a dedicated [AvaloxServer](https://github.com/wellmq/AvaloxServer) over raw TCP sockets using a custom binary framing protocol:

$$\text{[ 1 byte: Type ]} + \text{[ 4 bytes: Length ]} + \text{[ JSON Payload ]}$$

- **Runtime:** .NET 10.0 (C# 13)
- **UI Framework:** [Avalonia UI 12](https://avaloniaui.net/) (Linux, Windows, macOS)
- **Architecture:** MVVM (`CommunityToolkit.Mvvm`)
- **Networking:** Async sockets via `TcpClient` with `System.Threading.Channels` for packet sequencing

## Screenshots

<div align="center">
  <img src="Previews/chat_page_preview.png" width="800" alt="Avalox Chat Preview" />
  <br/><br/>
  <img src="Previews/login_page_preview.png" width="800" alt="Avalox Login Preview" />
</div>

## Build & Run

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Run from Source

```bash
git clone https://github.com/wellmq/Avalox.git
cd Avalox
dotnet run
```

### Build Release Binary

```bash
dotnet build -c Release
```

The compiled binaries will be located in `bin/Release/net10.0/`.

## License

[MIT](LICENSE)
