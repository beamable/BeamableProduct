using System;

namespace UnityEngine {
    public static class Mathf {
        private static volatile float FloatMinNormal = 1.175494E-38f;
        private static volatile float FloatMinDenormal = float.Epsilon;
        private static bool IsFlushToZeroEnabled = (double) FloatMinDenormal == 0.0;
        /// <summary>
        ///   <para>A tiny floating point value (Read Only).</para>
        /// </summary>
        public static readonly float Epsilon = IsFlushToZeroEnabled ? FloatMinNormal : FloatMinDenormal;
        /// <summary>
        ///   <para>The well-known 3.14159265358979... value (Read Only).</para>
        /// </summary>
        public const float PI = 3.141593f;
        /// <summary>
        ///   <para>A representation of positive infinity (Read Only).</para>
        /// </summary>
        public const float Infinity = float.PositiveInfinity;
        /// <summary>
        ///   <para>A representation of negative infinity (Read Only).</para>
        /// </summary>
        public const float NegativeInfinity = float.NegativeInfinity;
        /// <summary>
        ///   <para>Degrees-to-radians conversion constant (Read Only).</para>
        /// </summary>
        public const float Deg2Rad = 0.01745329f;
        /// <summary>
        ///   <para>Radians-to-degrees conversion constant (Read Only).</para>
        /// </summary>
        public const float Rad2Deg = 57.29578f;
        
        public static int FloorToInt(float v) {
            return (int) Math.Floor(v);
        }

        public static int CeilToInt(float v) {
            return (int) Math.Ceiling(v);
        }

        public static int RoundToInt(float v) {
            return (int) Math.Round(v);
        }

        public static int Min(int a, int b) {
            return Math.Min(a, b);
        }

        public static int Max(int a, int b) {
            return Math.Max(a, b);
        }

        public static float Min(float a, float b) {
            return Math.Min(a, b);
        }

        public static float Max(float a, float b) {
            return Math.Max(a, b);
        }

        public static float Sqrt(float a) {
            return (float)Math.Sqrt(a);
        }

        public static float Clamp01(float a) {
            return Clamp(a, 0f, 1f);
        }

        public static float Sign(float a) {
            if (a > 0f) return 1f;
            if (a < 0f) return -1f;
            return 0f;
        }

        public static float Clamp(float a, float min, float max) {
            if (a < min) {
                return min;
            }

            if (a > max) {
                return max;
            }

            return a;
        }

        public static float Lerp(float a, float b, float t) {
            return a + (b - a) * Clamp01(t);
        }

        public static float InverseLerp(float a, float b, float t) {
            return a == b ? 0f : Clamp01((t - a) / (b - a));
        }

        public static float Acos(float a) => (float)Math.Acos(a);

        public static float Abs(float a) => Math.Abs(a);

        public static float Sin(float a) => (float)Math.Sin(a);

        public static float Atan2(float a, float b) => (float)Math.Atan2(a, b);
    }
}