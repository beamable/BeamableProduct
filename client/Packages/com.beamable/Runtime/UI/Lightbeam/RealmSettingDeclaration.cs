using System.Collections.Generic;
using Beamable.Common.Content;
using Beamable.Serialization;
using UnityEditor;
using UnityEngine;

namespace Beamable.Runtime.LightBeams
{
	public class RealmSettingDeclaration : JsonSerializable.ISerializable
	{
		public string uniqueRealmName;
		public MapOfString realmConfig = new MapOfString();
		public List<ContentEntry> contentEntries = new List<ContentEntry>();

		public static RealmSettingDeclaration FromAsset(TextAsset asset)
		{
			var result = JsonSerializable.FromJson<RealmSettingDeclaration>(asset.text);
			return result;
		}

		[MenuItem("LightBeam/TestItOut")]
		public static void Test()
		{
			Debug.Log("hello world");
			var json = @"{""contentEntries"": [{""content"": [1,2]}, {""content"": {""id"":""leaderboards.default""}}], ""realmConfig"":{""namespace|a"":""value""}}";
			var result = JsonSerializable.FromJson<RealmSettingDeclaration>(json);
			Debug.Log("Done " + result);
		}

		public class ContentEntry : JsonSerializable.ISerializable
		{
			public string id;
			public JsonString properties;
			public List<string> tags;
			
			public void Serialize(JsonSerializable.IStreamSerializer s)
			{
				s.Serialize(nameof(id), ref id);
				s.SerializeNestedJson(nameof(properties), ref properties);
				s.SerializeList(nameof(tags), ref tags);
			}
		}

		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			s.Serialize(nameof(uniqueRealmName), ref uniqueRealmName);
			s.SerializeList(nameof(contentEntries), ref contentEntries);
			s.SerializeDictionary<MapOfString, string>(nameof(realmConfig), ref realmConfig);
		}
	}
	
}
