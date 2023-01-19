using System;
using UnityEngine;

namespace Beamable.UI.Buss
{
	[Serializable]
	public class RectTransformProperty : DefaultBussProperty, IRectTransformBussProperty
	{
		[SerializeField] private float top;
		[SerializeField] private float left;
		[SerializeField] private float right;
		[SerializeField] private float bottom;
		[SerializeField] private Vector2 pivot;
		[SerializeField] private Vector2 anchorMin;
		[SerializeField] private Vector2 anchorMax;
		
		public IBussProperty CopyProperty()
		{
			return new RectTransformProperty(this);
		}

		public RectTransformProperty()
		{
			AnchorMax = Vector2.one;
			Pivot = new Vector2(.5f, .5f);
		}

		public RectTransformProperty(RectTransformProperty other)
		{
			Pivot = other.Pivot;
			AnchorMin = other.AnchorMin;
			AnchorMax = other.AnchorMax;
			Left = other.Left;
			Right = other.Right;
			Top = other.Top;
			Bottom = other.Bottom;
		}

		public Vector2 Pivot { get => pivot; set => pivot = value; }
		public float Left { get => left; set => left = value; }
		public float Right { get => right; set => right = value; }
		public float Top { get => top; set => top = value; }
		public float Bottom { get => bottom; set => bottom = value; }
		public Vector2 AnchorMin { get => anchorMin; set => anchorMin = value; }
		public Vector2 AnchorMax { get => anchorMax; set => anchorMax = value; }
	}
}
