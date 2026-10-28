using System;
using System.Linq;
using Beamable.Server;
using NUnit.Framework;

namespace microserviceTests.microservice
{
	/// <summary>
	/// Startup copies the args several times, so any property <see cref="MicroserviceArgsExtensions.Copy"/>
	/// forgets is silently lost for every request.
	/// </summary>
	[TestFixture]
	public class MicroserviceArgsCopyTests
	{
		[Test]
		public void Copy_PreservesZid()
		{
			var copy = new MicroserviceArgs { Zid = "ZONE_123" }.Copy();

			Assert.That(copy.Zid, Is.EqualTo("ZONE_123"));
		}

		[Test]
		public void Copy_PreservesEverySettableProperty()
		{
			var properties = typeof(MicroserviceArgs).GetProperties()
				.Where(p => p.CanRead && p.CanWrite)
				.ToList();

			var original = new MicroserviceArgs();
			var seed = 1;
			foreach (var property in properties)
			{
				var value = CreateValue(property.PropertyType, property.Name, seed++);
				if (value != null)
				{
					property.SetValue(original, value);
				}
			}

			var copy = original.Copy();

			foreach (var property in properties)
			{
				Assert.That(property.GetValue(copy), Is.EqualTo(property.GetValue(original)),
					$"{nameof(MicroserviceArgs)}.{property.Name} is not copied by Copy()");
			}
		}

		static object CreateValue(Type type, string name, int seed)
		{
			if (type == typeof(string))
			{
				return $"{name}_{seed}";
			}
			if (type == typeof(int))
			{
				return seed;
			}
			if (type == typeof(long))
			{
				return (long)seed;
			}
			if (type == typeof(double))
			{
				return seed + 0.5;
			}
			if (type == typeof(bool))
			{
				return true;
			}
			if (type.IsEnum)
			{
				var values = Enum.GetValues(type);
				return values.GetValue(values.Length - 1);
			}
			return null;
		}
	}
}
