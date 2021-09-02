using System;
using System.Collections.Generic;
using Beamable.Common.Content;
using Beamable.Common.Inventory;
using Beamable.Content;
using Beamable.Editor.Content;
using Beamable.Inventory;
using NUnit.Framework;

namespace Beamable.Editor.Tests.Beamable.Content.ContentQueryTests
{
   public class ToStringTests
   {
      [SetUp]
      public void Setup()
      {
         ContentRegistry.LoadRuntimeTypeData();
      }

      [Test]
      public void AddOn_Tag()
      {
         var existing = "foobar";
         var queryAdd = new ContentQuery {TagConstraints = new HashSet<string> {"a"}};
         var str = queryAdd.ToString(existing);
         Assert.AreEqual("foobar, tag:a", str);
      }

      [Test]
      public void AddOn_Tag_IdOverlapsTag()
      {
         var existing = "a";
         var queryAdd = new ContentQuery {TagConstraints = new HashSet<string> {"a"}};
         var str = queryAdd.ToString(existing);
         Assert.AreEqual("a, tag:a", str);
      }

      [Test]
      public void AddOn_Id()
      {
         var existing = "tag:a";
         var queryAdd = new ContentQuery {IdContainsConstraint = "foo"};
         var str = queryAdd.ToString(existing);
         Assert.AreEqual("tag:a, foo", str);
      }

      [Test]
      public void AddOn_PartialRule_Tag()
      {
         var existing = "tag:";
         var queryAdd = new ContentQuery {IdContainsConstraint = "foo"};
         var str = queryAdd.ToString(existing);
         Assert.AreEqual("tag:, foo", str);
      }

      [Test]
      public void SimpleId()
      {
         var query = new ContentQuery {IdContainsConstraint = "foo"};
         var str = query.ToString();

         Assert.AreEqual("foo", str);
      }

      [Test]
      public void SimpleTag()
      {
         var query = new ContentQuery {TagConstraints = new HashSet<string>{"a"}};
         var str = query.ToString();

         Assert.AreEqual("tag:a", str);
      }

      [Test]
      public void MultipleTags()
      {
         var query = new ContentQuery {TagConstraints = new HashSet<string>{"a", "b"}};
         var str = query.ToString();

         Assert.AreEqual("tag:a b", str);
      }

      [Test]
      public void SimpleType()
      {
         var query = new ContentQuery {TypeConstraints = new HashSet<Type>{typeof(ItemContent)}};
         var str = query.ToString();

         Assert.AreEqual("t:items", str);
      }

      [Test]
      public void ManyClauses()
      {
         var query = new ContentQuery {TypeConstraints = new HashSet<Type>{typeof(ItemContent)}, IdContainsConstraint = "foo", TagConstraints = new HashSet<string>{"a", "b"}};
         var str = query.ToString();

         Assert.AreEqual("tag:a b, t:items, foo", str);
      }
   }
}