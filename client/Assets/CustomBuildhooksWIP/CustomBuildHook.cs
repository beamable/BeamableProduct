using Beamable.Server.Editor;
using UnityEngine;

// This will be a part of solana example package
public class CustomBuildHook : IMicroserviceBuildHook
{
	public void Execute()
	{
		Debug.Log("Custom build hook called");
	}
}
