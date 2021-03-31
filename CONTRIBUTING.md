# Contributing

## Setting up development environment
Setting up development environment for compilation:

1. Download [BepInExPack for Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) and extract the contents of the BepInExPack_Valheim folder of the zip file into your root Valheim directory.
2. Download [CabbageCros's AssemblyPublicizer](https://github.com/CabbageCrow/AssemblyPublicizer/releases/tag/v1.1.0) and extract the AssemblyPublicizer.exe to a folder. Drag the assembly_*.dll files in your Valheim install\valheim_Data\Managed folder onto the AssemblyPublicizer.exe. It will automatically create the publicized dll's in the correct folder.
3. Create a new props file `Environment.props` alongside the solution file to define some properties local to your system. Paste this snippet and configure the paths as they are present on your computer.
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- Needs to be your path to the base Valheim folder -->
    <VALHEIM_INSTALL>X:\SteamLibrary\steamapps\common\Valheim</VALHEIM_INSTALL>
  </PropertyGroup>
</Project>
```
4. Create a new file `JotunnLib.csproj.user` in the JotunnLib folder. Paste this snippet and configure the start arguments to your needs.
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Condition="'$(VALHEIM_INSTALL)' == ''" Project="..\Environment.props" />
  <PropertyGroup Condition="'$(Configuration)|$(Platform)' == 'Debug|AnyCPU' And '$(ProjectName)' == 'JotunnLib'">
    <StartAction>Program</StartAction>
    <!-- Start valheim.exe after building Debug-->
    <StartProgram>$(VALHEIM_INSTALL)\valheim.exe</StartProgram>
    <!-- If you want to connect to a server automatically, add '+connect <ip-address>:<port>' as StartArguments -->
    <StartArguments>+console</StartArguments>
    <!-- Alternatively run Steam.exe opening Valheim after building debug -->
    <!-- Needs to be your local path to the Steam client -->
    <!-- StartProgram>C:\Program Files %28x86%29\Steam\steam.exe</StartProgram -->
    <!-- StartArguments>-applaunch 892970</StartArguments -->
  </PropertyGroup>
</Project>
```

## Code formatting
WIP