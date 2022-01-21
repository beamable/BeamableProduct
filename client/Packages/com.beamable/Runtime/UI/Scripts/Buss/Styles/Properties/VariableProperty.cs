using Beamable.UI.Sdf.Styles;
using System;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Beamable.UI.Buss
{
	[Serializable]
	public class VariableProperty : IBussProperty
	{
		[SerializeField]
		private string _variableName = "";
		public string VariableName
		{
			get => _variableName;
			set => _variableName = value;
		}

		public VariableProperty() { }

		public VariableProperty(string variableName)
		{
			VariableName = variableName;
		}

		public IBussProperty CopyProperty()
		{
			return new VariableProperty(VariableName);
		}
	}
}
