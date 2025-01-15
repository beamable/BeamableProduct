// this file was copied from nuget package Beamable.Server.Common@3.0.2
// https://www.nuget.org/packages/Beamable.Server.Common/3.0.2

﻿using Beamable.Server;

namespace Beamable.Common
{
	/// <summary>
	/// Interface used to mark <see cref="StorageDocument"/> descendants as an element of specific <see cref="MongoStorageObject"/> storage.
	/// Detected by <see cref="MongoIndexesReflectionCache"/> and necessary to make automatic attribute based mongo indexes creation work.
	/// </summary>
	/// <typeparam name="T">Constrained to <see cref="MongoStorageObject"/></typeparam>
	public interface ICollectionElement<T> where T : MongoStorageObject
	{

	}
}
