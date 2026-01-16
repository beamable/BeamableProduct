namespace UnityEngine.AddressableAssets
{
   public class AssetReference
   {
      public string AssetGUID, SubObjectName;

      public AssetReference(string guid)
      {
         AssetGUID = guid;
      }
      
      public AssetReference()
      {
      }
   }

   public class AssetReferenceT<TObject> : AssetReference
   {
      public AssetReferenceT(string guid) : base(guid)
      {
      }
   }

   public class AssetReferenceSprite : AssetReferenceT<Sprite>
   {
      public AssetReferenceSprite(string guid) : base(guid)
      {
      }
   }

   public class AssetReferenceGameObject : AssetReferenceT<GameObject>
   {
      public AssetReferenceGameObject(string guid) : base(guid)
      {
      }
   }
}
