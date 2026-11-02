using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyFrame
{
    public class GameObjectPoolData
    {
        public Transform poolRoot;
        public int MaxCapacity = -1;
        public string objectName;
        public readonly Queue<GameObject> PoolQueue = new();
        public Action<GameObject> GetAction;
        public Action<GameObject> PushAction;
        #region 物件池的生命周期
        public void Init(string poolName,string objectName,Transform rootParent, int maxCapacity = -1,Action<GameObject> getAction = null,Action<GameObject> pushAction = null)
        {
            var root = PoolSystem.GetGameObject(PoolSystem.PoolDataRootKey, rootParent);
            if (!root)
            {
                root = new GameObject(poolName);
                root.transform.SetParent(rootParent);
            }
            poolRoot = root.transform;
            poolRoot.name = poolName;
            this.objectName = objectName;
            GetAction = getAction;
            PushAction = pushAction;
            this.MaxCapacity = maxCapacity;
        }
        public void Destroy()
        {
            foreach (var go in PoolQueue)
            {
               GameObject.Destroy(go);
            }
            MaxCapacity = -1;
            PoolQueue.Clear();
            GetAction = null;
            PushAction = null;
            PoolSystem.PushGameObject(PoolSystem.PoolDataRootKey, poolRoot.gameObject);
            PoolSystem.PushObject(this);
        }
        #endregion

        #region 物件存取

        public bool PushGameObject(GameObject objectToPush)
        {
            PushAction?.Invoke(objectToPush);
            if (MaxCapacity != -1 && PoolQueue.Count >= MaxCapacity)
            {
                GameObject.Destroy(objectToPush);
                return false;
            }
            objectToPush.transform.SetParent(poolRoot);
            objectToPush.SetActive(false);
            objectToPush.transform.name = objectName;
            PoolQueue.Enqueue(objectToPush);
            return true;
        }
        public GameObject GetGameObject(Transform parent,bool worldPositionStays)
        {
            if (PoolQueue.Count == 0) return null;
            var go = PoolQueue.Dequeue();
            go.transform.SetParent(parent, worldPositionStays);
            go.SetActive(true);
            if (parent == null)
            {
                // 回归默认场景
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }
            GetAction?.Invoke(go);
            return go;
        }
        #endregion
    }
}