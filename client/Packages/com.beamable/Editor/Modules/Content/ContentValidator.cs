using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Content.Validation;
using Beamable.Editor.Content.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.Content
{
	public delegate void HandleValidationErrors(ContentExceptionCollection errors);

	public class ContentValidator
	{
		private readonly ContentIO _io;

		public ContentValidator(ContentIO io)
		{
			_io = io;
		}

		public Promise<List<ContentExceptionCollection>> Validate(IValidationContext ctx,
																  int allContentCount,
																  IEnumerable<ContentItemDescriptor> allContent,
																  HandleContentProgress progressHandler = null,
																  HandleValidationErrors errorHandler = null)
		{
			progressHandler?.Invoke(0, 0, allContentCount);
			var count = 0f;
			var exceptionList = new List<ContentExceptionCollection>();
			var promise = new Promise<List<ContentExceptionCollection>>();

			EditorCoroutineUtility.StartCoroutine(Routine(), this);

			IEnumerator Routine()
			{
				var numberOfUpdatesBeforeRenderFrame = 30;
				if (allContentCount < 30)
				{
					numberOfUpdatesBeforeRenderFrame = allContentCount / 5;
				}
				var n = 0;
				foreach (var contentDescriptor in allContent)
				{
					try
					{
						if (contentDescriptor.LocalStatus != HostStatus.AVAILABLE)
							continue; // cannot validate server content. (yet?)

						var localObject = contentDescriptor.GetContent() as ContentObject;
						if (localObject == null)
						{
							Debug.LogWarning("Failed to load local content " + contentDescriptor.Id);
							continue;
						}

						n++;

						ContentExceptionCollection collection = null;
						if (localObject.HasValidationExceptions(ctx, out var exceptions))
						{
							contentDescriptor.EnrichWithValidationErrors(exceptions);
							collection = new ContentExceptionCollection(localObject, exceptions);
							errorHandler?.Invoke(collection);
							exceptionList.Add(collection);
						}
						else
						{
							contentDescriptor.EnrichWithNoValidationErrors();
						}

						count += 1;
						var progress = count / allContentCount;
						progressHandler?.Invoke(progress, (int)count, allContentCount);

					}
					catch (Exception ex)
					{
						Debug.LogError($"Failed content validation - ");
						Debug.LogException(ex);
					}

					if (numberOfUpdatesBeforeRenderFrame != 0 && (n % numberOfUpdatesBeforeRenderFrame == 0))
					{
						yield return null; // delay a frame
					}
				}

				promise.CompleteSuccess(exceptionList);
				progressHandler?.Invoke(1, allContentCount, allContentCount);
				yield return null;
			}

			return promise;
		}

	}
}
