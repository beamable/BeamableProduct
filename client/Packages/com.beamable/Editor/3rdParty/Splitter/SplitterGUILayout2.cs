using System;
using UnityEngine;
using UnityEditor;

namespace Beamable.Editor.ThirdParty.Splitter
{
	/// <summary>
	/// Modified from https://github.com/miguel12345/EditorGUISplitView/blob/master/Assets/EditorGUISplitView/Scripts/Editor/EditorGUISplitView.cs
	/// </summary>
	[Serializable]
	public class EditorGUISplitView
	{

		public enum Direction
		{
			Horizontal,
			Vertical
		}

		public Direction splitDirection;

		public int cellCount;
		public float[] cellNormalizedSizes;
		
		public int resizingIndex = -1;
		public Vector2[] scrollPositions;
		public Rect availableRect;

		[NonSerialized]
		private int cellIndex = 0;

		public EditorGUISplitView(Direction splitDirection, params float[] startSizes) : this(splitDirection, startSizes.Length)
		{
			for (var i = 0; i < startSizes.Length; i++)
			{
				cellNormalizedSizes[i] = startSizes[i];
			}
		}

		public EditorGUISplitView(Direction splitDirection, int expectedCount)
		{
			cellCount = expectedCount;
			float splitPerCell = 1f / cellCount;
			cellNormalizedSizes = new float[cellCount];
			for (var i = 0; i < cellNormalizedSizes.Length; i++)
			{
				cellNormalizedSizes[i] = splitPerCell;
			}

			scrollPositions = new Vector2[cellCount];
			
			this.splitDirection = splitDirection;
		}

		public void BeginSplitView()
		{
			Rect tempRect;

			cellIndex = 0;
			if (splitDirection == Direction.Horizontal)
				tempRect = EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
			else
				tempRect = EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

			if (tempRect.width > 0.0f)
			{
				availableRect = tempRect;
			}

			var sum = 0f;
			for (var i = 0; i < cellNormalizedSizes.Length; i++)
			{
				cellNormalizedSizes[i] = Mathf.Clamp01(cellNormalizedSizes[i]);
				sum += cellNormalizedSizes[i];
			}

			// if the sum isn't close enough to 1, then we need to re-balance the weights
			if (Mathf.Abs(1 - sum) > .01f)
			{
				Debug.LogWarning("splitter view is unbalanced and is resetting. ");
				// this is a crappy way to do it; but cannot think of another way. 
				for (var i = 0; i < cellNormalizedSizes.Length; i++)
				{
					cellNormalizedSizes[i] = 1f / cellNormalizedSizes.Length;
				}
			}

			StartSplit(cellIndex);
		}

		void StartSplit(int index)
		{
			
			if (splitDirection == Direction.Horizontal)
				scrollPositions[index] = GUILayout.BeginScrollView(scrollPositions[index],
				                                           GUILayout.Width(
					                                           availableRect.width * cellNormalizedSizes[index]));
			else
				scrollPositions[index] = GUILayout.BeginScrollView(scrollPositions[index],
				                                           GUILayout.Height(availableRect.height * cellNormalizedSizes[index]));


		}

		public void Split(EditorWindow window)
		{
			GUILayout.EndScrollView();
			ResizeSplitFirstView(window, cellIndex);

			cellIndex++;
			if (cellIndex < cellCount - 1)
			{
				StartSplit(cellIndex);
			}
		}

		public void EndSplitView()
		{
			if (cellIndex != cellCount - 1)
			{
				throw new InvalidOperationException("cell count must match split count");
			}
			if (splitDirection == Direction.Horizontal)
				EditorGUILayout.EndHorizontal();
			else
				EditorGUILayout.EndVertical();
		}

		private float GetSumForIndex(int index)
		{
			var value = 0f;
			for (var i = 0; i <= index; i ++)
			{
				value += cellNormalizedSizes[i];
			}

			return value;
		}
		
		private void ResizeSplitFirstView(EditorWindow window, int index)
		{

			Rect resizeHandleRect;

			var width = 4f;
			var halfWidth = width * .5f;
			var value = GetSumForIndex(index);
			if (splitDirection == Direction.Horizontal)
				resizeHandleRect = new Rect(availableRect.width * value - halfWidth, availableRect.y, width,
				                            availableRect.height);
			else
				resizeHandleRect = new Rect(availableRect.x, availableRect.height * value - halfWidth,
				                            availableRect.width, width);

			// GUI.DrawTexture(resizeHandleRect, EditorGUIUtility.whiteTexture);
			EditorGUI.DrawRect(resizeHandleRect, new Color(0,0,0,.3f));
			if (splitDirection == Direction.Horizontal)
				EditorGUIUtility.AddCursorRect(resizeHandleRect, MouseCursor.ResizeHorizontal);
			else
				EditorGUIUtility.AddCursorRect(resizeHandleRect, MouseCursor.ResizeVertical);

			if (Event.current.type == EventType.MouseDown && resizeHandleRect.Contains(Event.current.mousePosition))
			{
				resizingIndex = index;
			}

			if (resizingIndex == index)
			{
				if (splitDirection == Direction.Horizontal)
				{
					
					var ratio = Event.current.mousePosition.x / availableRect.width;
					var diff = ratio - value;

					cellNormalizedSizes[index] += diff;
					cellNormalizedSizes[index + 1] -= diff;
				}
				else
				{
					var ratio = Event.current.mousePosition.y / availableRect.height;
					var diff = ratio - value;

					cellNormalizedSizes[index] += diff;
					cellNormalizedSizes[index + 1] -= diff;
				}
				
				window.Repaint();
			}

			if (Event.current.type == EventType.MouseUp)
				resizingIndex = -1;
		}
	}
}
