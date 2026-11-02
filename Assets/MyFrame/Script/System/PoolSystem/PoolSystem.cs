using System;
using UnityEngine;

namespace MyFrame
{
    public static class PoolSystem
    {
        public const string PoolDataRootKey = "PoolDataRoot";
        private static GameObjectPoolModule GameObjectPoolModule;
        private static ObjectPoolModule ObjectPoolModule;
        private static Transform poolRootTransform;
        
        public static void Init()
        {
            GameObjectPoolModule = new GameObjectPoolModule();
            ObjectPoolModule = new ObjectPoolModule();
            poolRootTransform = MyFrameRoot.Instance.transform.Find("PoolRoot") ?? new GameObject("PoolRoot").transform;
            poolRootTransform.position = Vector3.zero;
            poolRootTransform.SetParent(MyFrameRoot.FrameRootTransform);
            GameObjectPoolModule.Init(poolRootTransform); ;
        }


        #region 与GameObject池相关的方法

        public static void InitGameObjectPool(string poolName, string objectName, int maxCapacity = -1, int defaultQuantity = 0, GameObject prefab = null,Action<GameObject> getAction = null, Action<GameObject> pushAction = null)
        {
            GameObjectPoolModule.InitGameObjectPool(poolName, objectName, maxCapacity, defaultQuantity, prefab, getAction, pushAction);
        }
        
        public static void InitGameObjectPool(GameObject prefab ,string poolName = null,int maxCapacity = -1, int defaultQuantity = 0, Action<GameObject> getAction = null, Action<GameObject> pushAction = null)
        {
            poolName ??= $"{prefab.name}Pool";
            GameObjectPoolModule.InitGameObjectPool(poolName, prefab.name, maxCapacity, defaultQuantity,prefab, getAction, pushAction);
        }
        
        public static void InitGameObjectPool(string poolName, string objectName, GameObject[] gameObjects, int maxCapacity = -1, Action<GameObject> getAction = null, Action<GameObject> pushAction = null)
        {
            GameObjectPoolModule.InitGameObjectPool(poolName, objectName, gameObjects, maxCapacity, getAction, pushAction);
        }

        public static void InitGameObject(GameObject prefab, string poolName = null, int maxCapacity = -1, int defaultQuantity = 0, Action<GameObject> getAction = null, Action<GameObject> pushAction = null)
        {
            poolName ??= $"{prefab.name}Pool";
            GameObjectPoolModule.InitGameObjectPool(prefab, poolName, maxCapacity, defaultQuantity, getAction, pushAction);
        }

        
        public static GameObject GetGameObject(string poolName, Transform rootParent = null,bool worldPositionStays = false)
        {
            return GameObjectPoolModule.GetGameObject(poolName,rootParent,worldPositionStays);
        }

        public static GameObject GetGameObject(GameObject prefab, Transform rootParent = null,bool worldPositionStays = false)
        {
            var obj = GameObjectPoolModule.GetGameObject($"{prefab.name}Pool",rootParent,worldPositionStays);
            if (!obj)
            {
               obj = GameObject.Instantiate(prefab, rootParent,worldPositionStays);
               obj.name = prefab.name;//修改名称，防止带上Clone标签出现入池错误
            }
            return obj;
        }
        
        public static void PushGameObject(string poolName, GameObject gameObject)
        {
            GameObjectPoolModule.PushGameObject(poolName, gameObject);
        }

        public static void PushGameObject(GameObject gameObject)
        {
            GameObjectPoolModule.PushGameObject(gameObject);
        }

        public static void RemoveGameObjectPool(string poolName)
        {
            GameObjectPoolModule.RemovePool(poolName);
        }

        public static void RemoveGameObjectPool(GameObject gameObject)
        {
            GameObjectPoolModule.RemovePool(gameObject.name);
        }

        public static void ClearGameObjectPool()
        {
            GameObjectPoolModule.ClearPools();
        }
        #endregion


        #region 与普通Object类相关的方法

        public static void InitObjectPool<T>(string poolName, Func<T> createFunc, int maxCapacity = -1, int defaultQuantity = 0, Action<T> getAction = null, Action<T> pushAction = null) where T : class
        {
            ObjectPoolModule.InitPoolData(poolName, createFunc, maxCapacity, defaultQuantity, getAction, pushAction);
        }
        public static void InitObjectPool<T>(string poolName, int maxCapacity = -1, int defaultQuantity = 0, Func<T> createFunc = null, Action<T> getAction = null, Action<T> pushAction = null) where T : class,new()
        {
            ObjectPoolModule.InitPoolData(poolName, maxCapacity, defaultQuantity, createFunc, getAction, pushAction);
        }

        public static void InitObjectPool<T>(int maxCapacity = -1, int defaultQuantity = 0, Func<T> createFunc = null, Action<T> getAction = null, Action<T> pushAction = null) where T : class, new()
        {
            ObjectPoolModule.InitPoolData(maxCapacity, defaultQuantity, createFunc, getAction, pushAction);
        }
        public static T GetObject<T>() where T : class
        {
             return ObjectPoolModule.GetObject<T>();
        }
        public static T GetObject<T>(string poolName) where T : class
        {
            return ObjectPoolModule.GetObject<T>(poolName);
        }
        public static void PushObject<T>(T obj)  where T : class
        {
           ObjectPoolModule.PushObject<T>(obj);
        }
        public static void PushObject<T>(T obj, string poolName) where T : class
        {
            ObjectPoolModule.PushObject(obj, poolName);
        }

        public static void RemoveObjectPool(string poolName)
        {
            ObjectPoolModule.RemovePool(poolName);
        }

        public static void RemoveObjectPool<T>()
        {
            ObjectPoolModule.RemovePool<T>();
        }
        #endregion
 
        
    }
}