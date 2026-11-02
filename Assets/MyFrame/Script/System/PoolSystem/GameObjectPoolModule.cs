using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyFrame
{
    public class GameObjectPoolModule
    {
        public Transform poolRootParent;
        public Dictionary<string, GameObjectPoolData> poolDic = new Dictionary<string, GameObjectPoolData>();

        public void Init(Transform poolRootParent)
        {
            this.poolRootParent = poolRootParent;
        }

        #region 物件池数据的创建

        public void InitGameObjectPool(string poolName,string objectName,int maxCapacity = -1,int defaultQuantity = 0,GameObject prefab = null,Action<GameObject> getAction = null, Action<GameObject> pushAction = null)
        {
            if (defaultQuantity > maxCapacity && maxCapacity != -1)
            {
                Debug.LogError("默认容量超出最大容量限制");
                return;
            }
            if (!prefab)
            {
                Debug.LogError("预制体不能为空");
                return;
            }
            if (poolDic.TryGetValue(poolName, out GameObjectPoolData poolData))
            {
                poolData.MaxCapacity = maxCapacity;
                poolData.PushAction = pushAction;
                poolData.GetAction = getAction;
                var deltaNumber = defaultQuantity - poolData.PoolQueue.Count;
                if (deltaNumber > 0)
                {
                    for (int i = 0; i < deltaNumber; i++)
                    {
                        var go =  GameObject.Instantiate(prefab);
                        if (prefab == null) return;
                        poolData.PushGameObject(go);
                    }
                }
            }
            else
            {
                poolData = CreateGameObjectPoolData(poolName,objectName,maxCapacity, getAction, pushAction);
                if (prefab == null) return;
                for (int i = 0; i < defaultQuantity; i++)
                {
                    var go = GameObject.Instantiate(prefab);
                    poolData.PushGameObject(go);
                }
            }
        }
        
        public void InitGameObjectPool(GameObject gameObject,string poolName = null,int maxCapacity = -1, int defaultQuantity = 0, Action<GameObject> getAction = null, Action<GameObject> pushAction = null)
        {
            InitGameObjectPool(poolName,gameObject.name,maxCapacity, defaultQuantity,gameObject, getAction, pushAction);
        }
        
        public void InitGameObjectPool(string poolName,string objectName,GameObject[] gameObjects, int maxCapacity = -1, Action<GameObject> getAction = null, Action<GameObject> pushAction = null)
        {
            if (gameObjects.Length > maxCapacity && maxCapacity != -1)
            {
                Debug.LogError("默认容量超出最大容量限制");
                return;
            }
            if (poolDic.TryGetValue(poolName, out GameObjectPoolData poolData))
            {
                poolData.MaxCapacity = maxCapacity;
                poolData.PushAction = pushAction;
                poolData.GetAction = getAction;
            }
            else
            {
                poolData = CreateGameObjectPoolData(poolName,objectName,maxCapacity, getAction, pushAction);
            }

            if (maxCapacity == -1)
            {
                foreach (var go in gameObjects)
                {
                    poolData.PushGameObject(go);
                }
            }
            else if (poolData.PoolQueue.Count < maxCapacity)
            {
                var deltaNumber = Math.Min(maxCapacity - poolData.PoolQueue.Count, gameObjects.Length);
                for (int i = 0; i < deltaNumber; i++)
                {
                    poolData.PushGameObject(gameObjects[i]);
                }
            }
        }
        
        private GameObjectPoolData CreateGameObjectPoolData(string poolName,string objectName,int maxCapacity = -1, Action<GameObject> getAction = null, Action<GameObject> pushAction = null)
        {
            var poolData = PoolSystem.GetObject<GameObjectPoolData>() ??  new GameObjectPoolData();
            poolData.Init(poolName,objectName,poolRootParent,maxCapacity, getAction, pushAction);
            poolDic.Add(poolName, poolData); 
            return poolData;
        }
        #endregion
        #region 物件存取

        public GameObject GetGameObject(string poolName,Transform parent = null,bool worldPositionStays = false)
        {
            if (!poolDic.TryGetValue(poolName, out GameObjectPoolData poolData)) return null;
            var go =  poolData.GetGameObject(parent,worldPositionStays);
            return go;
        }
        public void PushGameObject(GameObject go)
        {
            PushGameObject($"{go.name}Pool",go);
        }
        public void PushGameObject(string poolName,GameObject go)
        {
            if (!go)
            {
                Debug.LogError("当前物体为空不可入池");
                return;
            }
            if (!poolDic.TryGetValue(poolName, out GameObjectPoolData poolData))
            {
               InitGameObjectPool(go,poolName);
               poolDic[poolName].PushGameObject(go);
               return;
            }
            poolData.PushGameObject(go);
        }

        public bool RemovePool(string poolName)
        {
            if(!poolDic.TryGetValue(poolName, out GameObjectPoolData poolData)) return false;
            poolData.Destroy();
            poolDic.Remove(poolName);
            return true;
        }

        public void ClearPools()
        {
            foreach (var poolData in poolDic.Values)
            {
                poolData.Destroy();
            }
            poolDic.Clear();
        }
        #endregion
    }
}