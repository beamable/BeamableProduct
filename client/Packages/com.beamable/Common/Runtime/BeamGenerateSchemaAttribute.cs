// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

﻿using System;

namespace Beamable
{
	/// <summary>
	/// Placing this attribute on a JSON-serializable class or struct inside a Beamable Microservice will tell the microservice that this type should be part of the Microservice's OAPI schemas.
	/// This means that, for non-C# engine integrations, this type will be auto-generated for you. For C# engine integrations (such as Unity/Godot), this is not needed as you can simply reference a common library
	/// between both.
	/// <para/>
	/// THIS MUST ALWAYS BE ACCOMPANIED BY THE <see cref="System.SerializableAttribute"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class BeamGenerateSchemaAttribute : Attribute
	{
	}
}
