using UnityEngine;
using UnityEngine.UI;
#if !UNITY_2019_1_OR_NEWER
using UnityEngine.Experimental.UI;
#endif

namespace Beamable.UI.SDF
{
	public static class SDFUtils
	{
		/// <summary>
		/// Packs SDFImage parameters in vertex data and adds quad to the vertex helper;
		/// </summary>
		public static void AddRect(this VertexHelper vh,
								   Rect position,
								   Rect uvs,
								   Rect coords,
								   Color32 vertexColor,
								   Vector2 size,
								   float threshold,
								   Vector2 uvToCoordsFactor,
								   float rounding,
								   float outlineWidth,
								   Color outlineColor,
								   Color shadowColor,
								   float shadowThreshold,
								   Vector2 shadowOffset)
		{
			var uv2 = new Vector2(outlineWidth,
								  PackVector3ToFloat(outlineColor.r, outlineColor.g, outlineColor.b));
			var normal = new Vector3(
				threshold,
				uvToCoordsFactor.x,
				uvToCoordsFactor.y);
			var tangent = new Vector4(shadowOffset.x, shadowOffset.y,
									  PackVector3ToFloat(shadowThreshold, shadowColor.a, 0),
									  PackVector3ToFloat(shadowColor.r, shadowColor.g, shadowColor.b));

			AddRect(vh, position, rounding, uvs, coords, vertexColor, size, uv2, normal, tangent);
		}

		/// <summary>
		/// Adds quad to the vertex helper.
		/// </summary>
		private static void AddRect(this VertexHelper vh,
									Rect position,
									float z,
									Rect uvs,
									Rect coords,
									Color32 vertexColor,
									Vector2 uv1,
									Vector2 uv2,
									Vector3 normal,
									Vector4 tangent)
		{
			var startVertexIndex = vh.currentVertCount;
			vh.AddVert(
				new Vector3(position.xMin, position.yMin, z),
				vertexColor,
				new Vector2(uvs.xMin, uvs.yMin),
				uv1,
				uv2,
				new Vector2(coords.xMin, coords.yMin),
				normal,
				tangent);
			vh.AddVert(
				new Vector3(position.xMax, position.yMin, z),
				vertexColor,
				new Vector2(uvs.xMax, uvs.yMin),
				uv1,
				uv2,
				new Vector2(coords.xMax, coords.yMin),
				normal,
				tangent);
			vh.AddVert(
				new Vector3(position.xMax, position.yMax, z),
				vertexColor,
				new Vector2(uvs.xMax, uvs.yMax),
				uv1,
				uv2,
				new Vector2(coords.xMax, coords.yMax),
				normal,
				tangent);
			vh.AddVert(
				new Vector3(position.xMin, position.yMax, z),
				vertexColor,
				new Vector2(uvs.xMin, uvs.yMax),
				uv1,
				uv2,
				new Vector2(coords.xMin, coords.yMax),
				normal,
				tangent);
			vh.AddTriangle(startVertexIndex, startVertexIndex + 3, startVertexIndex + 2);
			vh.AddTriangle(startVertexIndex, startVertexIndex + 2, startVertexIndex + 1);
		}

		private static float PackVector3ToFloat(float x, float y, float z)
		{
			return PackVector3ToFloat(new Vector3(x, y, z));
		}

		private static float PackVector3ToFloat(this Vector3 vector)
		{
			return Vector3.Dot(Vector3Int.RoundToInt(vector * 255), new Vector3(65536, 256, 1));
		}
	}
}
