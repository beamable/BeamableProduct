using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common.Api.Content;

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
      public List<ClientContentInfo> entries;

      public ClientManifest Filter(ContentQuery query)
      {
         return new ClientManifest
         {
            entries = entries.Where(e => query.Accept(e)).ToList()
         };
      }

      public ClientManifest Filter(string queryString)
      {
         return Filter(ContentQuery.Parse(queryString));
      }

      public SequencePromise<IContentObject> ResolveAll(int batchSize=50)
      {
         return entries.ResolveAll(batchSize);
      }

      public static ClientManifest ParseCSV(string data)
      {
         // TODO: Consider replacing this with a more advanced csv parser... This method breaks many "rules"
         //       https://donatstudios.com/Falsehoods-Programmers-Believe-About-CSVs

         var lines = (data ?? "").Split('\n');

         var contentEntries = lines.Select(line =>
         {
            var parts = line.Split(new char[]{','}, StringSplitOptions.None);
            if (parts.Length <= 1)
            {
               return null; // skip line.
            }
            return new ClientContentInfo()
            {
               type = parts[0].Trim(),
               contentId = parts[1].Trim(),
               version = parts[2].Trim(),
               visibility = ContentVisibility.Public, // the csv content is always public.
               uri = parts[3].Trim(),
               tags = parts.Length >= 5
                  ? parts[4].Trim().Split(new []{';'}, StringSplitOptions.RemoveEmptyEntries)
                  : new string[]{}
            };
         }).Where(entry => entry != null);

         return new ClientManifest()
         {
            entries = contentEntries.ToList()
         };
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
      public string contentId, version, uri, type, manifestID;
      public ContentVisibility visibility = ContentVisibility.Public;
      public string[] tags;

      public IContentRef ToContentRef()
      {
         var contentType = ContentRegistry.GetTypeFromId(contentId);
         return new ContentRef(contentType, contentId);
      }

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

      public static SequencePromise<IContentObject> ResolveAll(this IEnumerable<ClientContentInfo> set, int batchSize = 50)
      {
         return set.ToContentRefs().ResolveAll(batchSize);
      }
   }

}