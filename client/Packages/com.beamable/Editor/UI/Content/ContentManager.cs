using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Common.Content;
using Beamable.Common.Content.Validation;
using Beamable.Editor.Content.Models;
using Beamable.Platform.SDK;
using Beamable.Content;
using Beamable.Editor;
using Beamable.Editor.Content;
using Beamable.Editor.Modules.Account;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEditor;

namespace Beamable.Editor.Content
{
   public class ContentManager
   {
      public ContentDataModel Model { get; private set; } = new ContentDataModel();

      public void Initialize()
      {
         EditorAPI.Instance.Then(de =>
         {
            Model.ContentIO = de.ContentIO;


            Model.UserCanPublish = de.Permissions.CanPushContent;
            EditorAPI.Instance.Then(b =>
            {
               b.OnUserChange -= HandleOnUserChanged;
               b.OnUserChange += HandleOnUserChanged;
            });

            var localManifest = de.ContentIO.BuildLocalManifest();
            Model.SetLocalContent(localManifest);
            de.ContentIO.OnManifest.Then(manifest =>
            {
               Model.SetServerContent(manifest);
            });

            Model.OnSoftReset += () =>
            {
               var nextLocalManifest = de.ContentIO.BuildLocalManifest();
               Model.SetLocalContent(nextLocalManifest);
               RefreshServer();
            };

            Model.SetContentTypes(ContentRegistry.GetAll().ToList());

            ValidateContent(null, null); // start a validation in the background.

            ContentIO.OnContentCreated += ContentIO_OnContentCreated;
            ContentIO.OnContentDeleted += ContentIO_OnContentDeleted;
            ContentIO.OnContentRenamed += ContentIO_OnContentRenamed;
         });
      }

      public void RefreshServer()
      {
         EditorAPI.Instance.Then(de =>
         {
            de.ContentIO.FetchManifest().Then(manifest =>
            {
               Model.SetServerContent(manifest);
            });
         });
      }

      public IContentObject AddItem()
      {
         TreeViewItem selectedTreeViewItem = Model.SelectedContentTypes.FirstOrDefault();
         ContentTypeTreeViewItem selectedContentTypeTreeViewItem = (ContentTypeTreeViewItem)selectedTreeViewItem;

         if (selectedContentTypeTreeViewItem == null)
         {
            BeamableLogger.LogError(new Exception("AddItem() failed. selectedContentTypeTreeViewItem must not be null."));
            return null;
         }

         return AddItem(selectedContentTypeTreeViewItem.TypeDescriptor);
      }

      public IContentObject AddItem(ContentTypeDescriptor typeDescriptor)
      {
         var itemType = typeDescriptor.ContentType;
         var itemName = ContentManagerConstants.GetNameForNewContentFileByType(itemType);
         ContentObject content = ScriptableObject.CreateInstance(itemType) as ContentObject;
         content.SetContentName(itemName);

         Model.CreateItem(content);
         return content;
      }

      public Promise<List<ContentExceptionCollection>> ValidateContent(HandleContentProgress progressHandler, HandleValidationErrors errorHandler)
      {
         return EditorAPI.Instance.FlatMap(de =>
         {
            var contentValidator = new ContentValidator(de.ContentIO);
            var ctx = de.ContentIO.GetValidationContext();
            ContentObject.ValidationContext = ctx;
            var promise = contentValidator.Validate(ctx, Model.TotalContentCount, Model.GetAllContents(), progressHandler, errorHandler);
            return promise;
         });
      }

      public Promise<Unit> PublishContent(ContentPublishSet publishSet, HandleContentProgress progressHandler, HandleDownloadFinished finishedHandler)
      {
         return EditorAPI.Instance.FlatMap(de =>
         {
            var promise = de.ContentPublisher.Publish(publishSet, progress =>
            {
               progressHandler?.Invoke(progress.Progress, progress.CompletedOperations, progress.TotalOperations);
            });

            finishedHandler?.Invoke(promise);
            return promise.Map(_ =>
            {
               de.ContentIO.FetchManifest();
               return _;
            });
         });
      }


      public Promise<Unit> DownloadContent(DownloadSummary summary, HandleContentProgress progressHandler, HandleDownloadFinished finishedHandler)
      {
         return EditorAPI.Instance.FlatMap(de =>
         {
            var contentDownloader = new ContentDownloader(de.Requester, de.ContentIO);
            //Disallow updating anything while importing / refreshing
            var downloadPromise = contentDownloader.Download(summary, progressHandler);

            finishedHandler?.Invoke(downloadPromise);
            return downloadPromise;
         });
      }

      /// <summary>
      /// Refresh the data and thus rendering of the <see cref="ContentManagerWindow"/>
      /// </summary>
      /// <param name="isHardRefresh">TODO: My though there is that false means keep the currently selected item. TBD if possible. - srivello</param>
      public void RefreshWindow(bool isHardRefresh)
      {
         if (isHardRefresh)
         {
            ContentManagerWindow.Instance.Refresh();
         }
         else
         {
            RefreshServer();
         }
      }

      public void ShowDocs()
      {
         BeamableLogger.Log("ShowDocs");
         Application.OpenURL(BeamableConstants.URL_TOOL_WINDOW_CONTENT_MANAGER);
      }

      private void ContentIO_OnContentDeleted(IContentObject content)
      {
         Model.HandleContentDeleted(content);
      }

      private void ContentIO_OnContentCreated(IContentObject content)
      {
         Model.HandleContentAdded(content);
      }

      private void ContentIO_OnContentRenamed(string oldId, IContentObject content, string nextAssetPath)
      {
         Model.HandleContentRenamed(oldId, content, nextAssetPath);
      }

      public Promise<DownloadSummary> PrepareDownloadSummary(params ContentItemDescriptor[] filter)
      {
         // no matter what, we always want a fresh manifest locally and from the server.
         return EditorAPI.Instance.FlatMap(de =>
         {
            return de.ContentIO.FetchManifest().Map(serverManifest =>
            {
               var localManifest = de.ContentIO.BuildLocalManifest();



               return new DownloadSummary(de.ContentIO, localManifest, serverManifest, filter.Select(x => x.Id).ToArray());
            });
         });
      }

      public Promise<DownloadSummary> PrepareDownloadSummary(string[] ids)
      {
         return EditorAPI.Instance.FlatMap(de =>
         {
             return de.ContentIO.FetchManifest().Map(serverManifest =>
             {
                 var localManifest = de.ContentIO.BuildLocalManifest();
                 return new DownloadSummary(de.ContentIO, localManifest, serverManifest, ids);
             });
         });
      }

      public void Destroy()
      {
         EditorAPI.Instance.Then(b => b.OnUserChange -= HandleOnUserChanged);
         ContentIO.OnContentCreated -= ContentIO_OnContentCreated;
         ContentIO.OnContentDeleted -= ContentIO_OnContentDeleted;
         ContentIO.OnContentRenamed -= ContentIO_OnContentRenamed;
      }

      private void HandleOnUserChanged(EditorUser user)
      {
	      EditorAPI.Instance.Then(de =>
	      {
		      Model.UserCanPublish = de.Permissions.CanPushContent;
	      });
      }

      public Promise<ContentPublishSet> CreatePublishSet(bool newNamespace=false)
      {
         var manifestId = newNamespace
            ? Guid.NewGuid().ToString()
            : null;
         return EditorAPI.Instance.FlatMap(de => de.ContentPublisher.CreatePublishSet(manifestId));

      }
   }
}
