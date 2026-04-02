[Documentation](https://help.beamable.com/Unity-Latest/unity/getting-started/installing-beamable/)

# Beamable Unity SDK
This folder contains the Unity SDK package sources used to produce the Beamable Unity packages (UPM). Most users should consume the published Unity packages via our scoped registry; this folder is primarily for maintainers and internal development.

## Quickstart
- Install via our package installer: https://packages.beamable.com/com.beamable/Beamable_SDK_Installer.unitypackage
- Or add the Beamable scoped registry to your project's `manifest.json`:

```json
"scopedRegistries": [
  {
    "name": "Beamable",
    "url": "https://nexus.beamable.com/nexus/content/repositories/unity",
    "scopes": [
  "com.beamable"
  ]
  }
]
```

## Compatibility notes
- For older Beamable 1.x projects you might have references to both `com.beamable` and `com.beamable.server`. Modern usage consolidates server types into `com.beamable`.

## For maintainers
- To release Unity SDK packages, use the `Release Unity SDK` GitHub Actions workflow (`.github/workflows/release-unity.yml`).
- Version bumping: follow the roadmap definitions for version numbers. For hotfixes, bump the minor (last) number based on the base version for that hotfix.

## Contributing 
This project has the same [contribution policy](https://github.com/beamable/BeamableProduct/tree/main/README.md#Contributing) as the main repository.

## License 
This project has the same [license policy](https://github.com/beamable/BeamableProduct/tree/main/README.md#License) as the main repository.