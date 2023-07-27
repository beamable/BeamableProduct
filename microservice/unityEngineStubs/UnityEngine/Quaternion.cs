using System;
using System.ComponentModel;
using System.Numerics;
using Newtonsoft.Json;

namespace UnityEngine
{

  /// <summary>
  ///   <para>Quaternions are used to represent rotations.</para>
  /// </summary>
  [JsonObject(MemberSerialization.Fields)]
  public struct Quaternion : IEquatable<Quaternion>
  {
	  
	  /// <summary>
	  ///   <para>X component of the Quaternion. Don't modify this directly unless you know quaternions inside out.</para>
	  /// </summary>
	  public float x;
	  /// <summary>
	  ///   <para>Y component of the Quaternion. Don't modify this directly unless you know quaternions inside out.</para>
	  /// </summary>
	  public float y;
	  /// <summary>
	  ///   <para>Z component of the Quaternion. Don't modify this directly unless you know quaternions inside out.</para>
	  /// </summary>
	  public float z;
	  /// <summary>
	  ///   <para>W component of the Quaternion. Do not directly modify quaternions.</para>
	  /// </summary>
	  public float w;
	  public const float kEpsilon = 1E-06f;

	  /// <summary>
	  ///   <para>Constructs new Quaternion with given x,y,z,w components.</para>
	  /// </summary>
	  /// <param name="x"></param>
	  /// <param name="y"></param>
	  /// <param name="z"></param>
	  /// <param name="w"></param>
	  public Quaternion(float x, float y, float z, float w)
	  {
		  this.x = x;
		  this.y = y;
		  this.z = z;
		  this.w = w;
	  }
	  
#if NETSTANDARD2_0
	  public bool Equals(Quaternion other)
	  {
		  throw new NotImplementedException("This method is not supported on net standard 2.0");
	  }
#else


    /// <summary>
    ///   <para>Creates a rotation which rotates from fromDirection to toDirection.</para>
    /// </summary>
    /// <param name="fromDirection"></param>
    /// <param name="toDirection"></param>
    public static Quaternion FromToRotation(Vector3 fromDirection, Vector3 toDirection) {
      var dot = Vector3.Dot(fromDirection, toDirection);
      if (Mathf.Abs(dot) > .99999f) return identity;
      var cross = Vector3.Cross(fromDirection, toDirection);
      var w = Math.Sqrt((fromDirection.sqrMagnitude) * (toDirection.sqrMagnitude)) + dot;
      return new Quaternion(cross.x, cross.y, cross.z, (float)w);
    }

    /// <summary>
    ///   <para>Returns the Inverse of rotation.</para>
    /// </summary>
    /// <param name="rotation"></param>
    public static Quaternion Inverse(Quaternion rotation) {
      var m = 1f / (rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w);
      return new Quaternion(-rotation.x * m, -rotation.y * m, -rotation.z * m, rotation.w * m);
    }

    /// <summary>
    ///   <para>Spherically interpolates between a and b by t. The parameter t is clamped to the range [0, 1].</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    public static Quaternion Slerp(Quaternion a, Quaternion b, float t) {
      return FromSystemQuaternion(System.Numerics.Quaternion.Slerp(a.asSystemQuaternion(), b.asSystemQuaternion(), Mathf.Clamp01(t)));
    }

    private System.Numerics.Quaternion asSystemQuaternion() {
      return new System.Numerics.Quaternion(x, y, z ,w);
    }

    private static Quaternion FromSystemQuaternion(System.Numerics.Quaternion quaternion) {
      return new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
    }

    /// <summary>
    ///   <para>Spherically interpolates between a and b by t. The parameter t is not clamped.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    public static Quaternion SlerpUnclamped(Quaternion a, Quaternion b, float t)
    {
      return FromSystemQuaternion(System.Numerics.Quaternion.Slerp(a.asSystemQuaternion(), b.asSystemQuaternion(), t));
    }

    /// <summary>
    ///   <para>Interpolates between a and b by t and normalizes the result afterwards. The parameter t is clamped to the range [0, 1].</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    public static Quaternion Lerp(Quaternion a, Quaternion b, float t)
    {
      return FromSystemQuaternion(System.Numerics.Quaternion.Lerp(a.asSystemQuaternion(), b.asSystemQuaternion(), Mathf.Clamp01(t)));
    }

    /// <summary>
    ///   <para>Interpolates between a and b by t and normalizes the result afterwards. The parameter t is not clamped.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t"></param>
    public static Quaternion LerpUnclamped(Quaternion a, Quaternion b, float t)
    {
      return FromSystemQuaternion(System.Numerics.Quaternion.Lerp(a.asSystemQuaternion(), b.asSystemQuaternion(), t));
    }

    /// <summary>
    ///   <para>Creates a rotation which rotates angle degrees around axis.</para>
    /// </summary>
    /// <param name="angle"></param>
    /// <param name="axis"></param>
    public static Quaternion AngleAxis(float angle, Vector3 axis) {
      return FromSystemQuaternion(
        System.Numerics.Quaternion.CreateFromAxisAngle(new System.Numerics.Vector3(axis.x, axis.y, axis.z), angle));
    }

    /// <summary>
    ///   <para>Creates a rotation with the specified forward and upwards directions.</para>
    /// </summary>
    /// <param name="forward">The direction to look in.</param>
    /// <param name="upwards">The vector that defines in which direction up is.</param>
    public static Quaternion LookRotation(Vector3 forward, [DefaultValue("Vector3.up")] Vector3 upwards) {
      var q = System.Numerics.Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateLookAt(
        System.Numerics.Vector3.Zero, 
        new System.Numerics.Vector3(forward.x, forward.y, forward.z), 
        new System.Numerics.Vector3(upwards.x, upwards.y, upwards.z)));
      return FromSystemQuaternion(q);
    }

    /// <summary>
    ///   <para>Creates a rotation with the specified forward and upwards directions.</para>
    /// </summary>
    /// <param name="forward">The direction to look in.</param>
    /// <param name="upwards">The vector that defines in which direction up is.</param>
    public static Quaternion LookRotation(Vector3 forward)
    {
      return LookRotation(forward, Vector3.up);
    }

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
            throw new IndexOutOfRangeException("Invalid Quaternion index!");
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
            throw new IndexOutOfRangeException("Invalid Quaternion index!");
        }
      }
    }

    /// <summary>
    ///   <para>Set x, y, z and w components of an existing Quaternion.</para>
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
    ///   <para>The identity rotation (Read Only).</para>
    /// </summary>
    public static Quaternion identity { get; } = new Quaternion(0.0f, 0.0f, 0.0f, 1f);

    public static Quaternion operator *(Quaternion lhs, Quaternion rhs)
    {
      return new Quaternion((float) (lhs.w * (double) rhs.x + lhs.x * (double) rhs.w + lhs.y * (double) rhs.z - lhs.z * (double) rhs.y), (float) (lhs.w * (double) rhs.y + lhs.y * (double) rhs.w + lhs.z * (double) rhs.x - lhs.x * (double) rhs.z), (float) (lhs.w * (double) rhs.z + lhs.z * (double) rhs.w + lhs.x * (double) rhs.y - lhs.y * (double) rhs.x), (float) (lhs.w * (double) rhs.w - lhs.x * (double) rhs.x - lhs.y * (double) rhs.y - lhs.z * (double) rhs.z));
    }

    public static Vector3 operator *(Quaternion rotation, Vector3 point)
    {
      float num1 = rotation.x * 2f;
      float num2 = rotation.y * 2f;
      float num3 = rotation.z * 2f;
      float num4 = rotation.x * num1;
      float num5 = rotation.y * num2;
      float num6 = rotation.z * num3;
      float num7 = rotation.x * num2;
      float num8 = rotation.x * num3;
      float num9 = rotation.y * num3;
      float num10 = rotation.w * num1;
      float num11 = rotation.w * num2;
      float num12 = rotation.w * num3;
      Vector3 vector3 = default;
      vector3.x = (float) ((1.0 - (num5 + (double) num6)) * point.x + (num7 - (double) num12) * point.y + (num8 + (double) num11) * point.z);
      vector3.y = (float) ((num7 + (double) num12) * point.x + (1.0 - (num4 + (double) num6)) * point.y + (num9 - (double) num10) * point.z);
      vector3.z = (float) ((num8 - (double) num11) * point.x + (num9 + (double) num10) * point.y + (1.0 - (num4 + (double) num5)) * point.z);
      return vector3;
    }

    private static bool IsEqualUsingDot(float dot)
    {
      return dot > 0.999998986721039;
    }

    public static bool operator ==(Quaternion lhs, Quaternion rhs)
    {
      return IsEqualUsingDot(Dot(lhs, rhs));
    }

    public static bool operator !=(Quaternion lhs, Quaternion rhs)
    {
      return !(lhs == rhs);
    }

    /// <summary>
    ///   <para>The dot product between two rotations.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static float Dot(Quaternion a, Quaternion b)
    {
      return (float) (a.x * (double) b.x + a.y * (double) b.y + a.z * (double) b.z + a.w * (double) b.w);
    }

    /// <summary>
    ///   <para>Creates a rotation with the specified forward and upwards directions.</para>
    /// </summary>
    /// <param name="view">The direction to look in.</param>
    /// <param name="up">The vector that defines in which direction up is.</param>
    public void SetLookRotation(Vector3 view)
    {
      Vector3 up = Vector3.up;
      SetLookRotation(view, up);
    }

    /// <summary>
    ///   <para>Creates a rotation with the specified forward and upwards directions.</para>
    /// </summary>
    /// <param name="view">The direction to look in.</param>
    /// <param name="up">The vector that defines in which direction up is.</param>
    public void SetLookRotation(Vector3 view, [DefaultValue("Vector3.up")] Vector3 up)
    {
      this = LookRotation(view, up);
    }

    /// <summary>
    ///   <para>Returns the angle in degrees between two rotations a and b.</para>
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    public static float Angle(Quaternion a, Quaternion b)
    {
      float num = Dot(a, b);
      return !IsEqualUsingDot(num) ? (float) ((double) Mathf.Acos(Mathf.Min(Mathf.Abs(num), 1f)) * 2.0 * 57.2957801818848) : 0.0f;
    }

    private static Vector3 Internal_MakePositive(Vector3 euler)
    {
      float num1 = -9f / (500f * Mathf.PI);
      float num2 = 360f + num1;
      if (euler.x < (double) num1)
        euler.x += 360f;
      else if (euler.x > (double) num2)
        euler.x -= 360f;
      if (euler.y < (double) num1)
        euler.y += 360f;
      else if (euler.y > (double) num2)
        euler.y -= 360f;
      if (euler.z < (double) num1)
        euler.z += 360f;
      else if (euler.z > (double) num2)
        euler.z -= 360f;
      return euler;
    }

    /// <summary>
    ///   <para>Returns or sets the euler angle representation of the rotation.</para>
    /// </summary>
    public Vector3 eulerAngles
    {
      get {
        var matrix = Matrix4x4.CreateFromQuaternion(this.asSystemQuaternion());
        float sy = Mathf.Sqrt(matrix.M11 * matrix.M11 +  matrix.M21 * matrix.M21 );

        bool singular = sy < 1e-6;

        float x, y, z;
        if (!singular)
        {
          x = Mathf.Atan2(matrix.M32 , matrix.M33);
          y =  Mathf.Atan2(-matrix.M31, sy);
          z =  Mathf.Atan2(matrix.M21, matrix.M11);
        }
        else
        {
          x =  Mathf.Atan2(-matrix.M23, matrix.M22);
          y =  Mathf.Atan2(-matrix.M31, sy);
          z = 0;
        }
        return new Vector3(x, y, z);
      }
      set
      {
        this = Euler(value * ((float) Math.PI / 180f));
      }
    }

    /// <summary>
    ///   <para>Returns a rotation that rotates z degrees around the z axis, x degrees around the x axis, and y degrees around the y axis.</para>
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public static Quaternion Euler(float x, float y, float z) {
      var q = System.Numerics.Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateFromYawPitchRoll(y, x, z));
      return FromSystemQuaternion(q);
    }

    /// <summary>
    ///   <para>Returns a rotation that rotates z degrees around the z axis, x degrees around the x axis, and y degrees around the y axis.</para>
    /// </summary>
    /// <param name="euler"></param>
    public static Quaternion Euler(Vector3 euler) {
      return Euler(euler.x, euler.y, euler.z);
    }

    public void ToAngleAxis(out float angle, out Vector3 axis)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    ///   <para>Creates a rotation which rotates from fromDirection to toDirection.</para>
    /// </summary>
    /// <param name="fromDirection"></param>
    /// <param name="toDirection"></param>
    public void SetFromToRotation(Vector3 fromDirection, Vector3 toDirection)
    {
      this = FromToRotation(fromDirection, toDirection);
    }

    /// <summary>
    ///   <para>Rotates a rotation from towards to.</para>
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="maxDegreesDelta"></param>
    public static Quaternion RotateTowards(
      Quaternion from,
      Quaternion to,
      float maxDegreesDelta)
    {
      float angle = Angle(from, to);
      return (double) angle == 0.0 ? to : SlerpUnclamped(from, to, Mathf.Min(1f, maxDegreesDelta / angle));
    }

    /// <summary>
    ///   <para>Converts this quaternion to one with the same orientation but with a magnitude of 1.</para>
    /// </summary>
    /// <param name="q"></param>
    public static Quaternion Normalize(Quaternion q)
    {
      float num = Mathf.Sqrt(Dot(q, q));
      return (double) num < (double) Mathf.Epsilon ? identity : new Quaternion(q.x / num, q.y / num, q.z / num, q.w / num);
    }

    public void Normalize()
    {
      this = Normalize(this);
    }

    /// <summary>
    ///   <para>Returns this quaternion with a magnitude of 1 (Read Only).</para>
    /// </summary>
    public Quaternion normalized
    {
      get
      {
        return Normalize(this);
      }
    }

    public override int GetHashCode()
    {
      return x.GetHashCode() ^ y.GetHashCode() << 2 ^ z.GetHashCode() >> 2 ^ w.GetHashCode() >> 1;
    }

    public override bool Equals(object other)
    {
      return other is Quaternion other1 && Equals(other1);
    }

    public bool Equals(Quaternion other)
    {
      return x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z) && w.Equals(other.w);
    }

    /// <summary>
    ///   <para>Returns a nicely formatted string of the Quaternion.</para>
    /// </summary>
    /// <param name="format"></param>
    public override string ToString()
    {
      return $"({(object) x:F1}, {(object) y:F1}, {(object) z:F1}, {(object) w:F1})";
    }

    /// <summary>
    ///   <para>Returns a nicely formatted string of the Quaternion.</para>
    /// </summary>
    /// <param name="format"></param>
    public string ToString(string format)
    {
      return
        $"({(object) x.ToString(format)}, {(object) y.ToString(format)}, {(object) z.ToString(format)}, {(object) w.ToString(format)})";
    }

    [Obsolete("Use Quaternion.Euler instead. This function was deprecated because it uses radians instead of degrees.")]
    public static Quaternion EulerRotation(float x, float y, float z) {
      return Euler(x, y, z);
    }

    [Obsolete("Use Quaternion.Euler instead. This function was deprecated because it uses radians instead of degrees.")]
    public static Quaternion EulerRotation(Vector3 euler) {
      return Euler(euler);
    }

    [Obsolete("Use Quaternion.Euler instead. This function was deprecated because it uses radians instead of degrees.")]
    public void SetEulerRotation(float x, float y, float z) {
      this = Euler(x, y, z);
    }

    [Obsolete("Use Quaternion.Euler instead. This function was deprecated because it uses radians instead of degrees.")]
    public void SetEulerRotation(Vector3 euler) {
      this = Euler(euler);
    }

    [Obsolete("Use Quaternion.eulerAngles instead. This function was deprecated because it uses radians instead of degrees.")]
    public Vector3 ToEuler() {
      return eulerAngles;
    }

    [Obsolete("Use Quaternion.Euler instead. This function was deprecated because it uses radians instead of degrees.")]
    public static Quaternion EulerAngles(float x, float y, float z) {
      return Euler(x, y, z);
    }

    [Obsolete("Use Quaternion.Euler instead. This function was deprecated because it uses radians instead of degrees.")]
    public static Quaternion EulerAngles(Vector3 euler) {
      return Euler(euler);
    }

    [Obsolete("Use Quaternion.ToAngleAxis instead. This function was deprecated because it uses radians instead of degrees.")]
    public void ToAxisAngle(out Vector3 axis, out float angle)
    {
      throw new NotImplementedException();
    }

    [Obsolete("Use Quaternion.Euler instead. This function was deprecated because it uses radians instead of degrees.")]
    public void SetEulerAngles(float x, float y, float z)
    {
      SetEulerRotation(new Vector3(x, y, z));
    }

    [Obsolete("Use Quaternion.Euler instead. This function was deprecated because it uses radians instead of degrees.")]
    public void SetEulerAngles(Vector3 euler)
    {
      this = EulerRotation(euler);
    }

    [Obsolete("Use Quaternion.eulerAngles instead. This function was deprecated because it uses radians instead of degrees.")]
    public static Vector3 ToEulerAngles(Quaternion rotation) {
      return rotation.eulerAngles;
    }

    [Obsolete("Use Quaternion.eulerAngles instead. This function was deprecated because it uses radians instead of degrees.")]
    public Vector3 ToEulerAngles() {
      return eulerAngles;
    }

    [Obsolete("Use Quaternion.AngleAxis instead. This function was deprecated because it uses radians instead of degrees.")]
    public void SetAxisAngle(Vector3 axis, float angle)
    {
      this = AxisAngle(axis, angle);
    }

    [Obsolete("Use Quaternion.AngleAxis instead. This function was deprecated because it uses radians instead of degrees")]
    public static Quaternion AxisAngle(Vector3 axis, float angle)
    {
      return AngleAxis(57.29578f * angle, axis);
    }
#endif
  }
}
