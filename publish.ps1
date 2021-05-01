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
    [System.String]$ProjectPath
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
    Start-Process -FilePath "$(Get-Location)\libraries\Debug\pdb2mdb.exe" -ArgumentList "`"$TargetPath\$TargetAssembly`""
}

# Debug copies the dll to Valheim
if ($Target.Equals("Debug")) {
    Write-Host "Updating local installation in $ValheimPath"
    
    $plug = New-Item -Type Directory -Path "$ValheimPath\BepInEx\plugins\$name" -Force
    Write-Host "Copy $TargetAssembly to $plug"
    Copy-Item -Path "$TargetPath\$name.dll" -Destination "$plug" -Force
    Copy-Item -Path "$TargetPath\$name.dll.mdb" -Destination "$plug" -Force
    
    $mono = "$ValheimPath\MonoBleedingEdge\EmbedRuntime";
    Write-Host "Copy mono-2.0-bdwgc.dll to $mono"
    if (!(Test-Path -Path "$mono\mono-2.0-bdwgc.dll.orig")) {
        Copy-Item -Path "$mono\mono-2.0-bdwgc.dll" -Destination "$mono\mono-2.0-bdwgc.dll.orig" -Force
    }
    Copy-Item -Path "$(Get-Location)\libraries\Debug\mono-2.0-bdwgc.dll" -Destination "$mono" -Force

    # set dnspy debugger env
    #$dnspy = '--debugger-agent=transport=dt_socket,server=y,address=127.0.0.1:56000,suspend=y,no-hide-debugger'
    #[Environment]::SetEnvironmentVariable('DNSPY_UNITY_DBG2','','User')
}

# Release builds packages for ThunderStore and NexusMods
if($Target.Equals("Release") -and $name.Equals("JotunnLib")) {
    $package = "$ProjectPath\_package"
    [xml]$versionxml = Get-Content -Path "$ProjectPath\BuildProps\version.props"
    $version = $versionxml.Project.PropertyGroup.Version
    
    Write-Host "Packaging for ThunderStore"
    New-Item -Type Directory -Path "$package\Thunderstore" -Force
    $thunder = New-Item -Type Directory -Path "$package\Thunderstore\package"
    $thunder.CreateSubdirectory('plugins')
    Copy-Item -Path "$TargetPath\$name.dll" -Destination "$thunder\plugins\"
    Copy-Item -Path "$TargetPath\$name.dll.mdb" -Destination "$thunder\plugins\"
    Copy-Item -Path "$ProjectPath\README.md" -Destination "$thunder\README"
    Copy-Item -Path "$ProjectPath\manifest.json" -Destination "$thunder\manifest.json"
    Copy-Item -Path "$(Get-Location)\resources\JVL_Logo_512x512.png" -Destination "$thunder\icon.png"
    Compress-Archive -Path "$thunder\*" -DestinationPath "$package\Thunderstore\$name-$version.zip" -Force
    $thunder.Delete($true)

    Write-Host "Packaging for NexusMods"
    New-Item -Type Directory -Path "$package\Nexusmods" -Force
    $nexus = New-Item -Type Directory -Path "$package\Nexusmods\package"
    Copy-Item -Path "$TargetPath\$name.dll" -Destination "$nexus\"
    Copy-Item -Path "$TargetPath\$name.dll.mdb" -Destination "$nexus\"
    Copy-Item -Path "$ProjectPath\README.md" -Destination "$nexus\README"
    Compress-Archive -Path "$nexus\*" -DestinationPath "$package\Nexusmods\$name-$version.zip" -Force
    $nexus.Delete($true)
}


# Pop Location
Pop-Location