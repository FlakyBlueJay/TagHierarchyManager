# Tag Hierarchy Manager
An application for managing tree hierarchies for music tags, focusing on the MusicBee tag hierarchy template format.

## Requirements
- .NET 9.0

## Development
JetBrains Rider is recommended for developing for this project, but Visual Studio Code/VS Codium should work too.

### Dependencies
- Terminal.Gui
- Serilog (for logging/debugging)
- NUnit (for tests)

### Building
Build with the `dotnet` command line app:

Linux: `dotnet publish -c Release -p:PublishProfile=Linux`
Windows: `dotnet publish -c Release -p:PublishProfile=Linux`
