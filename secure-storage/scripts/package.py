#!/usr/bin/env python3
"""Package rebuilt Unity assets, SDK patch, source, and the SDK files used by the harness."""
from pathlib import Path
import os
import shutil
import xml.etree.ElementTree as ET
import zipfile


project = Path(__file__).resolve().parents[1]
repository = project.parent
artifacts = project / "artifacts"
archive = project / "SecureStorage.zip"
temporary = artifacts / "SecureStorage.zip.tmp"

required = [
    artifacts / "unity/Plugins/Desktop/Beamable.SecureStorage.dll",
    artifacts / "unity/Plugins/iOS/Beamable.SecureStorage.dll",
    artifacts / "unity/Analytics/SecureAnalyticsBatchStorage.cs",
    artifacts / "sdk-patch/Packages/com.beamable/Runtime/Beam.cs",
]
for path in required:
    if not path.is_file():
        raise SystemExit(f"Build first; missing {path.relative_to(project)}")
for obsolete in ("AnalyticsTracker.cs", "SecurePersistentBatchManager.cs"):
    if (artifacts / "unity" / obsolete).exists():
        raise SystemExit(f"Obsolete adapter still present: {obsolete}; rebuild first")

files = set()
for directory, names, filenames in os.walk(project):
    names[:] = [name for name in names if name not in {"bin", "obj", "artifacts", ".git", "__pycache__"}]
    for name in filenames:
        if name == ".DS_Store" or name.startswith("SecureStorage.zip"):
            continue
        files.add(Path(directory) / name)
for folder in (artifacts / "unity", artifacts / "sdk-patch"):
    files.update(path for path in folder.rglob("*") if path.is_file() and path.name != ".DS_Store")

# These are source snapshots, not a complete Unity project. They allow the integration
# executable to compile the exact SDK sources in an extracted archive, with no network.
integration = project / "tests/Beamable.SecureStorage.IntegrationTests/Beamable.SecureStorage.IntegrationTests.csproj"
sdk = repository / "client/Packages/com.beamable"
for path in (artifacts / "sdk-patch/Packages/com.beamable").rglob("*"):
    if path.is_file():
        files.add(sdk / path.relative_to(artifacts / "sdk-patch/Packages/com.beamable"))
native = artifacts / "native/libBeamableSecureStorage.dylib"
if native.is_file():
    files.add(native)
tree = ET.parse(integration)
for item in tree.findall(".//Compile"):
    include = item.get("Include", "")
    if include.startswith("$(SdkRoot)/"):
        for path in sdk.glob(include[len("$(SdkRoot)/"):]):
            files.add(path)
            if path.with_suffix(path.suffix + ".meta").is_file():
                files.add(path.with_suffix(path.suffix + ".meta"))
fixture = sdk / "Tests/Runtime/Beamable/AnalyticsBatchPersistenceTests.cs"
files.add(fixture)
files.add(fixture.with_suffix(".cs.meta"))

backup = artifacts / "review-backup/SecureStorage-previous.zip"
backup.parent.mkdir(parents=True, exist_ok=True)
if archive.is_file() and not backup.exists():
    shutil.copy2(archive, backup)

with zipfile.ZipFile(temporary, "w", zipfile.ZIP_DEFLATED) as output:
    output.writestr("README.md", """# SecureStorage review revision

Start with `secure-storage/README.md` and `secure-storage/REVIEW-FIXES.md`.
The built plug-ins and opt-in integration are in `secure-storage/artifacts/unity/`.
Apply the SDK changes in `secure-storage/artifacts/sdk-patch/` to a compatible SDK
before installing the integration. Remove the previous SecureAnalyticsTracker and
SecurePersistentBatchManager adapters from consuming projects.

The client/ tree contains the SDK source snapshots used by the regression harness,
plus its Unity PlayMode fixture. It is not a complete Unity project or SDK package.
The archive retains repository-relative paths so the .NET solution and its harness
can be built from the extracted root. No backend analytics traffic is sent by tests.
""")
    for path in sorted(files):
        output.write(path, path.relative_to(repository).as_posix())
os.replace(temporary, archive)
print(f"Created {archive} ({len(files) + 1} files)")
