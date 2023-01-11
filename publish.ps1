param(
    [Parameter(Mandatory)]
    [ValidateSet('Debug','Release')]
    [System.String]$Target,
    
    [Parameter(Mandatory)]
    [System.String]$TargetPath,
    
    [Parameter(Mandatory)]
    [System.String]$TargetAssembly,

    [Parameter(Mandatory)]
    [System.String]$ValheimPath,
    
    [Parameter(Mandatory)]
    [System.String]$ProjectPath,
    
    [System.String]$DeployPath
)

# Make sure Get-Location is the script path
Push-Location -Path (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Test some preliminaries
("$TargetPath",
 "$ValheimPath",
 "$ProjectPath"
) | % {
    if (!(Test-Path "$_")) {Write-Error -ErrorAction Stop -Message "$_ folder is missing"}
}

# Go
Write-Host "Publishing for $Target from $TargetPath"

# Plugin name without ".dll"
$name = "$TargetAssembly" -Replace('.dll')

# Create the mdb file
$pdb = "$TargetPath\$name.pdb"
if (Test-Path -Path "$pdb") {
    Write-Host "Create mdb file for plugin $name"
    Invoke-Expression "& `"$(Get-Location)\libraries\Debug\pdb2mdb.exe`" `"$TargetPath\$TargetAssembly`""
}

# Debug copies the dll to Valheim
if ($Target.Equals("Debug")) {
    if ($DeployPath.Equals("")){
      $DeployPath = "$ValheimPath\BepInEx\plugins"
    }
    
    $plug = New-Item -Type Directory -Path "$DeployPath\$name" -Force
    Write-Host "Copy $TargetAssembly to $plug"
    Copy-Item -Path "$TargetPath\$name.dll" -Destination "$plug" -Force
    Copy-Item -Path "$TargetPath\$name.pdb" -Destination "$plug" -Force
    Copy-Item -Path "$TargetPath\$name.xml" -Destination "$plug" -Force -ErrorAction SilentlyContinue
    Copy-Item -Path "$TargetPath\$name.dll.mdb" -Destination "$plug" -Force
    
    $dedi = "$ValheimPath\..\Valheim dedicated server"
    if (Test-Path -Path "$dedi") {
      if (Get-Process -Name 'valheim_server' -ErrorAction Ignore) {
        Write-Host "Dedicated server is running, plugin will not be updated"
      }
      else {
        $dediplug = New-Item -Type Directory -Path "$dedi\BepInEx\plugins\$name" -Force
        Write-Host "Copy $TargetAssembly to $dediplug"
        Copy-Item -Path "$TargetPath\$name.dll" -Destination "$dediplug" -Force
        Copy-Item -Path "$TargetPath\$name.pdb" -Destination "$dediplug" -Force
        Copy-Item -Path "$TargetPath\$name.xml" -Destination "$dediplug" -Force -ErrorAction SilentlyContinue
        Copy-Item -Path "$TargetPath\$name.dll.mdb" -Destination "$dediplug"
      }
    }
}

# Release builds packages for ThunderStore and NexusMods
if($Target.Equals("Release") -and $name.Equals("Jotunn")) {
    $package = "$ProjectPath\_package"
    [xml]$versionxml = Get-Content -Path "$ProjectPath\BuildProps\version.props"
    $version = $versionxml.Project.PropertyGroup.Version
    
    Write-Host "Packaging for ThunderStore"
    New-Item -Type Directory -Path "$package\Thunderstore" -Force
    $thunder = New-Item -Type Directory -Path "$package\Thunderstore\package"
    $thunder.CreateSubdirectory('plugins')
    Copy-Item -Path "$TargetPath\$name.dll" -Destination "$thunder\plugins\"
    Copy-Item -Path "$TargetPath\$name.pdb" -Destination "$thunder\plugins\"
    Copy-Item -Path "$TargetPath\$name.xml" -Destination "$thunder\plugins\"
    Copy-Item -Path "$TargetPath\$name.dll.mdb" -Destination "$thunder\plugins\"
    Copy-Item -Path "$ProjectPath\..\README.md" -Destination "$thunder\"
    Copy-Item -Path "$ProjectPath\..\CHANGELOG.md" -Destination "$thunder\"
    Copy-Item -Path "$ProjectPath\manifest.json" -Destination "$thunder\manifest.json"
    Remove-Item -Path "$package\Thunderstore\$name-$version.zip" -Force
    Copy-Item -Path "$(Get-Location)\resources\JVL_Logo_256x256.png" -Destination "$thunder\icon.png"
    Invoke-Expression "& `"$(Get-Location)\libraries\7za.exe`" a `"$package\Thunderstore\$name-$version.zip`" `"$thunder\*`""
    $thunder.Delete($true)

    Write-Host "Packaging for NexusMods"
    New-Item -Type Directory -Path "$package\Nexusmods" -Force
    $nexus = New-Item -Type Directory -Path "$package\Nexusmods\package"
    $nexus.CreateSubdirectory('Jotunn')
    Copy-Item -Path "$TargetPath\$name.dll" -Destination "$nexus\Jotunn\"
    Copy-Item -Path "$TargetPath\$name.pdb" -Destination "$nexus\Jotunn\"
    Copy-Item -Path "$TargetPath\$name.xml" -Destination "$nexus\Jotunn\"
    Copy-Item -Path "$TargetPath\$name.dll.mdb" -Destination "$nexus\Jotunn\"
    Copy-Item -Path "$ProjectPath\..\README.md" -Destination "$nexus\Jotunn\"
    Copy-Item -Path "$ProjectPath\..\CHANGELOG.md" -Destination "$nexus\Jotunn\"
    Remove-Item -Path "$package\Nexusmods\$name-$version.zip" -Force
    Invoke-Expression "& `"$(Get-Location)\libraries\7za.exe`" a `"$package\Nexusmods\$name-$version.zip`" `"$nexus\*`""
    $nexus.Delete($true)
}


# Pop Location
Pop-Location