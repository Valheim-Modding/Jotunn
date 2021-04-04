param(
    [Parameter(Mandatory)]
    [ValidateSet('Debug','Release')]
    [System.String]$Target,
    
    [Parameter(Mandatory)]
    [System.String]$TargetPath,
    
    [Parameter(Mandatory)]
    [System.String]$TargetAssembly,

    [Parameter(Mandatory)]
    [System.String]$ValheimPath
)

# Make sure Get-Location is the script path
Push-Location -Path (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Test some preliminaries
("$TargetPath",
 "$ValheimPath",
 "$(Get-Location)\libraries"
) | % {
    if (!(Test-Path "$_")) {Write-Error -ErrorAction Stop -Message "$_ folder is missing"}
}

# Main Script
Write-Host "Publishing for $Target from $TargetPath"

if ($Target.Equals("Debug")) {
    Write-Host "Updating local installation in $ValheimPath"
    
    Write-Host "Copy mono-2.0-bdwgc.dll to $ValheimPath\MonoBleedingEdge\EmbedRuntime"
    if (!(Test-Path -Path "$ValheimPath\MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll.orig")) {
        Copy-Item -Path "$ValheimPath\MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll" -Destination "$ValheimPath\MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll.orig" -Force
    }
    Copy-Item -Path "$(Get-Location)\libraries\Debug\mono-2.0-bdwgc.dll" -Destination "$ValheimPath\MonoBleedingEdge\EmbedRuntime" -Force

    foreach($asm in $TargetAssembly.Split(',')){
        $pdb = "$TargetPath\" + ($asm -Replace('.dll','.pdb'))
        if (Test-Path -Path "$pdb") {
            Write-Host "Copy Debug files for plugin $asm"
            Copy-Item -Path "$pdb" -Destination "$ValheimPath\BepInEx\plugins" -Force
            start "$(Get-Location)\libraries\Debug\pdb2mdb.exe" "$ValheimPath\BepInEx\plugins\$asm"
        }
    }
        
    # set dnspy debugger env
    #$dnspy = '--debugger-agent=transport=dt_socket,server=y,address=127.0.0.1:56000,suspend=y,no-hide-debugger'
    #[Environment]::SetEnvironmentVariable('DNSPY_UNITY_DBG2','','User')
}

# Pop Location
Pop-Location