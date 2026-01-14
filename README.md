# Tag Hierarchy Manager
An application for managing tree hierarchies for music tags, focusing on the MusicBee tag hierarchy template format.

<img width="2126" height="1620" alt="image" src="https://github.com/user-attachments/assets/4f004620-ab60-4bc2-b8c8-bfa22fad5be1" />

## Requirements
- .NET 10.0
- Windows
  - tested on Windows 11 25H2, on may work on other OSes supported by .NET 10.0, you're on your own there.

## Development
JetBrains Rider is strongly recommended for developing for this project, but Visual Studio Code/VS Codium should work too. Generally, anything that allows you to work with Visual Studio solutions.

### Dependencies
- Avalonia
- Serilog (for logging/debugging)
- NUnit (for tests)

### Building
Build with the `dotnet` command line app:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfContained=true -p:DebugType=None -p:DebugSymbols=false
```

Should work on Linux and Mac too (just replace win-x64 with the equivalent for whichever OS) but it's untested on those operating systems.

## TODO
### Low priority
- Refactoring of hierarchy view to make things snappier
