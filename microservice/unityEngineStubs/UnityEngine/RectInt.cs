using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
	/// <summary>
	///   <para>A 2D Rectangle defined by x, y, width, height with integers.</para>
	/// </summary>
	[JsonObject(MemberSerialization.Fields)]
	public struct RectInt : IEquatable<RectInt>
	{
		/// <summary>
		///   <para>Left coordinate of the rectangle.</para>
		/// </summary>
		public int x;

		/// <summary>
		///   <para>Top coordinate of the rectangle.</para>
		/// </summary>
		public int y;

		/// <summary>
		///   <para>Center coordinate of the rectangle.</para>
		/// </summary>
		public Vector2 center
		{
			get
			{
				return new Vector2(x + width / 2f, y + height / 2f);
			}
		}

		/// <summary>
		///   <para>Lower left corner of the rectangle.</para>
		/// </summary>
		public Vector2Int min
		{
			get
			{
				return new Vector2Int(xMin, yMin);
			}
			set
			{
				xMin = value.x;
				yMin = value.y;
			}
		}

		/// <summary>
		///   <para>Upper right corner of the rectangle.</para>
		/// </summary>
		public Vector2Int max
		{
			get
			{
				return new Vector2Int(xMax, yMax);
			}
			set
			{
				xMax = value.x;
				yMax = value.y;
			}
		}

		/// <summary>
		///   <para>Width of the rectangle.</para>
		/// </summary>
		public int width;

		/// <summary>
		///   <para>Height of the rectangle.</para>
		/// </summary>
		public int height;

		/// <summary>
		///   <para>Returns the minimum X value of the RectInt.</para>
		/// </summary>
		public int xMin
		{
			get
			{
				return Math.Min(x, x + width);
			}
			set
			{
				int xMax = this.xMax;
				x = value;
				width = xMax - x;
			}
		}

		/// <summary>
		///   <para>Returns the minimum Y value of the RectInt.</para>
		/// </summary>
		public int yMin
		{
			get
			{
				return Math.Min(y, y + height);
			}
			set
			{
				int yMax = this.yMax;
				y = value;
				height = yMax - y;
			}
		}

		/// <summary>
		///   <para>Returns the maximum X value of the RectInt.</para>
		/// </summary>
		public int xMax
		{
			get
			{
				return Math.Max(x, x + width);
			}
			set
			{
				width = value - x;
			}
		}

		/// <summary>
		///   <para>Returns the maximum Y value of the RectInt.</para>
		/// </summary>
		public int yMax
		{
			get
			{
				return Math.Max(y, y + height);
			}
			set
			{
				height = value - y;
			}
		}

		/// <summary>
		///   <para>Returns the position (x, y) of the RectInt.</para>
		/// </summary>
		public Vector2Int position
		{
			get
			{
				return new Vector2Int(x, y);
			}
			set
			{
				x = value.x;
				y = value.y;
			}
		}

		/// <summary>
		///   <para>Returns the width and height of the RectInt.</para>
		/// </summary>
		public Vector2Int size
		{
			get
			{
				return new Vector2Int(width, height);
			}
			set
			{
				width = value.x;
				height = value.y;
			}
		}

		/// <summary>
		///   <para>Sets the bounds to the min and max value of the rect.</para>
		/// </summary>
		/// <param name="minPosition"></param>
		/// <param name="maxPosition"></param>
		public void SetMinMax(Vector2Int minPosition, Vector2Int maxPosition)
		{
			min = minPosition;
			max = maxPosition;
		}

		public RectInt(int xMin, int yMin, int width, int height)
		{
			x = xMin;
			y = yMin;
			this.width = width;
			this.height = height;
		}

		public RectInt(Vector2Int position, Vector2Int size)
		{
			x = position.x;
			y = position.y;
			width = size.x;
			height = size.y;
		}

		/// <summary>
		///   <para>Clamps the position and size of the RectInt to the given bounds.</para>
		/// </summary>
		/// <param name="bounds">Bounds to clamp the RectInt.</param>
		public void ClampToBounds(RectInt bounds)
		{
			position = new Vector2Int(Math.Max(Math.Min(bounds.xMax, position.x), bounds.xMin), Math.Max(Math.Min(bounds.yMax, position.y), bounds.yMin));
			size = new Vector2Int(Math.Min(bounds.xMax - position.x, size.x), Math.Min(bounds.yMax - position.y, size.y));
		}

		/// <summary>
		///   <para>Returns true if the given position is within the RectInt.</para>
		/// </summary>
		/// <param name="position">Position to check.</param>
		/// <returns>
		///   <para>Whether the position is within the RectInt.</para>
		/// </returns>
		public bool Contains(Vector2Int position)
		{
			return position.x >= xMin && position.y >= yMin && position.x < xMax && position.y < yMax;
		}

		/// <summary>
		///   <para>RectInts overlap if each RectInt Contains a shared point.</para>
		/// </summary>
		/// <param name="other">Other rectangle to test overlapping with.</param>
		/// <returns>
		///   <para>True if the other rectangle overlaps this one.</para>
		/// </returns>
		public bool Overlaps(RectInt other)
		{
			return other.xMin < xMax && other.xMax > xMin && other.yMin < yMax && other.yMax > yMin;
		}

		/// <summary>
		///   <para>Returns the x, y, width and height of the RectInt.</para>
		/// </summary>
		public override string ToString()
		{
			return $"(x:{(object)x}, y:{(object)y}, width:{(object)width}, height:{(object)height})";
		}

		/// <summary>
		///   <para>Returns true if the given RectInt is equal to this RectInt.</para>
		/// </summary>
		/// <param name="other"></param>
		public bool Equals(RectInt other)
		{
			return x == other.x && y == other.y && width == other.width && height == other.height;
		}

		/// <summary>
		///   <para>A RectInt.PositionCollection that contains all positions within the RectInt.</para>
		/// </summary>
		public PositionEnumerator allPositionsWithin
		{
			get
			{
				return new PositionEnumerator(min, max);
			}
		}

		/// <summary>
		///   <para>An iterator that allows you to iterate over all positions within the RectInt.</para>
		/// </summary>
		public struct PositionEnumerator : IEnumerator<Vector2Int>, IEnumerator, IDisposable
		{
			private readonly Vector2Int _min;
			private readonly Vector2Int _max;
			private Vector2Int _current;

			public PositionEnumerator(Vector2Int min, Vector2Int max)
			{
				_min = _current = min;
				_max = max;
				Reset();
			}

			/// <summary>
			///   <para>Returns this as an iterator that allows you to iterate over all positions within the RectInt.</para>
			/// </summary>
			/// <returns>
			///   <para>This RectInt.PositionEnumerator.</para>
			/// </returns>
			public PositionEnumerator GetEnumerator()
			{
				return this;
			}

			/// <summary>
			///   <para>Moves the enumerator to the next position.</para>
			/// </summary>
			/// <returns>
			///   <para>Whether the enumerator has successfully moved to the next position.</para>
			/// </returns>
			public bool MoveNext()
			{
				if (_current.y >= _max.y)
					return false;
				++_current.x;
				int x1 = _current.x;
				Vector2Int vector2Int = _max;
				int x2 = vector2Int.x;
				if (x1 >= x2)
				{
					ref Vector2Int local = ref _current;
					vector2Int = _min;
					int x3 = vector2Int.x;
					local.x = x3;
					++_current.y;
					int y1 = _current.y;
					vector2Int = _max;
					int y2 = vector2Int.y;
					if (y1 >= y2)
						return false;
				}
				return true;
			}

			/// <summary>
			///   <para>Resets this enumerator to its starting state.</para>
			/// </summary>
			public void Reset()
			{
				_current = _min;
				--_current.x;
			}

			/// <summary>
			///   <para>Current position of the enumerator.</para>
			/// </summary>
			public Vector2Int Current
			{
				get
				{
					return _current;
				}
			}

			object IEnumerator.Current
			{
				get
				{
					return Current;
				}
			}

			void IDisposable.Dispose()
			{
			}
		}
	}
}
