using Beamable.Serialization.SmallerJSON;
using NUnit.Framework;
using System;
using System.Text;
using UnityEngine;

namespace Beamable.Editor.Tests.SmallerJson
{
	public class JsonSerializeTest
	{
		[Test]
		public void CustomComplexClassTest()
		{
			var instance = new CustomComplexClass();
			instance.DisplayName = "Super Girl";
			instance.FirstName = "Kate";
			instance.LastName = "Super";
			var jsonSerializedUnity = JsonUtility.ToJson(instance);
			var smallerJsonSerialized = Json.Serialize(instance, new StringBuilder());
			Assert.AreEqual(jsonSerializedUnity,smallerJsonSerialized);
		}
		

		[Serializable]
		public class CustomComplexClass
		{
			public bool AllFieldsHaveValues =>
				!string.IsNullOrWhiteSpace(_displayName) &&
				!string.IsNullOrWhiteSpace(_firstName) &&
				!string.IsNullOrWhiteSpace(_lastName);
        
			public string DisplayName
			{
				get => _displayName;
				set => _displayName = value;
			}
			public string FirstName
			{
				get => _firstName;
				set => _firstName = value;
			}
			public string LastName
			{
				get => _lastName;
				set => _lastName = value;
			}
    
			[SerializeField] private string _displayName;
			[SerializeField] private string _firstName;
			[SerializeField] private string _lastName;
		}
	}
}
