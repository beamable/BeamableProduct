using System;
using System.Numerics;
using Newtonsoft.Json;

namespace UnityEngine {
  /// <summary>
  ///   <para>Representation of 2D vectors and points using integers.</para>
  /// </summary>
    [JsonObject(MemberSerialization.Fields)]
    public struct Vector2Int : IEquatable<Vector2Int> {
    /// <summary>
    ///   <para>X component of the vector.</para>
    /// </summary>
    public int x;

    /// <summary>
    ///   <para>Y component of the vector.</para>
    /// </summary>
    public int y;

    public Vector2Int(int x, int y)
    {
      this.x = x;
      this.y = y;
    }

    /// <summary>
    ///   <para>Set x and y components of an existing Vector2Int.</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void Set(int x, int y)
    {
      this.x = x;
      this.y = y;
    }

    public int this[int index]
    {
      get
      {
        switch (index)
        {
          case 0:
            return x;
          case 1:
            return y;
          default:
            throw new IndexOutOfRangeException($"Invalid Vector2Int index addressed: {index}!");
        }
      }
      set
      {
        switch (index)
        {
          case 0:
            x = value;
            break;
          case 1:
            y = value;
            break;
          default:
            throw new IndexOutOfRangeException($"Invalid Vector2Int index addressed: {index}!");
        }
      }
    }

    /// <summary>
    ///   <para>Returns the length of this vector (Read Only).</para>
    /// </summary>
    public float magnitude
    {
      get
      {
        return Mathf.Sqrt(sqrMagnitude);
      }
    }

    /// <summary>
    ///   <para>Returns the squared length of this vector (Read Only).</para>
    /// </summary>
    public int sqrMagnitude
    {
      get
      {
        return x * x + y * y;
      }
    }

    /// <summary>
    ///   <para>Returns the distance between a and b.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static float Distance(Vector2Int a, Vector2Int b)
    {
      float v1 = a.x - b.x;
      float v2 = a.y - b.y;
      return (float) Math.Sqrt(v1 * (double) v1 + v2 * (double) v2);
    }

    /// <summary>
    ///   <para>Returns a vector that is made from the smallest components of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    public static Vector2Int Min(Vector2Int lhs, Vector2Int rhs)
    {
      return new Vector2Int(Math.Min(lhs.x, rhs.x), Math.Min(lhs.y, rhs.y));
    }

    /// <summary>
    ///   <para>Returns a vector that is made from the largest components of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    public static Vector2Int Max(Vector2Int lhs, Vector2Int rhs)
    {
      return new Vector2Int(Math.Max(lhs.x, rhs.x), Math.Max(lhs.y, rhs.y));
    }

    /// <summary>
    ///   <para>Multiplies two vectors component-wise.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static Vector2Int Scale(Vector2Int a, Vector2Int b)
    {
      return new Vector2Int(a.x * b.x, a.y * b.y);
    }

    /// <summary>
    ///   <para>Multiplies every component of this vector by the same component of scale.</para>
    /// </summary>
    /// <param name="scale"></param>
    public void Scale(Vector2Int scale)
    {
      x *= scale.x;
      y *= scale.y;
    }

    /// <summary>
    ///   <para>Clamps the Vector2Int to the bounds given by min and max.</para>
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    public void Clamp(Vector2Int min, Vector2Int max)
    {
      x = Math.Max(min.x, x);
      x = Math.Min(max.x, x);
      y = Math.Max(min.y, y);
      y = Math.Min(max.y, y);
    }

    public static implicit operator Vector2(Vector2Int v)
    {
      return new Vector2(v.x, v.y);
    }

    public static explicit operator Vector3Int(Vector2Int v)
    {
      return new Vector3Int(v.x, v.y, 0);
    }

    /// <summary>
    ///   <para>Converts a Vector2 to a Vector2Int by doing a Floor to each value.</para>
    /// </summary>
    /// <param name="v"></param>
    public static Vector2Int FloorToInt(Vector2 v)
    {
      return new Vector2Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));
    }

    /// <summary>
    ///   <para>Converts a  Vector2 to a Vector2Int by doing a Ceiling to each value.</para>
    /// </summary>
    /// <param name="v"></param>
    public static Vector2Int CeilToInt(Vector2 v)
    {
      return new Vector2Int(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y));
    }

    /// <summary>
    ///   <para>Converts a  Vector2 to a Vector2Int by doing a Round to each value.</para>
    /// </summary>
    /// <param name="v"></param>
    public static Vector2Int RoundToInt(Vector2 v)
    {
      return new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
    }

    public static Vector2Int operator -(Vector2Int v)
    {
      return new Vector2Int(-v.x, -v.y);
    }

    public static Vector2Int operator +(Vector2Int a, Vector2Int b)
    {
      return new Vector2Int(a.x + b.x, a.y + b.y);
    }

    public static Vector2Int operator -(Vector2Int a, Vector2Int b)
    {
      return new Vector2Int(a.x - b.x, a.y - b.y);
    }

    public static Vector2Int operator *(Vector2Int a, Vector2Int b)
    {
      return new Vector2Int(a.x * b.x, a.y * b.y);
    }

    public static Vector2Int operator *(int a, Vector2Int b)
    {
      return new Vector2Int(a * b.x, a * b.y);
    }

    public static Vector2Int operator *(Vector2Int a, int b)
    {
      return new Vector2Int(a.x * b, a.y * b);
    }

    public static Vector2Int operator /(Vector2Int a, int b)
    {
      return new Vector2Int(a.x / b, a.y / b);
    }

    public static bool operator ==(Vector2Int lhs, Vector2Int rhs)
    {
      return lhs.x == rhs.x && lhs.y == rhs.y;
    }

    public static bool operator !=(Vector2Int lhs, Vector2Int rhs)
    {
      return !(lhs == rhs);
    }

    /// <summary>
    ///   <para>Returns true if the objects are equal.</para>
    /// </summary>
    /// <param name="other"></param>
    public override bool Equals(object other)
    {
      return other is Vector2Int other1 && Equals(other1);
    }

    public bool Equals(Vector2Int other)
    {
      return x.Equals(other.x) && y.Equals(other.y);
    }

    /// <summary>
    ///   <para>Gets the hash code for the Vector2Int.</para>
    /// </summary>
    /// <returns>
    ///   <para>The hash code of the Vector2Int.</para>
    /// </returns>
    public override int GetHashCode()
    {
      int num1 = x;
      int hashCode = num1.GetHashCode();
      num1 = y;
      int num2 = num1.GetHashCode() << 2;
      return hashCode ^ num2;
    }

    /// <summary>
    ///   <para>Returns a nicely formatted string for this vector.</para>
    /// </summary>
    public override string ToString()
    {
      return $"({(object) x}, {(object) y})";
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector2Int (0, 0).</para>
    /// </summary>
    public static Vector2Int zero => new Vector2Int(0, 0);

    /// <summary>
    ///   <para>Shorthand for writing Vector2Int (1, 1).</para>
    /// </summary>
    public static Vector2Int one => new Vector2Int(1, 1);

    /// <summary>
    ///   <para>Shorthand for writing Vector2Int (0, 1).</para>
    /// </summary>
    public static Vector2Int up { get; } = new Vector2Int(0, 1);

    /// <summary>
    ///   <para>Shorthand for writing Vector2Int (0, -1).</para>
    /// </summary>
    public static Vector2Int down { get; } = new Vector2Int(0, -1);

    /// <summary>
    ///   <para>Shorthand for writing Vector2Int (-1, 0).</para>
    /// </summary>
    public static Vector2Int left { get; } = new Vector2Int(-1, 0);

    /// <summary>
    ///   <para>Shorthand for writing Vector2Int (1, 0).</para>
    /// </summary>
    public static Vector2Int right { get; } = new Vector2Int(1, 0);
  }
}