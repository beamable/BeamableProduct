using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Util;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.UI.ContentWindow
{
	public partial class ContentWindow
	{
		public const string _legacyContentFolder = "Assets/Beamable/Editor/content";

		public bool NeedsMigration => (_legacyContentGuids?.Length ?? 0) > 0;
		public string[] _legacyContentGuids;

		public bool IsMigrating => _migrationPromise != null;
		
		private Promise _currentOperation;
		private SequencePromise<ContentMigrationResult> _migrationPromise;
		private SequencePromise<Unit> _deletePromise;

		private GUIStyle _contentHeaderStyle;
		private GUIStyle _contentHeaderDescriptionStyle;
		private GUIStyle _migrationErrorStyle;
		
		public void FindLegacyContent()
		{
			#if BEAM_IGNORE_CONTENT_MIGRATION
			return;
			#endif
			
			var sw = new Stopwatch();
			sw.Start();
			if (Directory.Exists(_legacyContentFolder))
			{
				_legacyContentGuids = BeamableAssetDatabase.FindAssets<ContentObject>(new string[]{_legacyContentFolder});
			}
			else
			{
				_legacyContentGuids = Array.Empty<string>();
			}
			sw.Stop();
			// Debug.Log($"Content migration detection took {sw.Elapsed.TotalMilliseconds}ms");
		}

		public void DrawMigration()
		{

			if (IsMigrating == false)
			{
				DrawMigration_Prepare();
			}
			else
			{
				DrawMigration_Active();
			}
			
		}

		public void BuildMigrationStyles()
		{
			if (_contentHeaderStyle == null)
			{
				_contentHeaderStyle = new GUIStyle(EditorStyles.largeLabel)
				{
					fontSize = 18,
					fixedHeight = 22
				};
			}

			if (_contentHeaderDescriptionStyle == null)
			{
				_contentHeaderDescriptionStyle = new GUIStyle(EditorStyles.label)
				{
					wordWrap = true,
					richText = true,
					fixedHeight = 0
				};
			}

			if (_migrationErrorStyle == null)
			{
				_migrationErrorStyle = new GUIStyle(EditorStyles.miniLabel)
				{
					normal = new GUIStyleState
					{
						textColor = new Color(1,.1f, .1f, .9f),
						
					}
				};
			}
		}

		public void DrawMigration_Active()
		{
			
			
			var headerContent = new GUIContent("Running Content Migration");
			var headerRect = GUILayoutUtility.GetRect(headerContent, _contentHeaderStyle);
			EditorGUI.LabelField(headerRect, headerContent, _contentHeaderStyle);
			GUILayout.Space(12);

			var content = new GUIContent(
				"Your content files are being migrated. Once all content is migrated, the old files will be removed. ");
	
			var rect = GUILayoutUtility.GetRect(content, _contentHeaderDescriptionStyle);
	
			EditorGUI.LabelField(rect, content.text, _contentHeaderDescriptionStyle);
			GUILayout.Space(12);
			
			BeamGUI.DrawLoadingBar("Move Content", _migrationPromise.Ratio, _migrationPromise.IsFailed);

			BeamGUI.DrawLoadingBar("Remove Old Content", _deletePromise?.Ratio ?? 0, _deletePromise?.IsFailed ?? false);

			var isMigrationFinished = _migrationPromise.IsCompleted && (_deletePromise?.IsCompleted ?? false);

			if (isMigrationFinished)
			{
				GUILayout.Space(12);
				content = new GUIContent(
					"The migration has finished. Your new files are in the <i>.beamable/content</i> folder. ");
	
				rect = GUILayoutUtility.GetRect(content, _contentHeaderDescriptionStyle);
				EditorGUI.LabelField(rect, content.text, _contentHeaderDescriptionStyle);
			} else if (_migrationPromise.IsFailed)
			{
				GUILayout.Space(12);
				content = new GUIContent(
					"The migration has failed. Please review the console logs and try again.  ");
	
				rect = GUILayoutUtility.GetRect(content, _contentHeaderDescriptionStyle);
				EditorGUI.LabelField(rect, content.text, new GUIStyle(_contentHeaderDescriptionStyle)
				{
					normal = new GUIStyleState
					{
						textColor = Color.red
					}
				});
			}
			
			GUILayout.FlexibleSpace();


			var gotoNewContentManager = false;
			if (_migrationPromise.IsFailed)
			{
				if (BeamGUI.CancelButton("Go back"))
				{
					_migrationPromise = null;
					_deletePromise = null;
					_legacyContentGuids = null;
					FindLegacyContent();
				}
			}
			else
			{
				BeamGUI.ShowDisabled(isMigrationFinished, () =>
				{
					gotoNewContentManager = BeamGUI.PrimaryButton(new GUIContent("Goto new Content Manager"));
				});
			}
			
			if (gotoNewContentManager)
			{
				_migrationPromise = null;
				_deletePromise = null;
				_legacyContentGuids = null;
				AssetDatabase.Refresh();
			}
		
		}
		
		public void DrawMigration_Prepare()
		{
			
			
			var headerContent = new GUIContent("Please migrate your Content");
			var headerRect = GUILayoutUtility.GetRect(headerContent, _contentHeaderStyle);
			EditorGUI.LabelField(headerRect, headerContent, _contentHeaderStyle);
			GUILayout.Space(12);

			var content = new GUIContent(
				"Previously, <i>Beamable Content</i> was managed directly as <i>Scriptable Objects</i> " +
				"stored in a Unity Editor folder, and accessed with Unity's <i>Asset Database</i>. " +
				"Now, your editor content lives outside of Unity's asset pipeline, in the <i>.beamable</i> folder. " +
				"In addition to editing content in Unity, you can edit these files directly, and manage them with the Beamable CLI. " +
				"\n" +
				"\n" +
				"The content files need to be moved from <i>ScriptableObject</i> format into the raw JSON format. " +
				"\n" +
				"\n" +
				"Click the <i>Migrate Content</i> button to automatically move your <i>ScriptableObject</i> files to the new format.");
	
			var rect = GUILayoutUtility.GetRect(content, _contentHeaderDescriptionStyle);
	
			EditorGUI.LabelField(rect, content.text, _contentHeaderDescriptionStyle);
			GUILayout.FlexibleSpace();

			
			var clickedMigrate = BeamGUI.PrimaryButton(new GUIContent("Migrate Content"));
			if (clickedMigrate)
			{
				const int numberOfWorkerCoroutines = 25;
				
				var _ = _contentService.TempDisableWatcher(async () =>
				{
					
					_migrationPromise = PromiseEditor.ExecuteOnRoutines(numberOfWorkerCoroutines, _legacyContentGuids.Select<string, Func<Promise<ContentMigrationResult>>>(legacyContentGuid =>
					{
						return () => MigrateContent(legacyContentGuid);
					}).ToList()).OnElementSuccess(_ =>
					{
						Repaint();
					});
					await _migrationPromise.Then(_ =>
					{
						_deletePromise = PromiseEditor.ExecuteOnRoutines<Unit>(
							numberOfWorkerCoroutines, _migrationPromise.SuccessfulResults.Select<ContentMigrationResult, Func<Promise<Unit>>>(
								path =>
								{
									return () => DeleteOldContent(path.originalAssetPath);
								}).ToList());
						_deletePromise.Then(_ =>
						{
							PostDeletionNotice();
						});
					});

					await _migrationPromise;
					await _deletePromise;
					
					
				});
				

			}
		}
		
		Promise DeleteOldContent(string path)
		{
			File.Delete(path);
			File.Delete(path + ".meta");
			return Promise.Success;
		}

		void PostDeletionNotice()
		{
			var path = Path.Combine(_legacyContentFolder, "content-migration-notice.txt");
			File.WriteAllText(path, 
			                  @$"# Beamable Content Migration Notice
On {DateTimeOffset.Now:s}, the content was migrated from Unity Scriptable Object 
representation to raw JSON representation. The content now lives in the .beamable/content folder
for this project. 

If you go to the new Beamable Content Manager, and no longer see the Migration 
workflow, then you may safely delete the {_legacyContentFolder} folder. ");
		}
		
		async Promise<ContentMigrationResult> MigrateContent(string guid)
		{
			// await Promise.Success;
			// throw new Exception("forced failure");

			var assetPath = AssetDatabase.GUIDToAssetPath(guid);
			var content = AssetDatabase.LoadAssetAtPath<ContentObject>(assetPath);
			var name = Path.GetFileNameWithoutExtension(assetPath);
			content.SetContentName(name);
			
			Debug.Log("beam migrating " + content.Id);
			
			string propertiesJson = ClientContentSerializer.SerializeProperties(content);
			var saveCommand = ActiveContext.Cli.ContentSave(new ContentSaveArgs()
			{
				contentIds = new[] {content.Id},
				contentProperties = new[] {propertiesJson}
			});
			await saveCommand.Run().Error(dp =>
			{
				Debug.LogError($"Failed to save content=[{content.Id}]. message=[{dp.Message}]");
			});

			if (content.Tags?.Length > 0)
			{
				var setContentTagCommand = ActiveContext.Cli.ContentTagSet(new ContentTagSetArgs()
				{
					filterType = ContentFilterType.ExactIds,
					filter = content.Id,
					tag = string.Join(",", content.Tags)
				});
				await setContentTagCommand.Run().Error(dp =>
				{
					Debug.LogError($"Failed to tag content=[{content.Id}]. message=[{dp.Message}]");
				});
			} 
		
			return new ContentMigrationResult
			{
				originalAssetPath = assetPath
			};
		}

		[Serializable]
		public struct ContentMigrationResult
		{
			public string originalAssetPath;
		}
	}
}
