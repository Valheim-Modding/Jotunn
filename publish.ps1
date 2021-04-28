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

$name = "$TargetAssembly" -Replace('.dll')

# Debug copies the dll to Valheim and creates the mdb
if ($Target.Equals("Debug")) {
    Write-Host "Updating local installation in $ValheimPath"
    
    $plug = New-Item -Type Directory -Path "$ValheimPath\BepInEx\plugins\$name" -Force
    Write-Host "Copy $TargetAssembly to $plug"
    Copy-Item -Path "$TargetPath\$TargetAssembly" -Destination "$plug" -Force
    
    $mono = "$ValheimPath\MonoBleedingEdge\EmbedRuntime";
    Write-Host "Copy mono-2.0-bdwgc.dll to $mono"
    if (!(Test-Path -Path "$mono\mono-2.0-bdwgc.dll.orig")) {
        Copy-Item -Path "$mono\mono-2.0-bdwgc.dll" -Destination "$mono\mono-2.0-bdwgc.dll.orig" -Force
    }
    Copy-Item -Path "$(Get-Location)\libraries\Debug\mono-2.0-bdwgc.dll" -Destination "$mono" -Force

    $pdb = "$TargetPath\$name.pdb"
    if (Test-Path -Path "$pdb") {
        Write-Host "Copy Debug files for plugin $name"
        Copy-Item -Path "$pdb" -Destination "$plug" -Force
        Start-Process -FilePath "$(Get-Location)\libraries\Debug\pdb2mdb.exe" -ArgumentList "`"$plug\$TargetAssembly`""
    }
        
    # set dnspy debugger env
    #$dnspy = '--debugger-agent=transport=dt_socket,server=y,address=127.0.0.1:56000,suspend=y,no-hide-debugger'
    #[Environment]::SetEnvironmentVariable('DNSPY_UNITY_DBG2','','User')
}

# Release builds packages for ThunderStore and NexusMods
if($Target.Equals("Release")) {
    $package = "$ProjectPath\_package"
    [xml]$versionxml = Get-Content -Path "$ProjectPath\BuildProps\version.props"
    $version = $versionxml.Project.PropertyGroup.Version
    
    Write-Host "Packaging for ThunderStore"
    $thunder = New-Item -Type Directory -Path "$package\Thunderstore"
    $thunder.CreateSubdirectory('plugins')
    Copy-Item -Path "$TargetPath\$TargetAssembly" -Destination "$thunder\plugins\$TargetAssembly"
    Copy-Item -Path "$ProjectPath\README.md" -Destination "$thunder\README"
    Compress-Archive -Path "$thunder\*" -DestinationPath "$package\Thunderstore-$name-$version.zip" -Forc
    $thunder.Delete($true)

    Write-Host "Packaging for NexusMods"
    $nexus = New-Item -Type Directory -Path "$package\Nexusmods"
    Copy-Item -Path "$TargetPath\$TargetAssembly" -Destination "$nexus\$TargetAssembly"
    Copy-Item -Path "$ProjectPath\README.bbcode" -Destination "$nexus\README"
    Compress-Archive -Path "$nexus\*" -DestinationPath "$package\Nexusmods-$name-$version.zip" -Force
    $nexus.Delete($true)
}


# Pop Location
Pop-Location