using UnityEngine;
using System.Collections.Generic;
using Beamable.Service;
using Beamable.Extensions;

namespace Beamable.Pooling
{
   public class DisablePool : MonoBehaviour
   {
      public readonly Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();

      #region Spawn
      public GameObject Spawn(GameObject prefab)
      {
         List<GameObject> pool;
         if (pools.TryGetValue(prefab, out pool))
         {
            for (int i = 0; i < pool.Count; ++i)
            {
               if (!pool[i].activeSelf)
               {
                  pool[i].gameObject.SetActive(true);
                  return pool[i];
               }
            }

            var newInstance = Object.Instantiate(prefab);
            pool.Add(newInstance);
            return newInstance;
         }
         else
         {
            pool = new List<GameObject>();
            var newInstance = Object.Instantiate(prefab);
            pool.Add(newInstance);

            pools.Add(prefab, pool);

            return newInstance;
         }
      }

      public GameObject SpawnOnParent(GameObject prefab, Transform parent)
      {
         var instance = Spawn(prefab);
         instance.transform.SetParent(parent, false);
         return instance;
      }

      public GameObject SpawnAtPosition(GameObject prefab, Vector3 position)
      {
         var instance = Spawn(prefab);
         instance.transform.position = position;
         return instance;
      }

      //LocalPosition is an optimization of position because no conversion to/from world space has to happen.
      //if you can use this one its better...
      public GameObject SpawnAtLocalPosition(GameObject prefab, Vector3 position)
      {
         var instance = Spawn(prefab);
         instance.transform.localPosition = position;
         return instance;
      }
      #endregion

      #region SpawnByType
      public T Spawn<T>(T prefab) where T : MonoBehaviour
      {
         return Spawn(prefab.gameObject).GetComponent<T>();
      }

      public T SpawnOnParent<T>(T prefab, Transform parent) where T : MonoBehaviour
      {
         return SpawnOnParent(prefab.gameObject, parent).GetComponent<T>();
      }

      public T SpawnAtPosition<T>(T prefab, Vector3 position) where T : MonoBehaviour
      {
         return SpawnAtPosition(prefab.gameObject, position).GetComponent<T>();
      }

      public T SpawnAtLocalPosition<T>(T prefab, Vector3 position) where T : MonoBehaviour
      {
         return SpawnAtLocalPosition(prefab.gameObject, position).GetComponent<T>();
      }
      #endregion

      #region Recycle
      public virtual void RecycleAllChildren(Transform t)
      {
         if(t != null)
         {
            t.SetActiveAllChildren(false);
         }
      }

      public virtual void Recycle(GameObject instance)
      {
         instance.SetActive(false);
      }
      #endregion
   }

#if UNITY_EDITOR
   public class DisablePoolProfiler
   {
      [UnityEditor.MenuItem(PoolingConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES +
         "/Show DisablePool Stats in Console",
         priority = PoolingConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_3)]
      public static void PrintStats()
      {
         if(!ServiceManager.CanResolve<DisablePool>())
         {
            Debug.Log("Could not resolve DisablePool");
            return;
         }

         var disablePool = ServiceManager.Resolve<DisablePool>();

         using (var pb = StringBuilderPool.StaticPool.Spawn())
         {
            var builder = pb.Builder;
            foreach (var key in disablePool.pools.Keys)
            {
               int count = disablePool.pools[key].Count;
               builder.AppendLine(string.Format("{0}    {1}", count, key));
            }
            Debug.Log(builder);
         }
      }

   }
#endif

}
