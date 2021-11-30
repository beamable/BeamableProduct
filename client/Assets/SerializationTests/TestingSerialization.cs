using Beamable.Common.Content;
using Beamable.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace DefaultNamespace
{
	[System.Serializable]
	public class SimplePoco
	{
		public int A;

		public override bool Equals(object obj)
		{
			return obj != null && obj is SimplePoco casted && casted.A == A;
		}

		public override int GetHashCode()
		{
			return A;
		}

		public override string ToString() => $"A=[{A}]";
	}

	class CustomOptionClass
	{
		public OptionalInt value;
	}

	public class TestingSerialization : MonoBehaviour
	{
		public async void Start()
		{
			await Task.Delay(1500);
			{
				Debug.Log("Serialize OptionalInt with value 5:");
				OptionalInt test = new OptionalInt {Value = 5, HasValue = true};
				var json = BeamableJson.Serialize(test);
				Debug.Log(json);
				var deserialized = BeamableJson.Deserialize<OptionalInt>(json);
				Assert.IsNotNull(deserialized);
			}

			{
				Debug.Log("Serialize CustomOptionClass with OptionalInt:");
				var secondTest = new CustomOptionClass {value = new OptionalInt() {Value = 32, HasValue = true}};
				var json = BeamableJson.Serialize(secondTest);
				Debug.Log(json);
				var deserialized = BeamableJson.Deserialize<CustomOptionClass>(json);
				Assert.IsNotNull(deserialized);
			}

			{
				Debug.Log("CanDeserializeList_OfTypedObjects");
				var deserialized = BeamableJson.Deserialize<List<SimplePoco>>("[{\"A\": 1}, {\"A\": 2}]");
				var toCompare = new List<SimplePoco> {new SimplePoco {A = 1}, new SimplePoco {A = 2}};
				Assert.IsTrue(deserialized.SequenceEqual(toCompare));
				Debug.Log("Success");
			}

			{
				Debug.Log("Deserialize [\"a\", \"b\", \"c\"] to List<string>");
				var deserialized = BeamableJson.Deserialize<List<string>>("[\"a\", \"b\", \"c\"]");
				Assert.IsTrue(deserialized.SequenceEqual(new List<string> {"a", "b", "c"}));
				Debug.Log("Success");
			}
			{
				Debug.Log("CanDeserializeList_OfInt [1,2,3,4,5]");
				var deserialized = BeamableJson.Deserialize<List<int>>("[1,2,3,4,5]");
				Assert.IsTrue(deserialized.SequenceEqual(new List<int>
				{
					1,
					2,
					3,
					4,
					5
				}));
				Debug.Log("Success");
			}
		}
	}
}
