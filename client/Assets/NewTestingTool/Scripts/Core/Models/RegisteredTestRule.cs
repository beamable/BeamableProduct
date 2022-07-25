using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.NewTestingTool.Core.Models
{
	[Serializable]
	public class RegisteredTestRule
	{
		public int Order => _order;
		public List<RegisteredTestRuleMethod> RegisteredTestRuleMethods => _registeredTestRuleMethods;

		[SerializeField] private int _order;
		[SerializeField] private List<RegisteredTestRuleMethod> _registeredTestRuleMethods = new List<RegisteredTestRuleMethod>();
		
		public RegisteredTestRule(int order)
		{
			_order = order;
		}
		public void Reset()
		{
			foreach (var registeredMethodTest in RegisteredTestRuleMethods)
				registeredMethodTest.Reset();
		}
	}
}
