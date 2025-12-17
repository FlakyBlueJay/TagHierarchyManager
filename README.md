# Tag Hierarchy Manager
An application for managing tree hierarchies for music tags, focusing on the MusicBee tag hierarchy template format.
<img width="2717" height="1469" alt="A screenshot of the Tag Hierarchy Manager running on Konsole in Linux, showing the RateYourMusic Genre Hierarchy tree." src="https://github.com/user-attachments/assets/d03e2500-e057-4a3f-bab8-dc8f85246945" />

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

## TODO
- Localisation - use .resx or .po for translatable strings in UI elements.
- Switch to Avalonia
