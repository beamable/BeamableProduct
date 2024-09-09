using System;

namespace UnityEngine
{
	/// <summary>
	///   <para>Gradient used for animating colors.</para>
	/// </summary>
	public class Gradient
	{
		public GradientColorKey[] colorKeys { get; set; }

		public GradientAlphaKey[] alphaKeys { get; set; }

		public Color Evaluate(float time)
		{
			throw new NotImplementedException();
		}
	}

	public struct GradientColorKey
	{
		/// <summary>
		///   <para>Color of key.</para>
		/// </summary>
		public Color color;

		/// <summary>
		///   <para>Time of the key (0 - 1).</para>
		/// </summary>
		public float time;

		/// <summary>
		///   <para>Gradient color key.</para>
		/// </summary>
		/// <param name="col">Color of key.</param>
		/// <param name="time">Time of the key (0 - 1).</param>
		public GradientColorKey(Color col, float time)
		{
			this.color = col;
			this.time = time;
		}
	}

	public struct GradientAlphaKey
	{
		/// <summary>
		///   <para>Alpha channel of key.</para>
		/// </summary>
		public float alpha;
		/// <summary>
		///   <para>Time of the key (0 - 1).</para>
		/// </summary>
		public float time;

		/// <summary>
		///   <para>Gradient alpha key.</para>
		/// </summary>
		/// <param name="alpha">Alpha of key (0 - 1).</param>
		/// <param name="time">Time of the key (0 - 1).</param>
		public GradientAlphaKey(float alpha, float time)
		{
			this.alpha = alpha;
			this.time = time;
		}
	}
}
