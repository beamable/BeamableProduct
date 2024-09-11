using System;
using System.Globalization;
using Newtonsoft.Json;
// ReSharper disable InconsistentNaming

namespace UnityEngine {
    /// <summary>
    ///   <para>Representation of 3D vectors and points.</para>
    /// </summary>
    [JsonObject(MemberSerialization.Fields)]
    public struct Vector3 : IEquatable<Vector3> {
        public const float kEpsilon = 1E-05f;
        public const float kEpsilonNormalSqrt = 1E-15f;

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
        ///   <para>Spherically interpolates between two vectors.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        public static Vector3 Slerp(Vector3 a, Vector3 b, float t) {
            t = Mathf.Clamp01(t);
            var angle = Vector3.Angle(a, b);
            
            var sinR = 1f / Mathf.Sin(angle);
            return Mathf.Sin((1f - t) * angle) * sinR * a + Mathf.Sin(t * angle) * sinR * b;
        }

        /// <summary>
        ///   <para>Spherically interpolates between two vectors.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        public static Vector3 SlerpUnclamped(Vector3 a, Vector3 b, float t) {
            var angle = Vector3.Angle(a, b);
            var sinR = 1f / Mathf.Sin(angle);
            return Mathf.Sin((1f - t) * angle) * sinR * a + Mathf.Sin(t * angle) * sinR * b;
        }

        public static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent) {
            throw new NotImplementedException();
        }

        public static void OrthoNormalize(
            ref Vector3 normal,
            ref Vector3 tangent,
            ref Vector3 binormal) {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   <para>Rotates a vector current towards target.</para>
        /// </summary>
        /// <param name="current">The vector being managed.</param>
        /// <param name="target">The vector.</param>
        /// <param name="maxRadiansDelta">The maximum angle in radians allowed for this rotation.</param>
        /// <param name="maxMagnitudeDelta">The maximum allowed change in vector magnitude for this rotation.</param>
        /// <returns>
        ///   <para>The location that RotateTowards generates.</para>
        /// </returns>
        public static Vector3 RotateTowards(
            Vector3 current,
            Vector3 target,
            float maxRadiansDelta,
            float maxMagnitudeDelta) {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   <para>Linearly interpolates between two points.</para>
        /// </summary>
        /// <param name="a">Start value, returned when t = 0.</param>
        /// <param name="b">End value, returned when t = 1.</param>
        /// <param name="t">Value used to interpolate between a and b.</param>
        /// <returns>
        ///   <para>Interpolated value, equals to a + (b - a) * t.</para>
        /// </returns>
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) {
            t = Mathf.Clamp01(t);
            return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }

        /// <summary>
        ///   <para>Linearly interpolates between two vectors.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) {
            return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }

        /// <summary>
        ///   <para>Calculate a position between the points specified by current and target, moving no farther than the distance specified by maxDistanceDelta.</para>
        /// </summary>
        /// <param name="current">The position to move from.</param>
        /// <param name="target">The position to move towards.</param>
        /// <param name="maxDistanceDelta">Distance to move current per call.</param>
        /// <returns>
        ///   <para>The new position.</para>
        /// </returns>
        public static Vector3 MoveTowards(
            Vector3 current,
            Vector3 target,
            float maxDistanceDelta) {
            float deltaX = target.x - current.x;
            float deltaY = target.y - current.y;
            float deltaZ = target.z - current.z;
            float sqrMagnitude =
                (float) (deltaX * (double) deltaX + deltaY * (double) deltaY + deltaZ * (double) deltaZ);
            if (sqrMagnitude == 0.0 ||
                maxDistanceDelta >= 0.0 && sqrMagnitude <= maxDistanceDelta * (double) maxDistanceDelta)
                return target;
            float magnitude = (float) Math.Sqrt(sqrMagnitude);
            return new Vector3(current.x + deltaX / magnitude * maxDistanceDelta,
                current.y + deltaY / magnitude * maxDistanceDelta, current.z + deltaZ / magnitude * maxDistanceDelta);
        }

        public static Vector3 SmoothDamp(
            Vector3 current,
            Vector3 target,
            ref Vector3 currentVelocity,
            float smoothTime,
            float maxSpeed) {
            throw new NotImplementedException();
        }

        public static Vector3 SmoothDamp(
            Vector3 current,
            Vector3 target,
            ref Vector3 currentVelocity,
            float smoothTime) {
            throw new NotImplementedException();
        }

        public static Vector3 SmoothDamp(
            Vector3 current,
            Vector3 target,
            ref Vector3 currentVelocity,
            float smoothTime,
            float maxSpeed = 0f,
            float deltaTime = 0f) {
            throw new NotImplementedException();
        }

        public float this[int index] {
            get {
                switch (index) {
                    case 0:
                        return x;
                    case 1:
                        return y;
                    case 2:
                        return z;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector3 index!");
                }
            }
            set {
                switch (index) {
                    case 0:
                        x = value;
                        break;
                    case 1:
                        y = value;
                        break;
                    case 2:
                        z = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector3 index!");
                }
            }
        }

        /// <summary>
        ///   <para>Creates a new vector with given x, y, z components.</para>
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Vector3(float x, float y, float z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        /// <summary>
        ///   <para>Creates a new vector with given x, y components and sets z to zero.</para>
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public Vector3(float x, float y) {
            this.x = x;
            this.y = y;
            z = 0.0f;
        }

        /// <summary>
        ///   <para>Set x, y and z components of an existing Vector3.</para>
        /// </summary>
        /// <param name="newX"></param>
        /// <param name="newY"></param>
        /// <param name="newZ"></param>
        public void Set(float newX, float newY, float newZ) {
            x = newX;
            y = newY;
            z = newZ;
        }

        /// <summary>
        ///   <para>Multiplies two vectors component-wise.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static Vector3 Scale(Vector3 a, Vector3 b) {
            return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        /// <summary>
        ///   <para>Multiplies every component of this vector by the same component of scale.</para>
        /// </summary>
        /// <param name="scale"></param>
        public void Scale(Vector3 scale) {
            x *= scale.x;
            y *= scale.y;
            z *= scale.z;
        }

        /// <summary>
        ///   <para>Cross Product of two vectors.</para>
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        public static Vector3 Cross(Vector3 lhs, Vector3 rhs) {
            return new Vector3((float) (lhs.y * (double) rhs.z - lhs.z * (double) rhs.y),
                (float) (lhs.z * (double) rhs.x - lhs.x * (double) rhs.z),
                (float) (lhs.x * (double) rhs.y - lhs.y * (double) rhs.x));
        }

        public override int GetHashCode() {
            return x.GetHashCode() ^ y.GetHashCode() << 2 ^ z.GetHashCode() >> 2;
        }

        /// <summary>
        ///   <para>Returns true if the given vector is exactly equal to this vector.</para>
        /// </summary>
        /// <param name="other"></param>
        public override bool Equals(object other) {
            return other is Vector3 other1 && Equals(other1);
        }

        public bool Equals(Vector3 other) {
            return x == (double) other.x && y == (double) other.y && z == (double) other.z;
        }

        /// <summary>
        ///   <para>Reflects a vector off the plane defined by a normal.</para>
        /// </summary>
        /// <param name="inDirection"></param>
        /// <param name="inNormal"></param>
        public static Vector3 Reflect(Vector3 inDirection, Vector3 inNormal) {
            float num = -2f * Dot(inNormal, inDirection);
            return new Vector3(num * inNormal.x + inDirection.x, num * inNormal.y + inDirection.y,
                num * inNormal.z + inDirection.z);
        }

        /// <summary>
        ///   <para>Makes this vector have a magnitude of 1.</para>
        /// </summary>
        /// <param name="value"></param>
        public static Vector3 Normalize(Vector3 value) {
            float num = Magnitude(value);
            return (double) num > 9.99999974737875E-06 ? value / num : zero;
        }

        public void Normalize() {
            float num = Magnitude(this);
            if (num > 9.99999974737875E-06)
                this = this / num;
            else
                this = zero;
        }

        /// <summary>
        ///   <para>Returns this vector with a magnitude of 1 (Read Only).</para>
        /// </summary>
        public Vector3 normalized {
            get { return Normalize(this); }
        }

        /// <summary>
        ///   <para>Dot Product of two vectors.</para>
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        public static float Dot(Vector3 lhs, Vector3 rhs) {
            return (float) (lhs.x * (double) rhs.x + lhs.y * (double) rhs.y + lhs.z * (double) rhs.z);
        }

        /// <summary>
        ///   <para>Projects a vector onto another vector.</para>
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="onNormal"></param>
        public static Vector3 Project(Vector3 vector, Vector3 onNormal) {
            float normalSqrMagnitude = Dot(onNormal, onNormal);
            if (normalSqrMagnitude < (double) Mathf.Epsilon)
                return zero;
            float dot = Dot(vector, onNormal);
            return new Vector3(onNormal.x * dot / normalSqrMagnitude, onNormal.y * dot / normalSqrMagnitude,
                onNormal.z * dot / normalSqrMagnitude);
        }

        /// <summary>
        ///   <para>Projects a vector onto a plane defined by a normal orthogonal to the plane.</para>
        /// </summary>
        /// <param name="planeNormal">The direction from the vector towards the plane.</param>
        /// <param name="vector">The location of the vector above the plane.</param>
        /// <returns>
        ///   <para>The location of the vector on the plane.</para>
        /// </returns>
        public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal) {
            float normalSqrMagnitude = Dot(planeNormal, planeNormal);
            if (normalSqrMagnitude < (double) Mathf.Epsilon)
                return vector;
            float dot = Dot(vector, planeNormal);
            return new Vector3(vector.x - planeNormal.x * dot / normalSqrMagnitude,
                vector.y - planeNormal.y * dot / normalSqrMagnitude,
                vector.z - planeNormal.z * dot / normalSqrMagnitude);
        }

        /// <summary>
        ///   <para>Returns the angle in degrees between from and to.</para>
        /// </summary>
        /// <param name="from">The vector from which the angular difference is measured.</param>
        /// <param name="to">The vector to which the angular difference is measured.</param>
        /// <returns>
        ///   <para>The angle in degrees between the two vectors.</para>
        /// </returns>
        public static float Angle(Vector3 from, Vector3 to) {
            float num = (float) Math.Sqrt(@from.sqrMagnitude * (double) to.sqrMagnitude);
            return (double) num < 1.00000000362749E-15
                ? 0.0f
                : (float) Math.Acos(Mathf.Clamp(Dot(@from, to) / num, -1f, 1f)) *
                  57.29578f; // 57.29578 degree = 1 radian
        }

        /// <summary>
        ///   <para>Returns the signed angle in degrees between from and to.</para>
        /// </summary>
        /// <param name="from">The vector from which the angular difference is measured.</param>
        /// <param name="to">The vector to which the angular difference is measured.</param>
        /// <param name="axis">A vector around which the other vectors are rotated.</param>
        public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis) {
            float angle = Angle(from, to);
            float yz = (float) (@from.y * (double) to.z - @from.z * (double) to.y);
            float zx = (float) (@from.z * (double) to.x - @from.x * (double) to.z);
            float xy = (float) (@from.x * (double) to.y - @from.y * (double) to.x);
            float sign = Mathf.Sign((float) (axis.x * (double) yz + axis.y * (double) zx + axis.z * (double) xy));
            return angle * sign;
        }

        /// <summary>
        ///   <para>Returns the distance between a and b.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public static float Distance(Vector3 a, Vector3 b) {
            float num1 = a.x - b.x;
            float num2 = a.y - b.y;
            float num3 = a.z - b.z;
            return (float) Math.Sqrt(num1 * (double) num1 + num2 * (double) num2 + num3 * (double) num3);
        }

        /// <summary>
        ///   <para>Returns a copy of vector with its magnitude clamped to maxLength.</para>
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="maxLength"></param>
        public static Vector3 ClampMagnitude(Vector3 vector, float maxLength) {
            float sqrMagnitude = vector.sqrMagnitude;
            if (sqrMagnitude <= maxLength * (double) maxLength)
                return vector;
            float magnitude = (float) Math.Sqrt(sqrMagnitude);
            float xNormalized = vector.x / magnitude;
            float num3 = vector.y / magnitude;
            float num4 = vector.z / magnitude;
            return new Vector3(xNormalized * maxLength, num3 * maxLength, num4 * maxLength);
        }

        public static float Magnitude(Vector3 vector) {
            return (float) Math.Sqrt(vector.x * (double) vector.x + vector.y * (double) vector.y +
                                     vector.z * (double) vector.z);
        }

        /// <summary>
        ///   <para>Returns the length of this vector (Read Only).</para>
        /// </summary>
        public float magnitude {
            get { return (float) Math.Sqrt(x * (double) x + y * (double) y + z * (double) z); }
        }

        public static float SqrMagnitude(Vector3 vector) {
            return (float) (vector.x * (double) vector.x + vector.y * (double) vector.y + vector.z * (double) vector.z);
        }

        /// <summary>
        ///   <para>Returns the squared length of this vector (Read Only).</para>
        /// </summary>
        public float sqrMagnitude {
            get { return (float) (x * (double) x + y * (double) y + z * (double) z); }
        }

        /// <summary>
        ///   <para>Returns a vector that is made from the smallest components of two vectors.</para>
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        public static Vector3 Min(Vector3 lhs, Vector3 rhs) {
            return new Vector3(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y), Mathf.Min(lhs.z, rhs.z));
        }

        /// <summary>
        ///   <para>Returns a vector that is made from the largest components of two vectors.</para>
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        public static Vector3 Max(Vector3 lhs, Vector3 rhs) {
            return new Vector3(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y), Mathf.Max(lhs.z, rhs.z));
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector3(0, 0, 0).</para>
        /// </summary>
        public static Vector3 zero { get; } = new Vector3(0.0f, 0.0f, 0.0f);

        /// <summary>
        ///   <para>Shorthand for writing Vector3(1, 1, 1).</para>
        /// </summary>
        public static Vector3 one { get; } = new Vector3(1f, 1f, 1f);

        /// <summary>
        ///   <para>Shorthand for writing Vector3(0, 0, 1).</para>
        /// </summary>
        public static Vector3 forward { get; } = new Vector3(0.0f, 0.0f, 1f);

        /// <summary>
        ///   <para>Shorthand for writing Vector3(0, 0, -1).</para>
        /// </summary>
        public static Vector3 back { get; } = new Vector3(0.0f, 0.0f, -1f);

        /// <summary>
        ///   <para>Shorthand for writing Vector3(0, 1, 0).</para>
        /// </summary>
        public static Vector3 up { get; } = new Vector3(0.0f, 1f, 0.0f);

        /// <summary>
        ///   <para>Shorthand for writing Vector3(0, -1, 0).</para>
        /// </summary>
        public static Vector3 down { get; } = new Vector3(0.0f, -1f, 0.0f);

        /// <summary>
        ///   <para>Shorthand for writing Vector3(-1, 0, 0).</para>
        /// </summary>
        public static Vector3 left { get; } = new Vector3(-1f, 0.0f, 0.0f);

        /// <summary>
        ///   <para>Shorthand for writing Vector3(1, 0, 0).</para>
        /// </summary>
        public static Vector3 right { get; } = new Vector3(1f, 0.0f, 0.0f);

        /// <summary>
        ///   <para>Shorthand for writing Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity).</para>
        /// </summary>
        public static Vector3 positiveInfinity { get; } =
            new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

        /// <summary>
        ///   <para>Shorthand for writing Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity).</para>
        /// </summary>
        public static Vector3 negativeInfinity { get; } =
            new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        public static Vector3 operator +(Vector3 a, Vector3 b) {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3 operator -(Vector3 a, Vector3 b) {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector3 operator -(Vector3 a) {
            return new Vector3(-a.x, -a.y, -a.z);
        }

        public static Vector3 operator *(Vector3 a, float d) {
            return new Vector3(a.x * d, a.y * d, a.z * d);
        }

        public static Vector3 operator *(float d, Vector3 a) {
            return new Vector3(a.x * d, a.y * d, a.z * d);
        }

        public static Vector3 operator /(Vector3 a, float d) {
            return new Vector3(a.x / d, a.y / d, a.z / d);
        }

        public static bool operator ==(Vector3 lhs, Vector3 rhs) {
            float num1 = lhs.x - rhs.x;
            float num2 = lhs.y - rhs.y;
            float num3 = lhs.z - rhs.z;
            return num1 * (double) num1 + num2 * (double) num2 + num3 * (double) num3 < 9.99999943962493E-11;
        }

        public static bool operator !=(Vector3 lhs, Vector3 rhs) {
            return !(lhs == rhs);
        }

        /// <summary>
        ///   <para>Returns a nicely formatted string for this vector.</para>
        /// </summary>
        public override string ToString() {
            return $"({(object) x:F1}, {(object) y:F1}, {(object) z:F1})";
        }

        /// <summary>
        ///   <para>Returns a nicely formatted string for this vector.</para>
        /// </summary>
        /// <param name="format"></param>
        public string ToString(string format) {
            return
                $"({(object) x.ToString(format, CultureInfo.InvariantCulture.NumberFormat)}, {(object) y.ToString(format, CultureInfo.InvariantCulture.NumberFormat)}, {(object) z.ToString(format, CultureInfo.InvariantCulture.NumberFormat)})";
        }

        [Obsolete("Use Vector3.forward instead.")]
        public static Vector3 fwd {
            get { return new Vector3(0.0f, 0.0f, 1f); }
        }

        [Obsolete(
            "Use Vector3.Angle instead. AngleBetween uses radians instead of degrees and was deprecated for this reason")]
        public static float AngleBetween(Vector3 from, Vector3 to) {
            return (float) Math.Acos(Mathf.Clamp(Dot(@from.normalized, to.normalized), -1f, 1f));
        }

        [Obsolete("Use Vector3.ProjectOnPlane instead.")]
        public static Vector3 Exclude(Vector3 excludeThis, Vector3 fromThat) {
            return ProjectOnPlane(fromThat, excludeThis);
        }
    }
}
