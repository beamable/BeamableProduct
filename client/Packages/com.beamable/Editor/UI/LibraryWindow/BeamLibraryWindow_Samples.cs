using Beamable.Editor.Util;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace Beamable.Editor.Library
{
	public partial class BeamLibraryWindow
	{
		public Vector2 sampleScrollPosition;

		public Dictionary<string, Texture> loadedTextures = new Dictionary<string, Texture>();

		Texture LoadTexture(string path)
		{
			if (string.IsNullOrEmpty(path)) return null;

			if (!loadedTextures.TryGetValue(path, out var texture))
			{
				texture = loadedTextures[path] = EditorResources.Load<Texture>(path);
			}

			return texture;
		}

		const int minWidth = 240;
		const int maxWidth = 300;
		const int minHeight = 200;
		const int maxHeight = 280;

		void DrawSampleSection()
		{
			var width = position.width - 38;
			sampleScrollPosition = EditorGUILayout.BeginScrollView(sampleScrollPosition);
			EditorGUILayout.BeginVertical(new GUIStyle
			{
				margin = new RectOffset(12, 12, 12, 12)
			});

			var count = library.lightbeams.Count;
			var widthPerCard = Mathf.Clamp(width / count, minWidth, maxWidth);
			var cardsPerRow = Mathf.FloorToInt(width / widthPerCard);
			widthPerCard = Mathf.Min(maxWidth, width / cardsPerRow);
			var heightPerCard = Mathf.Clamp(widthPerCard, minHeight, maxHeight);
			var rows = Mathf.CeilToInt(count / (float)cardsPerRow);


			var xOffset = (width * .5f) - (widthPerCard * .5f * cardsPerRow); // center the cards
			var index = 0;
			var cardRects = new List<Rect>();

			{ // layout the rects
				for (var r = 0; r < rows && index < library.lightbeams.Count; r++)
				{
					EditorGUILayout.BeginHorizontal();
					for (var c = 0; c < cardsPerRow && index < library.lightbeams.Count; c++, index++)
					{

						var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
															GUILayout.Width(widthPerCard),
															GUILayout.Height(heightPerCard),
															GUILayout.ExpandWidth(false));

						int padding = 8;
						var padded = new Rect(rect.x + padding + xOffset, rect.y + padding, rect.width - padding * 2,
											  rect.height - padding * 2);
						cardRects.Add(padded);
					}

					EditorGUILayout.EndHorizontal();
				}
			}

			{ // draw drop shadow
				for (var i = 0; i < library.lightbeams.Count; i++)
				{
					var rect = cardRects[i];

					var shadowOffset = 24;
					var shadowRect = new Rect(rect.x - shadowOffset, rect.y + shadowOffset, rect.width, rect.height);
					GUI.DrawTexture(shadowRect, BeamGUI.iconShadowSoftA, ScaleMode.StretchToFill, true);

					shadowOffset = 6;
					shadowRect = new Rect(rect.x - shadowOffset, rect.y + shadowOffset, rect.width, rect.height);
					GUI.DrawTexture(shadowRect, BeamGUI.iconShadowSoftA, ScaleMode.StretchToFill, true);
				}
			}

			{ // draw the actual card content
				for (var i = 0; i < library.lightbeams.Count; i++)
				{
					var rect = cardRects[i];
					DrawSample(rect, library.lightbeams[i]);
				}
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();


		}

		void DrawSample(Rect bounds, LightbeamSampleInfo lightBeam)
		{
			bool clickedOpen = false;
			bool clickedMore = false;
			bool clickedDocs = false;

			const int padding = 4;
			int cardTitleGap = (int)bounds.height - 140;
			const int cardTitleHeight = 24;
			const int buttonHeight = 30;


			var titleRect = new Rect(bounds.x + padding, bounds.y + padding + cardTitleGap, bounds.width - padding * 2, cardTitleHeight);
			var subTextRect = new Rect(titleRect.x, titleRect.yMax, titleRect.width, 20);
			var descRect = new Rect(titleRect.x, subTextRect.yMax, titleRect.width, bounds.yMax - subTextRect.yMax - buttonHeight);
			var contentRect = new Rect(titleRect.x, titleRect.y, titleRect.width, bounds.yMax - titleRect.yMin);


			// draw background
			EditorGUI.DrawRect(bounds, new Color(.26f, .26f, .26f, 1));

			// draw texture
			var texture = LoadTexture(lightBeam.texturePath);

			var gradient = LoadTexture("Packages/com.beamable/Editor/UI/Common/Icons/gradient-deep-space.jpg");
			// var gradient = LoadTexture("Packages/com.beamable/Editor/UI/Toolbox/Icons/beamable_gradient.png");
			var texRect = new Rect(bounds.x, bounds.y, bounds.width, contentRect.yMin - bounds.yMin);


			GUI.DrawTexture(texRect, gradient, ScaleMode.ScaleAndCrop);

			var uv = new Rect(texRect.xMin / position.width, 1 - (texRect.yMin - sampleScrollPosition.y) / position.height,
							  texRect.width / position.width, texRect.height / position.height);
			GUI.DrawTextureWithTexCoords(texRect, gradient, uv, true);

			GUI.DrawTexture(texRect, texture, ScaleMode.ScaleToFit);

			// draw title

			var fontSize = Mathf.Lerp(15, 18, (bounds.width - minWidth) / (maxWidth - minWidth));
			EditorGUI.LabelField(titleRect, lightBeam.name, new GUIStyle(EditorStyles.largeLabel)
			{
				fontStyle = FontStyle.Bold,
				fontSize = (int)fontSize,
				alignment = TextAnchor.UpperLeft,
				padding = new RectOffset(8, 4, 4, 0)
			});


			// draw subtext
			EditorGUI.LabelField(subTextRect, lightBeam.subText, new GUIStyle(EditorStyles.miniLabel)
			{
				alignment = TextAnchor.UpperLeft,
				padding = new RectOffset(8, 4, 0, 0),
				fontStyle = FontStyle.Italic
			});



			// draw desc
			EditorGUI.LabelField(descRect, lightBeam.description, new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.UpperLeft,
				wordWrap = true,
				richText = true,
				padding = new RectOffset(8, 4, 0, 0)

			});


			{ // draw more button 
				var moreBtnSize = 16;
				var moreBtnRect = new Rect(bounds.xMax - moreBtnSize - padding, bounds.y + padding, moreBtnSize,
										   moreBtnSize);
				EditorGUI.DrawRect(moreBtnRect, new Color(0, 0, 0, .4f));
				clickedMore = GUI.Button(moreBtnRect, BeamGUI.iconMoreOptions,
										 new GUIStyle(EditorStyles.iconButton) { margin = new RectOffset(4, 4, 4, 4), });
			}


			{ // draw button 
				var text = "Open Sample";
				if (!lightBeam.isLocal)
				{
					text = "Add To Project";
				}

				var content = new GUIContent(text);
				var size = BeamGUI.primaryButtonStyle.CalcSize(content);
				size.x += 5;
				var buttonRect = new Rect(bounds.xMax - size.x, bounds.yMax - buttonHeight, size.x - padding,
										  buttonHeight - padding);
				clickedOpen = BeamGUI.PrimaryButton(buttonRect, content);
			}


			if (lightBeam.HasDocsUrl)
			{ // draw docs link

				var docContent = new GUIContent(bounds.width > 260 ? "Documentation" : "Docs");

				var size = EditorStyles.linkLabel.CalcSize(docContent);
				var docsRect = new Rect(bounds.x + padding, bounds.yMax - EditorGUIUtility.singleLineHeight - 5, size.x + 4,
										EditorGUIUtility.singleLineHeight);

				clickedDocs = EditorGUI.LinkButton(docsRect, docContent);
			}

			if (clickedOpen)
			{
				AddDelayedAction(() =>
				{
					library.OpenSample(lightBeam);
				});
			}

			if (clickedMore)
			{
				AddDelayedAction(() =>
				{
					OpenSampleMenu(lightBeam);
				});
			}

			if (clickedDocs)
			{
				AddDelayedAction(() =>
				{
					library.OpenDocumentation(lightBeam);
				});
			}
		}

		void OpenSampleMenu(LightbeamSampleInfo lightBeam)
		{
			var menu = new GenericMenu();

			menu.AddItem(new GUIContent("Open Sample"), false, () =>
			{
				library.OpenSample(lightBeam);
			});

			if (lightBeam.HasDocsUrl)
			{
				menu.AddItem(new GUIContent("Goto Documentation"), false, () =>
				{
					library.OpenDocumentation(lightBeam);
				});
			}
			else
			{
				menu.AddDisabledItem(new GUIContent("Goto Documentation"));
			}


			menu.AddSeparator("");

			if (lightBeam.isLocal)
			{
				menu.AddItem(new GUIContent("Show In Project"), false, () =>
				{
					library.ShowInProject(lightBeam);
				});
				menu.AddItem(new GUIContent("Remove Sample"), false, () =>
				{
					library.RemoveSampleFromProject(lightBeam);
				});
			}
			else
			{
				menu.AddDisabledItem(new GUIContent("Show In Project"));
				menu.AddDisabledItem(new GUIContent("Remove Sample"));
			}

			menu.ShowAsContext();
		}
	}
}
