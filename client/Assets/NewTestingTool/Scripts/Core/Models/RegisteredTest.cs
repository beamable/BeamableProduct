using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.NewTestingTool.Core.Models
{
	[Serializable]
	public class RegisteredTest
	{
		public string TestClassName => _testClassName;
		public List<RegisteredTestRule> RegisteredTestRules => _registeredTestRules;
		
		[SerializeField] private string _testClassName;
		[SerializeField] private List<RegisteredTestRule> _registeredTestRules;
		
		public RegisteredTest(string testClassName, List<RegisteredTestRule> registeredTestRules)
		{
			_testClassName = testClassName;
			_registeredTestRules = registeredTestRules;
		}
		public void Reset()
		{
			foreach (var registeredTestRule in RegisteredTestRules)
				registeredTestRule.Reset();
		}
	}
}
