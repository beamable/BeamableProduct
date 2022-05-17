using Beamable.Common.Api.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Beamable.Common.Content
{
	/// <summary>
	/// This type defines the %ClientManifest for the %ContentService.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Content.ContentService script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	[System.Serializable]
	public class ClientManifest
	{
		/// <summary>
		/// A set of <see cref="ClientContentInfo"/> that exist in this <see cref="ClientManifest"/>.
		/// Each <see cref="ClientContentInfo"/> describes one piece of content.
		/// </summary>
		public List<ClientContentInfo> entries;

		/// <summary>
		/// Use a <see cref="ContentQuery"/> to filter the <see cref="entries"/> and get a new <see cref="ClientManifest"/>.
		/// This method will not mutate the <i>current</i> <see cref="ClientManifest"/>. Instead, it allocates a new one.
		/// </summary>
		/// <param name="query">A <see cref="ContentQuery"/> to down filter the <see cref="entries"/></param>
		/// <returns>A new <see cref="ClientManifest"/></returns>
		public ClientManifest Filter(ContentQuery query)
		{
			return new ClientManifest {entries = entries.Where(e => query.Accept(e)).ToList()};
		}

		/// <summary>
		/// Use a string version of a <see cref="ContentQuery"/> to filter the <see cref="entries"/> and get a new <see cref="ClientManifest"/>.
		/// This method will not mutate the <i>current</i> <see cref="ClientManifest"/>. Instead, it allocates a new one.
		/// </summary>
		/// <param name="queryString">A string version of a <see cref="ContentQuery"/> to down filter the <see cref="entries"/></param>
		/// <returns>A new <see cref="ClientManifest"/></returns>
		public ClientManifest Filter(string queryString)
		{
			return Filter(ContentQuery.Parse(queryString));
		}

		/// <summary>
		/// The <see cref="entries"/> only describe the content, but don't contain the entire content data.
		/// This method will return every <see cref="ClientContentInfo"/> in the <see cref="entries"/> set.
		/// This may result in many network requests if the <see cref="entries"/> haven't been downloaded before.
		/// </summary>
		/// <param name="batchSize">
		/// The <see cref="batchSize"/> controls how many concurrent network requests will be allowed to run simultaneously.
		/// </param>
		/// <returns>
		/// A <see cref="SequencePromise{T}"/> representing the progress of the content resolution. At the end, it will have a
		/// list of <see cref="IContentObject"/> for each <see cref="ClientContentInfo"/> in the <see cref="entries"/> set
		/// </returns>
		public SequencePromise<IContentObject> ResolveAll(int batchSize = 50)
		{
			return entries.ResolveAll(batchSize);
		}

		/// <summary>
		/// The <see cref="ClientManifest"/> is represented as a CSV when delivered to the game client.
		/// This method is used internally to parse the CSV.
		/// </summary>
		/// <param name="data">Raw CSV data</param>
		/// <returns>A <see cref="ClientManifest"/></returns>
		public static ClientManifest ParseCSV(string data)
		{
			var dataLength = string.IsNullOrWhiteSpace(data) ? 0 : data.Length;
			var emptyStringArray = new string[] { };

			var contentEntries = new List<ClientContentInfo>();
			bool isInDoubleQuote = false;
			var parts = new[]
			{
				new StringBuilder(), new StringBuilder(), new StringBuilder(), new StringBuilder(),
				new StringBuilder()
			};
			var currentPart = 0;

			void AddNewEntry()
			{
				contentEntries.Add(new ClientContentInfo()
				{
					type = parts[0].ToString().Trim(),
					contentId = parts[1].ToString().Trim(),
					version = parts[2].ToString().Trim(),
					visibility = ContentVisibility.Public, // the csv content is always public.
					uri = parts[3].ToString().Trim(),
					tags = parts[4].Length > 0
						? parts[4].ToString().Trim().Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)
						: emptyStringArray
				});
				for (int i = 0; i < parts.Length; i++)
				{
					parts[i].Clear();
				}

				currentPart = 0;
			}

			for (var i = 0; i < dataLength; i++)
			{
				var c = data[i];
				bool isDoubleQuote = c == '"';
				bool isComma = c == ',';
				bool isNewLine = c == '\n';
				isInDoubleQuote ^= isDoubleQuote;

				if (isDoubleQuote)
					continue;

				switch (isInDoubleQuote)
				{
					case false when (isComma || isNewLine):
					{
						currentPart++;
						if (currentPart > 4 || isNewLine)
							AddNewEntry();
						continue;
					}
				}

				parts[currentPart].Append(c);
			}

			if (currentPart > 0)
				AddNewEntry();

			return new ClientManifest {entries = contentEntries};
		}
	}

	/// <summary>
	/// This type defines the %ClientContentInfo for the %ContentService.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Content.ContentService script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	[System.Serializable]
	public class ClientContentInfo
	{
		/// <summary>
		/// The full content id. A content id is a dot separated string with at least one dot.
		/// The right-most clause is the name of the content, and everything else represents the type of the content.
		/// </summary>
		public string contentId;

		/// <summary>
		/// A checksum of the content's data.
		/// </summary>
		public string version;

		/// <summary>
		/// The public uri where the full content json can be downloaded
		/// </summary>
		public string uri;

		/// <summary>
		/// An internal field that will be removed in a future version.
		/// This is <b>NOT</b> the C# backing type of the content.
		/// </summary>
		public string type;

		/// <summary>
		/// The id of the manifest that this content was sourced from.
		/// For most cases, this will be the default manifest, "global".
		/// </summary>
		public string manifestID;

		/// <summary>
		/// An internal field that will be removed in a future version.
		/// This will always be <see cref="ContentVisibility.Public"/>
		/// </summary>
		public ContentVisibility visibility = ContentVisibility.Public;

		/// <summary>
		/// A set of content tags. Tags do not effect the <see cref="version"/> checksum.
		/// </summary>
		public string[] tags;

		/// <summary>
		/// Convert this <see cref="ClientContentInfo"/> into a <see cref="IContentRef"/> by using the <see cref="contentId"/> field.
		/// This method verifies that the backing C# class exists.
		/// </summary>
		/// <returns>A <see cref="IContentRef{TContent}"/></returns>
		public IContentRef ToContentRef()
		{
			var contentType = ContentRegistry.GetTypeFromId(contentId);
			return new ContentRef(contentType, contentId);
		}

		/// <summary>
		/// This object only describes the content, but does not contain the entire content data.
		/// This method will get the actual <see cref="IContentObject"/> by checking for the data at the <see cref="uri"/>.
		/// This may result in a network request if the entry has not been downloaded before.
		/// </summary>
		/// <returns></returns>
		public Promise<IContentObject> Resolve()
		{
			return ContentApi.Instance.FlatMap(api => api.GetContent(ToContentRef()));
		}
	}

	/// <summary>
	/// This type defines the %ClientContentInfoExtensions for the %ContentService.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Content.ContentService script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public static class ClientContentInfoExtensions
	{
		public static IEnumerable<IContentRef> ToContentRefs(this IEnumerable<ClientContentInfo> set)
		{
			return set.Select(info => info.ToContentRef());
		}

		public static SequencePromise<IContentObject> ResolveAll(this IEnumerable<ClientContentInfo> set,
		                                                         int batchSize = 50)
		{
			return set.ToContentRefs().ResolveAll(batchSize);
		}
	}
}
