using System;
using System.Globalization;
using System.Numerics;
using Newtonsoft.Json;
// ReSharper disable InconsistentNaming

namespace UnityEngine
{
  /// <summary>
  ///   <para>Representation of 2D vectors and points.</para>
  /// </summary>
  [JsonObject(MemberSerialization.Fields)]
  public struct Vector2 : IEquatable<Vector2> {
    /// <summary>
    ///   <para>X component of the vector.</para>
    /// </summary>
    public float x;

    /// <summary>
    ///   <para>Y component of the vector.</para>
    /// </summary>
    public float y;
    
    public const float kEpsilon = 1E-05f;
    public const float kEpsilonNormalSqrt = 1E-15f;

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
          default:
            throw new IndexOutOfRangeException("Invalid Vector2 index!");
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
            throw new IndexOutOfRangeException("Invalid Vector2 index!");
        }
      }
    }

    /// <summary>
    ///   <para>Constructs a new vector with given x, y components.</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public Vector2(float x, float y)
    {
      this.x = x;
      this.y = y;
    }

    /// <summary>
    ///   <para>Set x and y components of an existing Vector2.</para>
    /// </summary>
    /// <param name="newX"></param>
    /// <param name="newY"></param>
    public void Set(float newX, float newY)
    {
      x = newX;
      y = newY;
    }

    /// <summary>
    ///   <para>Linearly interpolates between vectors a and b by t.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
    {
      t = Mathf.Clamp01(t);
      return new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
    }

    /// <summary>
    ///   <para>Linearly interpolates between vectors a and b by t.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float t)
    {
      return new Vector2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
    }

    /// <summary>
    ///   <para>Moves a point current towards target.</para>
    /// </summary>
    /// <param name="current"></param>
    /// <param name="target"></param>
    /// <param name="maxDistanceDelta"></param>
    public static Vector2 MoveTowards(
      Vector2 current,
      Vector2 target,
      float maxDistanceDelta)
    {
      float deltaX = target.x - current.x;
      float deltaY = target.y - current.y;
      float sqrMagnitude = (float) (deltaX * (double) deltaX + deltaY * (double) deltaY);
      if (sqrMagnitude == 0.0 || maxDistanceDelta >= 0.0 && sqrMagnitude <= maxDistanceDelta * (double) maxDistanceDelta)
        return target;
      float magnitude = (float) Math.Sqrt(sqrMagnitude);
      return new Vector2(current.x + deltaX / magnitude * maxDistanceDelta, current.y + deltaY / magnitude * maxDistanceDelta);
    }

    /// <summary>
    ///   <para>Multiplies two vectors component-wise.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static Vector2 Scale(Vector2 a, Vector2 b)
    {
      return new Vector2(a.x * b.x, a.y * b.y);
    }

    /// <summary>
    ///   <para>Multiplies every component of this vector by the same component of scale.</para>
    /// </summary>
    /// <param name="scale"></param>
    public void Scale(Vector2 scale)
    {
      x *= scale.x;
      y *= scale.y;
    }

    /// <summary>
    ///   <para>Makes this vector have a magnitude of 1.</para>
    /// </summary>
    public void Normalize()
    {
      float magnitude = this.magnitude;
      if (magnitude > 9.99999974737875E-06)
        this = this / magnitude;
      else
        this = zero;
    }

    /// <summary>
    ///   <para>Returns this vector with a magnitude of 1 (Read Only).</para>
    /// </summary>
    public Vector2 normalized
    {
      get
      {
        Vector2 vector2 = new Vector2(x, y);
        vector2.Normalize();
        return vector2;
      }
    }

    /// <summary>
    ///   <para>Returns a nicely formatted string for this vector.</para>
    /// </summary>
    public override string ToString()
    {
      return $"({x:F1}, {y:F1})";
    }

    /// <summary>
    ///   <para>Returns a nicely formatted string for this vector.</para>
    /// </summary>
    /// <param name="format"></param>
    public string ToString(string format)
    {
      return
        $"({x.ToString(format, CultureInfo.InvariantCulture.NumberFormat)}, {y.ToString(format, CultureInfo.InvariantCulture.NumberFormat)})";
    }

    public override int GetHashCode()
    {
      return x.GetHashCode() ^ y.GetHashCode() << 2;
    }

    /// <summary>
    ///   <para>Returns true if the given vector is exactly equal to this vector.</para>
    /// </summary>
    /// <param name="other"></param>
    public override bool Equals(object other)
    {
      return other is Vector2 other1 && Equals(other1);
    }

    public bool Equals(Vector2 other)
    {
      return x == (double) other.x && y == (double) other.y;
    }

    /// <summary>
    ///   <para>Reflects a vector off the vector defined by a normal.</para>
    /// </summary>
    /// <param name="inDirection"></param>
    /// <param name="inNormal"></param>
    public static Vector2 Reflect(Vector2 inDirection, Vector2 inNormal)
    {
      float num = -2f * Dot(inNormal, inDirection);
      return new Vector2(num * inNormal.x + inDirection.x, num * inNormal.y + inDirection.y);
    }

    /// <summary>
    ///   <para>Returns the 2D vector perpendicular to this 2D vector. The result is always rotated 90-degrees in a counter-clockwise direction for a 2D coordinate system where the positive Y axis goes up.</para>
    /// </summary>
    /// <param name="inDirection">The input direction.</param>
    /// <returns>
    ///   <para>The perpendicular direction.</para>
    /// </returns>
    public static Vector2 Perpendicular(Vector2 inDirection)
    {
      return new Vector2(-inDirection.y, inDirection.x);
    }

    /// <summary>
    ///   <para>Dot Product of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    public static float Dot(Vector2 lhs, Vector2 rhs)
    {
      return (float) (lhs.x * (double) rhs.x + lhs.y * (double) rhs.y);
    }

    /// <summary>
    ///   <para>Returns the length of this vector (Read Only).</para>
    /// </summary>
    public float magnitude
    {
      get
      {
        return (float) Math.Sqrt(x * (double) x + y * (double) y);
      }
    }

    /// <summary>
    ///   <para>Returns the squared length of this vector (Read Only).</para>
    /// </summary>
    public float sqrMagnitude
    {
      get
      {
        return (float) (x * (double) x + y * (double) y);
      }
    }

    /// <summary>
    ///   <para>Returns the unsigned angle in degrees between from and to.</para>
    /// </summary>
    /// <param name="from">The vector from which the angular difference is measured.</param>
    /// <param name="to">The vector to which the angular difference is measured.</param>
    public static float Angle(Vector2 from, Vector2 to)
    {
      float num = (float) Math.Sqrt(@from.sqrMagnitude * (double) to.sqrMagnitude);
      return (double) num < 1.00000000362749E-15 ? 0.0f : (float) Math.Acos(Mathf.Clamp(Dot(@from, to) / num, -1f, 1f)) * 57.29578f; // 57.29578 degree = 1 radian
    }

    /// <summary>
    ///   <para>Returns the signed angle in degrees between from and to.</para>
    /// </summary>
    /// <param name="from">The vector from which the angular difference is measured.</param>
    /// <param name="to">The vector to which the angular difference is measured.</param>
    public static float SignedAngle(Vector2 from, Vector2 to)
    {
      return Angle(from, to) * Mathf.Sign((float) (@from.x * (double) to.y - @from.y * (double) to.x));
    }

    /// <summary>
    ///   <para>Returns the distance between a and b.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static float Distance(Vector2 a, Vector2 b)
    {
      float deltaX = a.x - b.x;
      float deltaY = a.y - b.y;
      return (float) Math.Sqrt(deltaX * (double) deltaX + deltaY * (double) deltaY);
    }

    /// <summary>
    ///   <para>Returns a copy of vector with its magnitude clamped to maxLength.</para>
    /// </summary>
    /// <param name="vector"></param>
    /// <param name="maxLength"></param>
    public static Vector2 ClampMagnitude(Vector2 vector, float maxLength)
    {
      float sqrMagnitude = vector.sqrMagnitude;
      if (sqrMagnitude <= maxLength * (double) maxLength)
        return vector;
      float magnitude = (float) Math.Sqrt(sqrMagnitude);
      float xNormalized = vector.x / magnitude;
      float yNormalized = vector.y / magnitude;
      return new Vector2(xNormalized * maxLength, yNormalized * maxLength);
    }

    public static float SqrMagnitude(Vector2 a)
    {
      return (float) (a.x * (double) a.x + a.y * (double) a.y);
    }

    public float SqrMagnitude()
    {
      return (float) (x * (double) x + y * (double) y);
    }

    /// <summary>
    ///   <para>Returns a vector that is made from the smallest components of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    public static Vector2 Min(Vector2 lhs, Vector2 rhs)
    {
      return new Vector2(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y));
    }

    /// <summary>
    ///   <para>Returns a vector that is made from the largest components of two vectors.</para>
    /// </summary>
    /// <param name="lhs"></param>
    /// <param name="rhs"></param>
    public static Vector2 Max(Vector2 lhs, Vector2 rhs)
    {
      return new Vector2(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y));
    }

    public static Vector2 operator +(Vector2 a, Vector2 b)
    {
      return new Vector2(a.x + b.x, a.y + b.y);
    }

    public static Vector2 operator -(Vector2 a, Vector2 b)
    {
      return new Vector2(a.x - b.x, a.y - b.y);
    }

    public static Vector2 operator *(Vector2 a, Vector2 b)
    {
      return new Vector2(a.x * b.x, a.y * b.y);
    }

    public static Vector2 operator /(Vector2 a, Vector2 b)
    {
      return new Vector2(a.x / b.x, a.y / b.y);
    }

    public static Vector2 operator -(Vector2 a)
    {
      return new Vector2(-a.x, -a.y);
    }

    public static Vector2 operator *(Vector2 a, float d)
    {
      return new Vector2(a.x * d, a.y * d);
    }

    public static Vector2 operator *(float d, Vector2 a)
    {
      return new Vector2(a.x * d, a.y * d);
    }

    public static Vector2 operator /(Vector2 a, float d)
    {
      return new Vector2(a.x / d, a.y / d);
    }

    public static bool operator ==(Vector2 lhs, Vector2 rhs)
    {
      float num1 = lhs.x - rhs.x;
      float num2 = lhs.y - rhs.y;
      return num1 * (double) num1 + num2 * (double) num2 < 9.99999943962493E-11;
    }

    public static bool operator !=(Vector2 lhs, Vector2 rhs)
    {
      return !(lhs == rhs);
    }

    public static implicit operator Vector2(Vector3 v)
    {
      return new Vector2(v.x, v.y);
    }

    public static implicit operator Vector3(Vector2 v)
    {
      return new Vector3(v.x, v.y, 0.0f);
    }

    /// <summary>
    ///   <para>Shorthand for writing Vector2(0, 0).</para>
    /// </summary>
    public static Vector2 zero { get; } = new Vector2(0.0f, 0.0f);

    /// <summary>
    ///   <para>Shorthand for writing Vector2(1, 1).</para>
    /// </summary>
    public static Vector2 one { get; } = new Vector2(1f, 1f);

    /// <summary>
    ///   <para>Shorthand for writing Vector2(0, 1).</para>
    /// </summary>
    public static Vector2 up { get; } = new Vector2(0.0f, 1f);

    /// <summary>
    ///   <para>Shorthand for writing Vector2(0, -1).</para>
    /// </summary>
    public static Vector2 down { get; } = new Vector2(0.0f, -1f);

    /// <summary>
    ///   <para>Shorthand for writing Vector2(-1, 0).</para>
    /// </summary>
    public static Vector2 left { get; } = new Vector2(-1f, 0.0f);

    /// <summary>
    ///   <para>Shorthand for writing Vector2(1, 0).</para>
    /// </summary>
    public static Vector2 right { get; } = new Vector2(1f, 0.0f);

    /// <summary>
    ///   <para>Shorthand for writing Vector2(float.PositiveInfinity, float.PositiveInfinity).</para>
    /// </summary>
    public static Vector2 positiveInfinity { get; } = new Vector2(float.PositiveInfinity, float.PositiveInfinity);

    /// <summary>
    ///   <para>Shorthand for writing Vector2(float.NegativeInfinity, float.NegativeInfinity).</para>
    /// </summary>
    public static Vector2 negativeInfinity { get; } = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
  }
}
