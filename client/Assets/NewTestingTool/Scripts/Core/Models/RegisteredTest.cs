using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.NewTestingTool.Core.Models
{
	[Serializable]
	public class RegisteredTest
	{
		public List<RegisteredTestRule> RegisteredTestRules => _registeredTestRules;
		
		[SerializeField] private List<RegisteredTestRule> _registeredTestRules;
		
		public RegisteredTest(List<RegisteredTestRule> registeredTestRules)
		{
			_registeredTestRules = registeredTestRules;
		}
		public void Reset()
		{
			foreach (var registeredTestRule in RegisteredTestRules)
				registeredTestRule.Reset();
		}
	}
}
