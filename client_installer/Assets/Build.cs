using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Build
{
    [MenuItem("BUILD/Export")]
    public static void Export()
    {
        AssetDatabase.ExportPackage(
            "Assets/BeamableInstaller",
            "Beamable_SDK_Installer.unitypackage",
            ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
    }
}
