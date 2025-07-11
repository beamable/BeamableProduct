using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using Beamable.Content;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Beamable.Editor.ContentService
{
	public static class ContentUtils
	{
		private static readonly MapOfValidationChecksums ChecksumTable = new();

		public static string ComputeChecksum(IContentObject content)
		{
			if (TryGetChecksumFromCache(content, out string cachedChecksum))
			{
				return cachedChecksum;
			}

			var sortProperties = !ContentConfiguration.Instance.EnablePropertyOrderDependenceForContentChecksum;

			using (var md5 = MD5.Create())
			{
				var json = ClientContentSerializer.SerializeProperties(content);
				var bytes = Encoding.ASCII.GetBytes(json);
				var hash = md5.ComputeHash(bytes);
				var checksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

				AddChecksumToCache(content, checksum);

				return checksum;
			}
		}

		private static bool TryGetChecksumFromCache(IContentObject content, out string s)
		{
			s = null;
			if (!(content is ContentObject contentObj) || !contentObj)
				return false;

			bool containsChecksum = ChecksumTable.TryGetValue(content.Id, out var existing);
			bool match = containsChecksum && existing.ValidationId.Equals(contentObj.ValidationGuid);
			s = match ? existing.Checksum : null;
			return match;
		}

		private static void AddChecksumToCache(IContentObject content, string checksum)
		{
			if (content is ContentObject contentObj2 && contentObj2)
			{
				ChecksumTable[contentObj2.Id] = new ValidationChecksum
				{
					ValidationId = contentObj2.ValidationGuid, Checksum = checksum
				};
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool AreTagsEqual(string[] firstContentTags, string[] secondContentTags)
		{
			return firstContentTags.Length == secondContentTags.Length &&
			       firstContentTags.All(secondContentTags.Contains);
		}

	}

	[Serializable]
	public struct ValidationChecksum
	{
		public Guid ValidationId;
		public string Checksum;
	}

	[Serializable]
	public class MapOfValidationChecksums : SerializableDictionaryStringToSomething<ValidationChecksum> { }
}
