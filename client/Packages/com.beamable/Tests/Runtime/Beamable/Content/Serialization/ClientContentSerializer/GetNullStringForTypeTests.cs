using Beamable.Common.Content;
using Beamable.Tests.Content.Serialization.Support;
using NUnit.Framework;
using UnityEngine;

namespace Beamable.Tests.Content.Serialization.ClientContentSerializationTests
{
	public class GetNullStringForTypeTests
	{


		[Test]
		public void Serialize()
		{
			var meta = new BundlesContent {Id = "metadata.bundles.tuna"};
			meta.Thematic2 = true;
			meta.Thematic3 = false;
			var s = new TestSerializer();
			var json = s.Serialize(meta);
			Debug.Log(json);
			var reconstructed = s.Deserialize<BundlesContent>(json);

			Assert.IsNotNull(reconstructed.Thematic2);
			Assert.IsNotNull(reconstructed.Thematic3);
			Assert.IsTrue(reconstructed.Thematic2);
			Assert.IsFalse(reconstructed.Thematic3);

			// TODO: this doesn't feel great, because the data was serialized without 'Thematic' being set, why should it be there on the way back?
			Assert.IsNotNull(reconstructed.Thematic);
			Assert.IsFalse(reconstructed.Thematic);
		}

		[System.Serializable]
		[ContentType(CONTENT_TYPE)]
		public class MetadataContent : TestContentObject
		{
			public const string CONTENT_TYPE = "metadata";
		}

		[ContentType(SUB_CONTENT_TYPE)]
		public class BundlesContent : MetadataContent
		{
			public const string SUB_CONTENT_TYPE = "bundles";
			public string BundleId;
			public int league;
			public int division;
			public int? x;
			public bool? Thematic;
			public bool? Thematic2;
			public bool? Thematic3;
			public int[] Common;
		}


	}
}
