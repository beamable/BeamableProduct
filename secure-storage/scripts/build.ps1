$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$LibraryProject = Join-Path $ProjectRoot 'src/Beamable.SecureStorage/Beamable.SecureStorage.csproj'
$OutputRoot = Join-Path $ProjectRoot 'artifacts'
$UnityPlugins = Join-Path $OutputRoot 'unity/Plugins'
$SdkPatch = Join-Path $OutputRoot 'sdk-patch/Packages/com.beamable'
$SdkSource = Join-Path $ProjectRoot '../client/Packages/com.beamable'

dotnet build $LibraryProject -c Release --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build $LibraryProject -c Release --nologo -p:SecureStorageIOS=true -o (Join-Path $OutputRoot 'managed-ios')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

New-Item -ItemType Directory -Force -Path "$UnityPlugins/Desktop", "$UnityPlugins/iOS", "$OutputRoot/unity/Editor" | Out-Null
New-Item -ItemType Directory -Force -Path "$OutputRoot/unity/Analytics" | Out-Null
foreach ($obsolete in @('AnalyticsTracker.cs', 'AnalyticsTracker.cs.meta', 'SecurePersistentBatchManager.cs', 'SecurePersistentBatchManager.cs.meta')) {
    Remove-Item (Join-Path "$OutputRoot/unity" $obsolete) -ErrorAction SilentlyContinue
}
Copy-Item "$ProjectRoot/unity/Analytics/*.cs", "$ProjectRoot/unity/Analytics/*.meta" "$OutputRoot/unity/Analytics/"
Copy-Item "$ProjectRoot/src/Beamable.SecureStorage/bin/Release/netstandard2.1/Beamable.SecureStorage.dll" "$UnityPlugins/Desktop/"
Copy-Item "$ProjectRoot/src/Beamable.SecureStorage/bin/Release/netstandard2.1/Beamable.SecureStorage.xml" "$UnityPlugins/Desktop/"
Copy-Item "$OutputRoot/managed-ios/Beamable.SecureStorage.dll" "$UnityPlugins/iOS/"
Copy-Item "$ProjectRoot/unity/ManagedDesktop.dll.meta" "$UnityPlugins/Desktop/Beamable.SecureStorage.dll.meta"
Copy-Item "$ProjectRoot/unity/ManagedIOS.dll.meta" "$UnityPlugins/iOS/Beamable.SecureStorage.dll.meta"
Copy-Item "$ProjectRoot/native/apple/BeamableSecureStorage.c" "$UnityPlugins/iOS/"
Copy-Item "$ProjectRoot/unity/SecureStorageIOSBuild.cs" "$OutputRoot/unity/Editor/"
$SdkFiles = @(
    'Runtime/Beam.cs',
    'Runtime/Core/Platform/SDK/Analytics/AnalyticsTracker.cs',
    'Runtime/Core/Platform/SDK/Analytics/AnalyticsDependencyRegistration.cs',
    'Runtime/Core/Platform/SDK/Analytics/Batch/BatchManager.cs',
    'Runtime/Core/Platform/SDK/Analytics/Batch/PersistentBatchManager.cs',
    'Runtime/Core/Platform/SDK/Analytics/Batch/IAnalyticsBatchStorage.cs',
    'Runtime/Core/Platform/SDK/Analytics/Batch/PlayerPrefsBatchStorage.cs',
    'Tests/Runtime/Beamable/AnalyticsBatchPersistenceTests.cs'
)
foreach ($relative in $SdkFiles) {
    $destination = Join-Path $SdkPatch $relative
    New-Item -ItemType Directory -Force -Path (Split-Path $destination -Parent) | Out-Null
    Copy-Item (Join-Path $SdkSource $relative) $destination
    Copy-Item (Join-Path $SdkSource "$relative.meta") "$destination.meta"
}
Copy-Item "$ProjectRoot/README.md" "$OutputRoot/README.md"
Copy-Item "$ProjectRoot/REVIEW-FIXES.md" "$OutputRoot/REVIEW-FIXES.md"
Write-Host "Unity plug-ins: $OutputRoot/unity (build the macOS native library on a Mac)"
Write-Host "Required SDK source patch: $OutputRoot/sdk-patch"
