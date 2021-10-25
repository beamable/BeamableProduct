using System;
using UnityEngine;

namespace Beamable.Common.BeamableVectorIntExtensions
{
    [Serializable]
    public class Vector2IntEx
    {
        public int x;
        public int y;

        public Vector2IntEx(Vector2Int vec)
        {
            x = vec.x;
            y = vec.y;
        }

#if UNITY_EDITOR || UNITY_ENGINE
        public static Vector2Int DeserializeToVector2(string json)
        {
            Vector2IntEx tmp = JsonUtility.FromJson<Vector2IntEx>(json);
            return new Vector2Int(tmp.x, tmp.y);
        }
#endif
    }

    [Serializable]
    public class Vector3IntEx : Vector2IntEx
    {
        public int z;

        public Vector3IntEx(Vector3Int vec) : base(new Vector2Int(vec.x, vec.y))
        {
            z = vec.z;
        }

#if UNITY_EDITOR || UNITY_ENGINE
        public static Vector3Int DeserializeToVector3(string json)
        {
            Vector3IntEx tmp = JsonUtility.FromJson<Vector3IntEx>(json);
            return new Vector3Int(tmp.x, tmp.y, tmp.z);
        }
#endif
    }
}
