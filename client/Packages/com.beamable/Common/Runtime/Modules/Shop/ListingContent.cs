using System;
using System.Collections;
using System.Collections.Generic;
using Beamable.Common.Content;
using Beamable.Common.Content.Validation;
using Beamable.Common.Inventory;
using Beamable.Content;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beamable.Common.Shop
{
   /// <summary>
   /// This type defines a %Beamable %ContentObject subclass for the %CommerceService and %Store feature.
   ///
   /// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
   ///
   /// #### Related Links
   /// - See the <a target="_blank" href="https://docs.beamable.com/docs/store-feature">Store</a> feature documentation
   /// - See Beamable.Api.Commerce.CommerceService script reference
   ///
   /// ![img beamable-logo]
   ///
   /// </summary>
   [ContentType("listings")]
   [System.Serializable]
   [Agnostic]
   public class ListingContent : ContentObject
   {
      [Tooltip(ContentObject.TooltipListingPrice1)]
      public ListingPrice price;

      [Tooltip(ContentObject.TooltipListingOffer1)]
      public ListingOffer offer;

      [Tooltip(ContentObject.TooltipOptional0 + ContentObject.TooltipActivePeriod1)]
      public OptionalPeriod activePeriod;

      [Tooltip(ContentObject.TooltipOptional0 + ContentObject.TooltipPurchaseLimit1)]
      [MustBePositive]
      public OptionalInt purchaseLimit;

      [Tooltip(ContentObject.TooltipOptional0 + ContentObject.TooltipCohort1 + ContentObject.TooltipRequirement2)]
      public OptionalCohort cohortRequirements;

      [Tooltip(ContentObject.TooltipOptional0 + ContentObject.TooltipStat1 + ContentObject.TooltipRequirement2)]
      public OptionalStats playerStatRequirements;

      [Tooltip(ContentObject.TooltipOptional0 + ContentObject.TooltipActivePeriod1 + ContentObject.TooltipRequirement2)]
      public OptionalOffers offerRequirements;

      [Tooltip(ContentObject.TooltipOptional0 + ContentObject.TooltipActivePeriod1)]
      public OptionalSerializableDictionaryStringToString clientData;

      [MustBePositive]
      [Tooltip(ContentObject.TooltipOptional0 + ContentObject.TooltipDurationSeconds1 + ContentObject.TooltipActive2)]
      public OptionalInt activeDurationSeconds;

      [Tooltip(ContentObject.TooltipOptional0 + ContentObject.TooltipDurationSeconds1 + ContentObject.TooltipActiveCooldown2)]
      [MustBePositive]
      public OptionalInt activeDurationCoolDownSeconds;

      [Tooltip(ContentObject.TooltipOptional0 + ContentObject.TooltipDurationPurchaseLimit1)]
      [MustBePositive]
      public OptionalInt activeDurationPurchaseLimit;

      [Tooltip(ContentObject.TooltipOptional0 + ContentObject.ButtonText1)]
      public OptionalString buttonText; // TODO: This is a dictionary, not a string!

      [Tooltip(ContentObject.TooltipOptional0 + "schedule for when the listing will be active")]
      public OptionalListingSchedule schedule;

   }

   [System.Serializable]
   public class ListingOffer
   {
      [Tooltip(ContentObject.TooltipOptional0 + ContentObject.TooltipTitle1)]
      [CannotBeEmpty]
      public OptionalNonBlankStringList titles;

      [Tooltip(ContentObject.TooltipOptional0 + ContentObject.TooltipDescription1)]
      [CannotBeEmpty]
      public OptionalNonBlankStringList descriptions;

      [Tooltip(ContentObject.TooltipObtainCurrency1)]
      public List<OfferObtainCurrency> obtainCurrency;

      [Tooltip(ContentObject.TooltipObtainItem1)]
      public List<OfferObtainItem> obtainItems;
   }

   [System.Serializable]
   public class OptionalNonBlankStringList : Optional<NonBlankStringList> {}

   [System.Serializable]
   public class NonBlankStringList : DisplayableList
   {
      [CannotBeBlank]
      public List<string> listData = new List<string>();

      protected override IList InternalList => listData;
      public override string GetListPropertyPath() => nameof(listData);
   }

   [System.Serializable]
   [Agnostic]
   public class OfferObtainCurrency : ISerializationCallbackReceiver
   {
      public CurrencyRef symbol;
      public int amount;

      #region backwards compatability

      [FormerlySerializedAs("symbol")]
      [SerializeField]
      [HideInInspector]
      [IgnoreContentField]
      [Obsolete("use the symbol parameter instead.")]
      private string symbol_legacy;

      // disable obsolete warning...
#pragma warning disable 618
      public void OnBeforeSerialize()
      {
         symbol_legacy = null;
      }

      public void OnAfterDeserialize()
      {
         if (!string.IsNullOrEmpty(symbol_legacy))
         {
            symbol = new CurrencyRef(symbol_legacy);
            symbol_legacy = null;
         }
      }
#pragma warning restore 618
      #endregion
   }

   [System.Serializable]
   public class OfferObtainItem : ISerializationCallbackReceiver
   {
      public ItemRef contentId;
      public List<OfferObtainItemProperty> properties;

      #region backwards compatability

      [FormerlySerializedAs("contentId")]
      [SerializeField]
      [HideInInspector]
      [IgnoreContentField]
      [Obsolete("use the content parameter instead.")]
      private string content_legacy;

      // disable obsolete warning...
#pragma warning disable 618
      public void OnBeforeSerialize()
      {
         content_legacy = null;
      }

      public void OnAfterDeserialize()
      {
         if (!string.IsNullOrEmpty(content_legacy))
         {
            contentId = new ItemRef(content_legacy);
            content_legacy = null;
         }
      }
#pragma warning restore 618
      #endregion
   }

   [System.Serializable]
   public class OfferObtainItemProperty
   {
      [Tooltip(ContentObject.TooltipName1)]
      [CannotBeBlank]
      public string name;

      [Tooltip(ContentObject.TooltipValue1)]
      public string value;
   }

   [System.Serializable]
   public class ListingPrice : ISerializationCallbackReceiver
   {
      [FormerlySerializedAs("type")]
      [SerializeField, HideInInspector]
      [IgnoreContentField]
      private string typeOld;

      [Obsolete("Use 'priceType' instead")]
      public string type
      {
         get => priceType.ToString().ToLower();
         set => priceType = EnumConversionHelper.ParseEnumType<PriceType>(value);
      }

      [Tooltip(ContentObject.TooltipType1)]
      [MustBeNonDefault]
      [IgnoreContentField]
      public PriceType priceType;

      /// <summary>
      /// Don't use this field. It's used only for JSON serialization.
      /// </summary>
      [SerializeField, HideInInspector]
      [ContentField("type")]
      private string priceSerializedValue;
      
      [Tooltip(ContentObject.TooltipSymbol1)]
      [MustReferenceContent(false, typeof(CurrencyContent), typeof(SKUContent))]
      public string symbol;

      [Tooltip(ContentObject.TooltipAmount1)]
      [MustBeNonNegative]
      public int amount;

      public void OnBeforeSerialize()
      {
         priceSerializedValue = priceType.ToString().ToLower();
      }

      public void OnAfterDeserialize()
      {
         EnumConversionHelper.ConvertIfNotDoneAlready(ref priceType, ref typeOld);
      }
   }

   [System.Serializable]
   public class ActivePeriod
   {
      [Tooltip(ContentObject.TooltipStartDate1 + ContentObject.TooltipStartDate2)]
      [MustBeDateString]
      public string start;

      [Tooltip(ContentObject.TooltipOptional0 + ContentObject.TooltipEndDate1 + ContentObject.TooltipEndDate2)]
      [MustBeDateString]
      public OptionalString end;
   }

   [System.Serializable]
   public class OfferRequirement
   {
      [Tooltip(ContentObject.TooltipSymbol1)]
      [MustReferenceContent(AllowedTypes = new []{typeof(ListingContent)})]
      public string offerSymbol;

      [Tooltip(ContentObject.TooltipPurchase1)]
      public OfferConstraint purchases;
   }

   [System.Serializable]
   public class StatRequirement : ISerializationCallbackReceiver
   {
      // TODO: StatRequirement, by way of OptionalStats, is used by AnnouncementContent too. Should this be in a shared location? ~ACM 2021-04-22

      public StatRequirement()
      {
         domainCached = new OptionalString { Value = domainType.ToString().ToLower() };
         accessCached = new OptionalString { Value = accessType.ToString().ToLower() };
      }
      
      #region domain
      
      [FormerlySerializedAs("domain")]
      [SerializeField, HideInInspector]
      [IgnoreContentField]
      private OptionalString domainOld;

      private OptionalString domainCached;

      /// <summary>
      /// Don't use this field. It's used only for JSON serialization.
      /// </summary>
      [SerializeField, HideInInspector]
      [ContentField("domain")]
      private string domainSerializedValue;
      
      [Obsolete("Use 'domainType' instead")]
      public OptionalString domain
      {
         get => domainCached;
         set
         {
            domainType = EnumConversionHelper.ParseEnumType<DomainType>(value);
            domainCached.Value = domainType.ToString().ToLower();
         }
      }

      [Tooltip("Domain of the stat (e.g. 'platform', 'game', 'client'). Default is 'game'.")]
      [IgnoreContentField]
      public DomainType domainType;

      #endregion
      
      #region access
      
      [SerializeField, HideInInspector] [FormerlySerializedAs("access")]
      [IgnoreContentField]
      private OptionalString accessOld;

      private OptionalString accessCached;

      /// <summary>
      /// Don't use this field. It's used only for JSON serialization.
      /// </summary>
      [ContentField("access")] 
      [SerializeField, HideInInspector]
      private string accessSerializedValue;

      [Obsolete("Use 'accessType' instead")]
      public OptionalString access
      {
         get => accessCached;
         set
         {
            accessType = EnumConversionHelper.ParseEnumType<AccessType>(value);
            accessCached.Value = accessType.ToString().ToLower();
         }
      }

      [Tooltip("Visibility of the stat (e.g. 'private', 'public'). Default is 'private'.")]
      [IgnoreContentField]
      public AccessType accessType;
      
      #endregion

      [Tooltip(ContentObject.TooltipStat1)] [CannotBeBlank]
      public string stat;

      #region constraint
      
      [FormerlySerializedAs("constraint")] 
      [SerializeField, HideInInspector]
      [IgnoreContentField]
      private string constraintOld;

      /// <summary>
      /// Don't use this field. It's used only for JSON serialization.
      /// </summary>
      [SerializeField, HideInInspector]
      [ContentField("constraint")]
      private string constraintSerializedValue;
      
      [Obsolete("Use 'constraintType' instead")]
      public string constraint
      {
         get => constraintType.ToString().ToLower();
         set => constraintType = EnumConversionHelper.ParseEnumType<ComparatorType>(value);
      }

      [Tooltip(ContentObject.TooltipConstraint1)]
      [MustBeNonDefault]
      [IgnoreContentField]
      public ComparatorType constraintType;
      
      #endregion

      [Tooltip(ContentObject.TooltipValue1)] public int value;

      public void OnBeforeSerialize()
      {
         domainSerializedValue = domainType.ToString().ToLower();
         accessSerializedValue = accessType.ToString().ToLower();
         constraintSerializedValue = constraintType.ToString().ToLower();
      }

      public void OnAfterDeserialize()
      {
         EnumConversionHelper.ConvertIfNotDoneAlready(ref accessType, ref accessOld);
         EnumConversionHelper.ConvertIfNotDoneAlready(ref constraintType, ref constraintOld);
         EnumConversionHelper.ConvertIfNotDoneAlready(ref domainType, ref domainOld);
      }
   }

   [System.Serializable]
   public class CohortRequirement : ISerializationCallbackReceiver
   {
      [Tooltip(ContentObject.TooltipCohortTrial1)]
      [CannotBeBlank]
      public string trial;

      [Tooltip(ContentObject.TooltipCohort1)]
      [CannotBeBlank]
      public string cohort;

      [FormerlySerializedAs("constraint")]
      [SerializeField, HideInInspector]
      [IgnoreContentField]
      private string constraintOld;

      /// <summary>
      /// Don't use this field. It's used only for JSON serialization.
      /// </summary>
      [SerializeField, HideInInspector]
      [ContentField("constraint")]
      private string constraintSerializedValue;

      [Obsolete("Use 'constraintType' instead")]
      public string constraint
      {
         get => constraintType.ToString().ToLower();
         set => EnumConversionHelper.ParseEnumType<ComparatorType>(value);
      }

      [Tooltip(ContentObject.TooltipConstraint1)]
      [IgnoreContentField]
      public ComparatorType constraintType;

      public void OnBeforeSerialize()
      {
         constraintSerializedValue = constraintType.ToString().ToLower();
      }

      public void OnAfterDeserialize()
      {
         EnumConversionHelper.ConvertIfNotDoneAlready(ref constraintType, ref constraintOld);
      }
   }

   [System.Serializable]
   public class ContentDictionary
   {
      public List<KVPair> keyValues;
   }

   [System.Serializable]
   public class OfferConstraint : ISerializationCallbackReceiver
   {
      [FormerlySerializedAs("constraint"), HideInInspector]
      [IgnoreContentField]
      public string constraintOld;

      /// <summary>
      /// Don't use this field. It's used only for JSON serialization.
      /// </summary>
      [SerializeField, HideInInspector]
      [ContentField("constraint")]
      private string constraintSerializedValue;

      [Obsolete("Use 'constraintType' instead")]
      public string constraint
      {
         get => constraintType.ToString().ToLower();
         set => constraintType = EnumConversionHelper.ParseEnumType<ComparatorType>(value);
      }
      
      [Tooltip(ContentObject.TooltipConstraint1)]
      [IgnoreContentField]
      public ComparatorType constraintType;

      [Tooltip(ContentObject.TooltipValue1)]
      public int value;

      public void OnBeforeSerialize()
      {
         constraintSerializedValue = constraintType.ToString().ToLower();
      }

      public void OnAfterDeserialize()
      {
         EnumConversionHelper.ConvertIfNotDoneAlready(ref constraintType, ref constraintOld);
      }
   }
   [System.Serializable]
   public class OptionalColor : Optional<Color>
   {
      public static OptionalColor From(Color color)
      {
         return new OptionalColor {HasValue = true, Value = color};
      }
   }

   [System.Serializable]
   public class OptionalPeriod : Optional<ActivePeriod> { }

   [System.Serializable]
   public class OptionalStats : Optional<List<StatRequirement>> { }

   [System.Serializable]
   public class OptionalOffers : Optional<List<OfferRequirement>> { }

   [System.Serializable]
   public class OptionalDict : Optional<ContentDictionary> { }

   [System.Serializable]
   public class OptionalCohort : Optional<List<CohortRequirement>> {}
}
