# ZSocket2Fix
Valheim over plain TCP

For developer experimental use only. Only use if you know what you are doing.

Useful for analyzing game traffic through external network tools, i.e Wireshark.

## MSVC Installation / Setup "hell"
- MSVC Installer 
    install .NET 4.8 targeting pack

- Drag BepinEx to Valheim installer folder (Denikson)

- MSVC Project
    - Right-click project 'ZSocket2Fix'

    - Manage NuGet Packages...
        - Then update all

    - After Installing/Updating all packages, it should automatically resolve all red squiggles
      - If getting a "Dual dependency" warning / error, remove those conflicting packages from Nuget (means that these packages are already in the Valheim installation, no need to directly depend)
