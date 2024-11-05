using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Beamable.Editor.Eggs.Serpent
{
	[Serializable]
	public class EggSerpent
	{
		public List<Vector2Int> serpent = new List<Vector2Int>();
		public int serpentLength = 3;
		public List<Vector2Int> food = new List<Vector2Int>();
		public Queue<Vector2Int> inputs = new Queue<Vector2Int>();
		public EggEntry entry = new EggEntry();
		public bool wasEntered = false;
		public Vector2Int direction = Vector2Int.right;
		public bool isDead;

		double nextMoveAt = 0.0;

		public void OnGui(EditorWindow window, BeamableDispatcher dispatcher)
		{
			entry.OnGui();
			if (!entry.entered) return;

			var width = window.position.width;
			var height = window.position.height;
			const int cellSize = 10;
			int rows = (int)(height / cellSize);
			var cols = (int)(width / cellSize);
			var time = EditorApplication.timeSinceStartup;

			var bgColor = new Color(0, 0, 0, .2f);
			var foodColor = new Color(.2f, .4f, 1f);
			var serpentColor = new Color(.3f, .8f, .4f);

			int taskId = 0;
			IEnumerator Run(int id)
			{
				while (window != null && taskId == id)
				{
					yield return null;
					window.Repaint();
				}
			}

			{ // init
				if (!wasEntered)
				{
					taskId++;
					dispatcher.Run("serpent", Run(taskId));

					Debug.Log("Howdy, friend.");
					isDead = false;
					wasEntered = true;
					direction = Vector2Int.right;
					serpent.Clear();
					food.Clear();
					serpent.Add(new Vector2Int(cols / 2, rows / 2));
					serpent.Add(new Vector2Int(cols / 2 + 1, rows / 2));
					serpent.Add(new Vector2Int(cols / 2 + 2, rows / 2));
					serpentLength = serpent.Count;
					inputs.Clear();
				}
			}

			{ // draw grid
				for (var x = 0; x < cols; x++)
				{
					for (var y = 0; y < rows; y++)
					{
						var cell = GetCellRect(x, y);
						cell = new Rect(cell.x + 1, cell.y + 1, cell.width - 2, cell.height - 2);
						EditorGUI.DrawRect(cell, bgColor);
					}
				}
			}

			{ // spawn food if needed
				if (food.Count < serpentLength)
				{
					var f = new Vector2Int(Random.Range(1, cols - 1), Random.Range(1, rows - 1));
					food.Add(f);
				}
			}

			var head = serpent[0];
			var eatIndex = -1;

			{ // draw food
				for (var i = 0; i < food.Count; i++)
				{
					var cell = GetCellRectV(food[i]);
					EditorGUI.DrawRect(cell, foodColor);

					if (food[i] == head)
					{
						eatIndex = i;
						serpentLength++;
					}
				}

				if (eatIndex >= 0)
				{
					food.RemoveAt(eatIndex);
				}
			}

			{ // draw snake

				var cell = GetCellRectV(serpent[0]);
				EditorGUI.DrawRect(cell, serpentColor);

				for (var i = 1; i < serpent.Count; i++)
				{
					if (head == serpent[i])
					{
						isDead = true;
					}
					cell = GetCellRectV(serpent[i]);
					EditorGUI.DrawRect(cell, serpentColor);
				}
			}

			{ // check death
				if (head.x == 0 || head.y == 0 || head.x >= cols || head.y >= rows)
				{
					isDead = true;
				}
			}

			if (!isDead && time > nextMoveAt)
			{ // move snake

				if (inputs.TryDequeue(out var nextDir))
				{
					if (nextDir + direction != Vector2Int.zero) // this prevents the serpent from turning backwards into itself
					{
						direction = nextDir;
					}
				}

				var next = serpent[0] + direction;
				serpent.Insert(0, next);
				while (serpent.Count >= serpentLength)
				{
					serpent.RemoveAt(serpent.Count - 1);
				}
				nextMoveAt = time + Mathf.Lerp(.04f, .08f, Mathf.InverseLerp(20, 3, serpentLength));
			}

			if (!isDead && Event.current != null)
			{ // handle input
				if (Event.current.type == EventType.KeyUp)
				{
					var key = Event.current.keyCode;
					switch (key)
					{
						case KeyCode.UpArrow:
						case KeyCode.W:
							inputs.Enqueue(Vector2Int.down);
							break;
						case KeyCode.DownArrow:
						case KeyCode.S:
							inputs.Enqueue(Vector2Int.up);
							break;
						case KeyCode.LeftArrow:
						case KeyCode.A:
							inputs.Enqueue(Vector2Int.left);
							break;
						case KeyCode.RightArrow:
						case KeyCode.D:
							inputs.Enqueue(Vector2Int.right);
							break;
					}
				}
			}

			if (isDead)
			{ // render game over
				var center = new Rect(width / 2 - 100, height / 2 - 100, 200, 40);
				EditorGUI.LabelField(center, $"Score {serpentLength}", new GUIStyle
				{
					fontSize = 30,
					fontStyle = FontStyle.Bold,
					alignment = TextAnchor.MiddleCenter,
					normal = new GUIStyleState
					{
						textColor = new Color(1, .4f, .25f, 1)
					}
				});
				if (GUI.Button(new Rect(center.x, center.y + 40, center.width, 40), "Goodbye"))
				{
					Debug.Log("Close the window to stop playing...");
					wasEntered = false;
				}
			}
			Rect GetCellRectV(Vector2Int v) => GetCellRect(v.x, v.y);
			Rect GetCellRect(int x, int y)
			{
				return new Rect((x / (float)cols) * width, (y / (float)rows) * height, cellSize, cellSize);
			}

		}
	}
}
