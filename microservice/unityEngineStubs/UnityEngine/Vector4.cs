using System;
using System.Globalization;
using Newtonsoft.Json;
// ReSharper disable InconsistentNaming

namespace UnityEngine
{
  /// <summary>
  ///   <para>Representation of four-dimensional vectors.</para>
  /// </summary>
  [JsonObject(MemberSerialization.Fields)]
  public struct Vector4 : IEquatable<Vector4>
  {
    public const float kEpsilon = 1E-05f;

    /// <summary>
    ///   <para>X component of the vector.</para>
    /// </summary>
    public float x;

    /// <summary>
    ///   <para>Y component of the vector.</para>
    /// </summary>
    public float y;

    /// <summary>
    ///   <para>Z component of the vector.</para>
    /// </summary>
    public float z;

    /// <summary>
    ///   <para>W component of the vector.</para>
    /// </summary>
    public float w;

    public float this[int index]
    {
      get
      {
        switch (index)
        {
          case 0:
            return x;
          case 1:
            return y;
          case 2:
            return z;
          case 3:
            return w;
          default:
            throw new IndexOutOfRangeException("Invalid Vector4 index!");
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
          case 2:
            z = value;
            break;
          case 3:
            w = value;
            break;
          default:
            throw new IndexOutOfRangeException("Invalid Vector4 index!");
        }
      }
    }

    /// <summary>
    ///   <para>Creates a new vector with given x, y, z, w components.</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="w"></param>
    public Vector4(float x, float y, float z, float w)
    {
      this.x = x;
      this.y = y;
      this.z = z;
      this.w = w;
    }

    /// <summary>
    ///   <para>Creates a new vector with given x, y, z components and sets w to zero.</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public Vector4(float x, float y, float z)
    {
      this.x = x;
      this.y = y;
      this.z = z;
      w = 0.0f;
    }

    /// <summary>
    ///   <para>Creates a new vector with given x, y components and sets z and w to zero.</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public Vector4(float x, float y)
    {
      this.x = x;
      this.y = y;
      z = 0.0f;
      w = 0.0f;
    }

    /// <summary>
    ///   <para>Set x, y, z and w components of an existing Vector4.</para>
    /// </summary>
    /// <param name="newX"></param>
    /// <param name="newY"></param>
    /// <param name="newZ"></param>
    /// <param name="newW"></param>
    public void Set(float newX, float newY, float newZ, float newW)
    {
      x = newX;
      y = newY;
      z = newZ;
      w = newW;
    }

    /// <summary>
    ///   <para>Linearly interpolates between two vectors.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    public static Vector4 Lerp(Vector4 a, Vector4 b, float t)
    {
      t = Mathf.Clamp01(t);
      return new Vector4(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t);
    }

    /// <summary>
    ///   <para>Linearly interpolates between two vectors.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    public static Vector4 LerpUnclamped(Vector4 a, Vector4 b, float t)
    {
      return new Vector4(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t, a.w + (b.w - a.w) * t);
    }

    /// <summary>
    ///   <para>Moves a point current towards target.</para>
    /// </summary>
    /// <param name="current"></param>
    /// <param name="target"></param>
    /// <param name="maxDistanceDelta"></param>
    public static Vector4 MoveTowards(
      Vector4 current,
      Vector4 target,
      float maxDistanceDelta)
    {
      float deltaX = target.x - current.x;
      float deltaY = target.y - current.y;
      float deltaZ = target.z - current.z;
      float deltaW = target.w - current.w;
      float sqrMagnitude = (float) (deltaX * (double) deltaX + deltaY * (double) deltaY + deltaZ * (double) deltaZ + deltaW * (double) deltaW);
      if (sqrMagnitude == 0.0 || maxDistanceDelta >= 0.0 && sqrMagnitude <= maxDistanceDelta * (double) maxDistanceDelta)
        return target;
      float magnitude = (float) Math.Sqrt(sqrMagnitude);
      return new Vector4(current.x + deltaX / magnitude * maxDistanceDelta, current.y + deltaY / magnitude * maxDistanceDelta, current.z + deltaZ / magnitude * maxDistanceDelta, current.w + deltaW / magnitude * maxDistanceDelta);
    }

    /// <summary>
    ///   <para>Multiplies two vectors component-wise.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static Vector4 Scale(Vector4 a, Vector4 b)
    {
      return new Vector4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);
    }

    /// <summary>
    ///   <para>Multiplies every component of this vector by the same component of scale.</para>
    /// </summary>
    /// <param name="scale"></param>
    public void Scale(Vector4 scale)
    {
      x *= scale.x;
      y *= scale.y;
      z *= scale.z;
      w *= scale.w;
    }

    public override int GetHashCode()
    {
      return x.GetHashCode() ^ y.GetHashCode() << 2 ^ z.GetHashCode() >> 2 ^ w.GetHashCode() >> 1;
    }

    /// <summary>
    ///   <para>Returns true if the given vector is exactly equal to this vector.</para>
    /// </summary>
    /// <param name="other"></param>
    public override bool Equals(object other)
    {
      return other is Vector4 other1 && Equals(other1);
    }

    public bool Equals(Vector4 other)
    {
      return x == (double) other.x && y == (double) other.y && z == (double) other.z && w == (double) other.w;
    }

    /// <summary>
    ///   <para></para>
    /// </summary>
    /// <param name="a"></param>
    public static Vector4 Normalize(Vector4 a)
    {
      float magnitude = Magnitude(a);
      return (double) magnitude > 9.99999974737875E-06 ? a / magnitude : zero;
    }

    /// <summary>
    ///   <para>Makes this vector have a magnitude of 1.</para>
    /// </summary>
    public void Normalize()
    {
      float num = Magnitude(this);
      if (num > 9.99999974737875E-06)
        this = this / num;
      else
        this = zero;
    }

    /// <summary>
    ///   <para>Returns this vector with a magnitude of 1 (Read Only).</para>
    /// </summary>
    public Vector4 normalized
    {
      get
      {
        return Normalize(this);
      }
    }

    /// <summary>
    ///   <para>Dot Product of two vectors.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static float Dot(Vector4 a, Vector4 b)
    {
      return (float) (a.x * (double) b.x + a.y * (double) b.y + a.z * (double) b.z + a.w * (double) b.w);
    }

    /// <summary>
    ///   <para>Projects a vector onto another vector.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static Vector4 Project(Vector4 a, Vector4 b)
    {
      return b * (Dot(a, b) / Dot(b, b));
    }

    /// <summary>
    ///   <para>Returns the distance between a and b.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static float Distance(Vector4 a, Vector4 b)
    {
      return Magnitude(a - b);
    }

    public static float Magnitude(Vector4 a)
    {
      return (float) Math.Sqrt(Dot(a, a));
    }

    /// <summary>
    ///   <para>Returns the length of this vector (Read Only).</para>
    /// </summary>
    public float magnitude
    {
      get
      {
        return (float) Math.Sqrt(Dot(this, this));
      }
    }

    /// <summary>
    ///   <para>Returns the squared length of this vector (Read Only).</para>
    /// </summary>
    public float sqrMagnitude
    {
      get
      {
        return Dot(this, this);
      }
    }

    /// <summary>
    ///   <para>Returns a vector that is made from the smallest components of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    public static Vector4 Min(Vector4 lhs, Vector4 rhs)
    {
      return new Vector4(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y), Mathf.Min(lhs.z, rhs.z), Mathf.Min(lhs.w, rhs.w));
    }

    /// <summary>
    ///   <para>Returns a vector that is made from the largest components of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    public static Vector4 Max(Vector4 lhs, Vector4 rhs)
    {
      return new Vector4(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y), Mathf.Max(lhs.z, rhs.z), Mathf.Max(lhs.w, rhs.w));
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector4(0,0,0,0).</para>
    /// </summary>
    public static Vector4 zero { get; } = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

    /// <summary>
    ///   <para>Shorthand for writing Vector4(1,1,1,1).</para>
    /// </summary>
    public static Vector4 one { get; } = new Vector4(1f, 1f, 1f, 1f);

    /// <summary>
    ///   <para>Shorthand for writing Vector4(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity).</para>
    /// </summary>
    public static Vector4 positiveInfinity { get; } = new Vector4(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

    /// <summary>
    ///   <para>Shorthand for writing Vector4(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity).</para>
    /// </summary>
    public static Vector4 negativeInfinity { get; } = new Vector4(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

    public static Vector4 operator +(Vector4 a, Vector4 b)
    {
      return new Vector4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
    }

    public static Vector4 operator -(Vector4 a, Vector4 b)
    {
      return new Vector4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
    }

    public static Vector4 operator -(Vector4 a)
    {
      return new Vector4(-a.x, -a.y, -a.z, -a.w);
    }

    public static Vector4 operator *(Vector4 a, float d)
    {
      return new Vector4(a.x * d, a.y * d, a.z * d, a.w * d);
    }

    public static Vector4 operator *(float d, Vector4 a)
    {
      return new Vector4(a.x * d, a.y * d, a.z * d, a.w * d);
    }

    public static Vector4 operator /(Vector4 a, float d)
    {
      return new Vector4(a.x / d, a.y / d, a.z / d, a.w / d);
    }

    public static bool operator ==(Vector4 lhs, Vector4 rhs)
    {
      float num1 = lhs.x - rhs.x;
      float num2 = lhs.y - rhs.y;
      float num3 = lhs.z - rhs.z;
      float num4 = lhs.w - rhs.w;
      return num1 * (double) num1 + num2 * (double) num2 + num3 * (double) num3 + num4 * (double) num4 < 9.99999943962493E-11;
    }

    public static bool operator !=(Vector4 lhs, Vector4 rhs)
    {
      return !(lhs == rhs);
    }

    public static implicit operator Vector4(Vector3 v)
    {
      return new Vector4(v.x, v.y, v.z, 0.0f);
    }

    public static implicit operator Vector3(Vector4 v)
    {
      return new Vector3(v.x, v.y, v.z);
    }

    public static implicit operator Vector4(Vector2 v)
    {
      return new Vector4(v.x, v.y, 0.0f, 0.0f);
    }

    public static implicit operator Vector2(Vector4 v)
    {
      return new Vector2(v.x, v.y);
    }

    /// <summary>
    ///   <para>Returns a nicely formatted string for this vector.</para>
    /// </summary>
    public override string ToString()
    {
      return $"({(object) x:F1}, {(object) y:F1}, {(object) z:F1}, {(object) w:F1})";
    }

    /// <summary>
    ///   <para>Returns a nicely formatted string for this vector.</para>
    /// </summary>
    /// <param name="format"></param>
    public string ToString(string format)
    {
      return
        $"({(object) x.ToString(format, CultureInfo.InvariantCulture.NumberFormat)}, {(object) y.ToString(format, CultureInfo.InvariantCulture.NumberFormat)}, {(object) z.ToString(format, CultureInfo.InvariantCulture.NumberFormat)}, {(object) w.ToString(format, CultureInfo.InvariantCulture.NumberFormat)})";
    }

    public static float SqrMagnitude(Vector4 a)
    {
      return Dot(a, a);
    }

    public float SqrMagnitude()
    {
      return Dot(this, this);
    }
  }
}
