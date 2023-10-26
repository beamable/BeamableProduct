using System;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Beamable.Server.Editor.Usam.BeamCsProject
{
	[ScriptedImporter(0, ".beamref")]
	public class BeamProjectImporter : ScriptedImporter
	{
		public override void OnImportAsset(AssetImportContext ctx)
		{
			var instance = ScriptableObject.CreateInstance<BeamProjectAsset>();
			ctx.AddObjectToAsset("data", instance);
			ctx.SetMainObject(instance);
		}
	}
}
