using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Content;
using Beamable.Editor.Content;
using Beamable.Editor.Content.SaveRequest;
using Beamable.Editor.Tests.Beamable.Content.ContentIOTests;
using Beamable.Platform.SDK;
using Beamable.Platform.Tests;
using Beamable.Tests;
using NUnit.Framework;
using UnityEngine.TestTools;
using Manifest = Beamable.Editor.Content.Manifest;

namespace Beamable.Editor.Tests.Beamable.Content.ContentPublisherTests
{
   public class PublishTests
   {
      private ContentPublisher _publisher;
      private MockContentIO _mockContentIo;
      private IEnumerable<ContentObject> _content;
      private List<ContentManifestReference> _serverContent;
      private MockPlatformRequester _requester;
      private ExampleContent _exampleContent;
      private ContentReference _exampleContentReference;

      [SetUp]
      public void Init()
      {
         _exampleContent = ContentObject.Make<ExampleContent>("test");
         _exampleContentReference = new ContentReference()
         {
            checksum = "fake",
            id = _exampleContent.Id,
            uri = "somewhere.come",
            version = "123",
            visibility = "public"
         };
         _content = new List<ContentObject>() { };
         _requester = new MockPlatformRequester();
         _serverContent = new List<ContentManifestReference>();
         _mockContentIo = new MockContentIO();

         _mockContentIo.ChecksumResult = c => c.Id.Equals(_exampleContent.Id) ? _exampleContentReference.checksum : "";

         _publisher = new ContentPublisher(_requester, _mockContentIo);
      }

      [TearDown]
      public void CleanUp()
      {
         _requester.Reset();
         _serverContent.Clear();
      }
//
//      [UnityTest]
//      public IEnumerator RemovesDeletedItemsFromManifest()
//      {
//         _requester.Reset();
//         _serverContent.Clear();
//         _serverContent.Add(new ContentManifestReference()
//         {
//            id = _exampleContentReference.id,
//            version = _exampleContentReference.version + "difference",
//            checksum = _exampleContentReference.checksum + "difference",
//            visibility = _exampleContentReference.visibility
//         });
//
//         var set = new ContentPublishSet()
//         {
//            ToModify = new List<ContentObject>(),
//            ToDelete = new List<string> { _exampleContentReference.id },
//            ToAdd = new List<ContentObject>(),
//            ServerManifest = new Editor.Content.Manifest(_serverContent)
//         };
//
//         bool manifestWasSaved = false;
//
//         _requester.RegisterMockRequestJson((method, uri, body, header) =>
//         {
//            if (body is ManifestSaveRequest manifest)
//            {
//               manifestWasSaved = true;
//               Assert.AreEqual(0, manifest.References.Count);
//            }
//
//            return Promise<ContentManifest>.Successful(new ContentManifest());
//         });
//
//
//         yield return _publisher.Publish(set, p => { }).Then(_ =>
//         {
//            Assert.IsTrue(manifestWasSaved);
//         }).AsYield();
//      }
//
//      [UnityTest]
//      public IEnumerator SendsRequestForModification()
//      {
//               _requester.Reset();
//               _serverContent.Clear();
//         _serverContent.Add(new ContentManifestReference()
//         {
//            id = _exampleContentReference.id,
//            version = _exampleContentReference.version + "difference",
//            checksum = _exampleContentReference.checksum + "difference",
//            visibility = _exampleContentReference.visibility
//         });
//
//         var set = new ContentPublishSet()
//         {
//            ToModify = new List<ContentObject>() { _exampleContent},
//            ToDelete = new List<string>(),
//            ToAdd = new List<ContentObject>(),
//            ServerManifest = new Editor.Content.Manifest(_serverContent)
//         };
//
//         var modificationWasCalled = false;
//         var manifestWasSaved = false;
//         _requester.RegisterMockRequestJson((method, uri, body, header) =>
//         {
//            if (body is ContentSaveRequest request)
//            {
//               modificationWasCalled = true;
//               // notably, the checksum of the modified content is *NOT* what the existing server manifest was...
//               Assert.AreEqual(_exampleContentReference.checksum, request.Content.First().Checksum);
//               Assert.AreEqual(_exampleContent.Id, request.Content.First().Id);
//            }
//
//            return Promise<ContentSaveResponse>.Successful(new ContentSaveResponse()
//            {
//               content = new List<ContentReference>
//               {
//                  _exampleContentReference
//               }
//            });
//         });
//
//         _requester.RegisterMockRequestJson((method, uri, body, header) =>
//         {
//            if (body is ManifestSaveRequest manifest)
//            {
//               manifestWasSaved = true;
//               Assert.AreEqual(1, manifest.References.Count);
//               Assert.AreEqual(_exampleContentReference.checksum, manifest.References.First().Checksum);
//            }
//            return Promise<ContentManifest>.Successful(new ContentManifest());
//         });
//
//         yield return _publisher.Publish(set, prog => { }).Then(_ =>
//         {
//            Assert.IsTrue(modificationWasCalled);
//            Assert.IsTrue(manifestWasSaved);
//         }).AsYield();
//      }
//
//      [UnityTest]
//      public IEnumerator SendsRequestForAdditions()
//      {
//         var set = new ContentPublishSet()
//         {
//            ToAdd = new List<ContentObject>() { _exampleContent},
//            ToDelete = new List<string>(),
//            ToModify = new List<ContentObject>(),
//            ServerManifest = new Editor.Content.Manifest(_serverContent)
//         };
//         var additionWasCalled = false;
//         var manifestWasSaved = false;
//
//         _requester.RegisterMockRequestJson((method, uri, body, header) =>
//         {
//            if (body is ContentSaveRequest request)
//            {
//               additionWasCalled = true;
//               Assert.AreEqual(_exampleContent.Id, request.Content.First().Id);
//            }
//
//            return Promise<ContentSaveResponse>.Successful(new ContentSaveResponse()
//            {
//               content = new List<ContentReference>
//               {
//                  _exampleContentReference
//               }
//            });
//         });
//
//         _requester.RegisterMockRequestJson((method, uri, body, header) =>
//         {
//            if (body is ManifestSaveRequest manifest)
//            {
//               manifestWasSaved = true;
//               Assert.AreEqual(1, manifest.References.Count);
//               Assert.AreEqual(_exampleContentReference.checksum, manifest.References.First().Checksum);
//            }
//            return Promise<ContentManifest>.Successful(new ContentManifest());
//         });
//
//         yield return _publisher.Publish(set, prog => { }).Then(_ =>
//         {
//            Assert.IsTrue(additionWasCalled);
//            Assert.IsTrue(manifestWasSaved);
//         }).AsYield();
//      }
//
//      [UnityTest]
//      public IEnumerator SavesExistingManifestOnNoChanges()
//      {
//         var checksum = "fake";
//
//         _serverContent.Add(new ContentManifestReference()
//         {
//            id = "example.id",
//            version = "public",
//            checksum = checksum
//         });
//
//         bool _manifestWasSaved = false;
//
//         _requester.RegisterMockRequestJson((method, uri, body, header) =>
//         {
//            if (body is ManifestSaveRequest manifest)
//            {
//               _manifestWasSaved = true;
//               Assert.AreEqual(1, manifest.References.Count);
//               Assert.AreEqual(checksum, manifest.References.First().Checksum);
//            }
//
//            return Promise<ContentManifest>.Successful(new ContentManifest());
//         });
//
//
//         var set = new ContentPublishSet()
//         {
//            ToAdd = new List<ContentObject>(),
//            ToModify = new List<ContentObject>(),
//            ToDelete = new List<string>(),
//            ServerManifest = new Editor.Content.Manifest(_serverContent)
//         };
//
//         yield return _publisher.Publish(set, prog => { }).Then(_ =>
//         {
//            Assert.IsTrue(_manifestWasSaved);
//         }).AsYield();
//      }
   }
}