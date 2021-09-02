namespace Beamable.Common.Content
{
   /// <summary>
   /// This type defines the API for %Beamable %ContentObject and its many subclasses.
   ///
   /// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
   ///
   /// #### Related Links
   /// - See Beamable.Common.Content.ContentObject script reference
   ///
   /// ![img beamable-logo]
   ///
   /// </summary>
   public interface IContentObject
   {
      /// <summary>
      /// The id
      /// </summary>
      string Id { get; }
      
      /// <summary>
      /// The version
      /// </summary>
      string Version { get; }
      
      /// <summary>
      /// The tags
      /// </summary>
      string[] Tags { get; }
      string ManifestID { get; }

      /// <summary>
      /// Set Id And Version
      /// </summary>
      /// <param name="id"></param>
      /// <param name="version"></param>
      void SetIdAndVersion(string id, string version);

      string ToJson();
   }
}