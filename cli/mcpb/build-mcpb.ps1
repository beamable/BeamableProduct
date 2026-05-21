param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$ScriptDir = $PSScriptRoot
$CliCsproj = Join-Path $ScriptDir '..\cli\cli.csproj'
$StagingDir = Join-Path $ScriptDir 'staging'
$OutputDir = Join-Path $ScriptDir 'output'

$Rids = @('win-x64', 'osx-arm64', 'osx-x64', 'linux-x64')

if (-not (Get-Command mcpb -ErrorAction SilentlyContinue)) {
    Write-Error "mcpb CLI not found. Install it with: npm install -g @anthropic-ai/mcpb"
    exit 1
}

if (Test-Path $StagingDir) { Remove-Item $StagingDir -Recurse -Force }
if (Test-Path $OutputDir) { Remove-Item $OutputDir -Recurse -Force }
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

foreach ($Rid in $Rids) {
    Write-Host "=== Building $Rid ==="
    $Stage = Join-Path $StagingDir $Rid
    New-Item -ItemType Directory -Path (Join-Path $Stage 'server') -Force | Out-Null

    dotnet publish $CliCsproj `
        -f net8.0 `
        -r $Rid `
        -c Release `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:SKIP_GENERATION=true `
        -p:BeamBuild=true `
        -p:DebugType=none `
        -o (Join-Path $Stage 'server')

    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $Rid" }

    # dotnet publish uses AssemblyName (Beamable.Tools), but mcpb entry_point expects "beam"
    $ServerDir = Join-Path $Stage 'server'
    if ($Rid -like 'win-*') {
        Rename-Item (Join-Path $ServerDir 'Beamable.Tools.exe') 'beam.exe'
        $BinaryName = 'beam.exe'
    } else {
        Rename-Item (Join-Path $ServerDir 'Beamable.Tools') 'beam'
        $BinaryName = 'beam'
    }

    $ManifestContent = (Get-Content (Join-Path $ScriptDir 'manifest.json') -Raw) -replace 'VERSION_PLACEHOLDER', $Version -replace 'BINARY_PLACEHOLDER', $BinaryName
    Set-Content -Path (Join-Path $Stage 'manifest.json') -Value $ManifestContent -NoNewline

    Copy-Item (Join-Path $ScriptDir '.mcpbignore') -Destination $Stage

    $IconPath = Join-Path $ScriptDir 'icon.png'
    if (Test-Path $IconPath) { Copy-Item $IconPath -Destination $Stage }

    Write-Host "=== Packing $Rid ==="
    mcpb pack $Stage (Join-Path $OutputDir "beamable-$Rid.mcpb")

    if ($LASTEXITCODE -ne 0) { throw "mcpb pack failed for $Rid" }
}

Remove-Item $StagingDir -Recurse -Force

Write-Host ""
Write-Host "=== Done ==="
Get-ChildItem $OutputDir | Format-Table Name, @{N='Size(MB)';E={[math]::Round($_.Length/1MB,1)}}
