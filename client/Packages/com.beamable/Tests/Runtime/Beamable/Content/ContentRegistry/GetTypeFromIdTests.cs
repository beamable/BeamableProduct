using System;
using System.Collections.Generic;
using Beamable.Common.Content;
using Beamable.Tests.Content.Serialization.Support;
using Beamable.Content;
using Beamable.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Beamable.Tests.Content.ContentRegistryTests
{
   public class GetTypeFromIdTests
   {
      [Test]
      public void NonNested_Simple()
      {
         ContentRegistry.LoadRuntimeTypeData(new HashSet<Type>(){typeof(SimpleContent)});

         var type = ContentRegistry.GetTypeFromId("simple.foo");

         Assert.AreEqual(typeof(SimpleContent), type);
      }

      [Test]
      public void Polymorphic_Simple()
      {
         ContentRegistry.LoadRuntimeTypeData(new HashSet<Type>(){typeof(SimpleContent), typeof(SimpleSubContent)});

         var type = ContentRegistry.GetTypeFromId("simple.sub.foo");

         Assert.AreEqual(typeof(SimpleSubContent), type);
      }

      [Test]
      public void NonNested_MissingType()
      {
         ContentRegistry.LoadRuntimeTypeData(new HashSet<Type>(){});

         var type = ContentRegistry.GetTypeFromId("simple.foo");

         Assert.AreEqual(typeof(ContentObject), type);
      }

      [Test]
      public void Polymorphic_MissingAllTypes()
      {
         ContentRegistry.LoadRuntimeTypeData(new HashSet<Type>(){});

         var type = ContentRegistry.GetTypeFromId("simple.sub.foo");

         Assert.AreEqual(typeof(ContentObject), type);
      }

      [Test]
      public void Polymorphic_MissingSubType()
      {
         ContentRegistry.LoadRuntimeTypeData(new HashSet<Type>(){typeof(SimpleContent)});

         var type = ContentRegistry.GetTypeFromId("simple.sub.foo");

         Assert.AreEqual(typeof(SimpleContent), type);
      }

      [Test]
      public void FormerlySerializedAs_Simple()
      {
         ContentRegistry.LoadRuntimeTypeData(new HashSet<Type>(){typeof(SimpleFormerlyContent)});

         var type = ContentRegistry.GetTypeFromId("oldschool.foo");

         Assert.AreEqual(typeof(SimpleFormerlyContent), type);
      }

      [Test]
      public void FormerlySerializedAs_Many()
      {
         ContentRegistry.LoadRuntimeTypeData(new HashSet<Type>(){typeof(ManyFormerlyContent)});

         var type = ContentRegistry.GetTypeFromId("cool.foo");

         Assert.AreEqual(typeof(ManyFormerlyContent), type);
      }


      [Test]
      public void FormerlySerializedAs_Polymorphic()
      {
         ContentRegistry.LoadRuntimeTypeData(new HashSet<Type>(){typeof(SimpleFormerlyContent), typeof(SubFormerlyContent)});

         var type1 = ContentRegistry.GetTypeFromId("oldschool.oldsub.foo");
         var type2 = ContentRegistry.GetTypeFromId("simple.oldsub.foo");
         var type3 = ContentRegistry.GetTypeFromId("oldschool.sub.foo");
         var type4 = ContentRegistry.GetTypeFromId("simple.sub.foo");
         Assert.AreEqual(typeof(SubFormerlyContent), type1);
         Assert.AreEqual(typeof(SubFormerlyContent), type2);
         Assert.AreEqual(typeof(SubFormerlyContent), type3);
         Assert.AreEqual(typeof(SubFormerlyContent), type4);
      }

      [Test]
      public void FormerlySerializedAs_Missing()
      {
         ContentRegistry.LoadRuntimeTypeData(new HashSet<Type>(){});

         var type = ContentRegistry.GetTypeFromId("oldschool.foo");
         Assert.AreEqual(typeof(ContentObject), type);
      }

      [Test]
      public void FormerlySerializedAs_Missing_Polymorphic()
      {
         ContentRegistry.LoadRuntimeTypeData(new HashSet<Type>(){typeof(SimpleFormerlyContent)});

         var type1 = ContentRegistry.GetTypeFromId("oldschool.oldsub.foo");
         var type2 = ContentRegistry.GetTypeFromId("simple.oldsub.foo");
         var type3 = ContentRegistry.GetTypeFromId("oldschool.sub.foo");
         Assert.AreEqual(typeof(SimpleFormerlyContent), type1);
         Assert.AreEqual(typeof(SimpleFormerlyContent), type2);
         Assert.AreEqual(typeof(SimpleFormerlyContent), type3);
      }

      [Serializable]
      [ContentType("simple")]
      [ContentFormerlySerializedAs("oldschool")]
      class SimpleFormerlyContent : TestContentObject
      {

      }

      [Serializable]
      [ContentType("sub")]
      [ContentFormerlySerializedAs("oldsub")]
      class SubFormerlyContent : SimpleFormerlyContent
      {

      }

      [Serializable]
      [ContentType("simple")]
      [ContentFormerlySerializedAs("oldschool")]
      [ContentFormerlySerializedAs("cool")]
      class ManyFormerlyContent : TestContentObject
      {

      }

      [Serializable]
      [ContentType("simple")]
      class SimpleContent : TestContentObject
      {

      }

      [Serializable]
      [ContentType("sub")]
      class SimpleSubContent : SimpleContent
      {

      }
   }
}