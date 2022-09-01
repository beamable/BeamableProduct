using System;
using UnityEngine;

namespace Beamable.NewTestingTool.Core.Models.Descriptors
{
	[Serializable]
	public class TestSceneDescriptor : ScriptableObject
	{
		[SerializeField] private TestDescriptor _testDescriptor;

		public TestDescriptor GetTestDescriptor() => _testDescriptor;
	}
}
