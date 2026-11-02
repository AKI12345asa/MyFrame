using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyFrame
{
    public class ObjectPoolModule
    {
        public readonly Dictionary<string, ObjectPoolData> PoolDic = new();

        #region 物件池创建

        /// <summary>
        /// 非可new的数据在初始化数据池的时候 需要给定createFunc
        /// </summary>
        /// <param name="poolName"></param>
        /// <param name="createFunc"></param>
        /// <param name="maxCapacity"></param>
        /// <param name="defaultQuantity"></param>
        /// <param name="getAction"></param>
        /// <param name="pushAction"></param>
        /// <typeparam name="T"></typeparam>
        public void InitPoolData<T>(string poolName, Func<T> createFunc, int maxCapacity = -1, int defaultQuantity = 0, Action<T> getAction = null, Action<T> pushAction = null)where T : class
        {
            if (defaultQuantity > maxCapacity && maxCapacity != -1)
            {
                Debug.LogError("默认容量超出最大容量限制");
                return;
            }
            if (PoolDic.TryGetValue(poolName, out ObjectPoolData poolData))
            {
                var data = poolData as ObjectPoolData<T>;
                data.MaxCapacity = maxCapacity;
                data.CreateFunc = createFunc;
                data.GetAction = getAction;
                data.PushAction = pushAction;
                var deltaNumber = defaultQuantity - data.PoolQueue.Count;
                if (deltaNumber > 0)
                {
                    for (int i = 0; i < deltaNumber; i++)
                    {
                       var obj = createFunc();
                       data.PushObject(obj);
                    }
                }
            }
            else
            {
                CreatePoolData(poolName,maxCapacity, createFunc, getAction, pushAction);
                var data = PoolDic[poolName] as ObjectPoolData<T>;
                for (int i = 0; i < defaultQuantity; i++)
                {
                    var obj = createFunc();
                    data.PushObject(obj);
                }
            }
            
        }

        public void InitPoolData<T>(string poolName, int maxCapacity = -1, int defaultQuantity = 0, Func<T> createFunc = null, Action<T> getAction = null, Action<T> pushAction = null)  where T : class, new()
        {
            if (defaultQuantity > maxCapacity && maxCapacity != -1)
            {
                Debug.LogError("默认容量超出最大容量限制");
                return;
            }
            if (PoolDic.TryGetValue(poolName, out ObjectPoolData poolData))
            {
                var data = poolData as ObjectPoolData<T>;
                data.MaxCapacity = maxCapacity;
                data.CreateFunc = createFunc;
                data.GetAction = getAction;
                data.PushAction = pushAction;
                var deltaNumber = defaultQuantity - data.PoolQueue.Count;
                if (deltaNumber > 0)
                {
                    for (int i = 0; i < deltaNumber; i++)
                    {
                        T obj;
                        if(createFunc != null) obj = createFunc();
                        else obj = new T();
                        data.PushObject(obj);
                    }
                }
            }
            else
            {
                CreatePoolData(poolName,maxCapacity, createFunc, getAction, pushAction);
                var data = PoolDic[poolName] as ObjectPoolData<T>;
                for (int i = 0; i < defaultQuantity; i++)
                {
                    T obj;
                    if(createFunc != null) obj = createFunc();
                    else obj = new T();
                    data.PushObject(obj);
                }
            }
        }

        private void InitPoolData<T>(string poolName, Func<T> createFunc = null, int maxCapacity = -1, Action<T> getAction = null, Action<T> pushAction = null) where T : class
        {
            if (PoolDic.TryGetValue(poolName, out ObjectPoolData poolData))
            {
                var data = poolData as ObjectPoolData<T>;
                data.MaxCapacity = maxCapacity;
                data.CreateFunc = createFunc;
                data.GetAction = getAction;
                data.PushAction = pushAction;
            }
            else
            {
                CreatePoolData(poolName,maxCapacity, createFunc, getAction, pushAction);
            }
        }
        
        public void InitPoolData<T>(int maxCapacity = -1, int defaultQuantity = 0, Func<T> createFunc = null, Action<T> getAction = null, Action<T> pushAction = null) where T : class, new()
        {
            InitPoolData<T>(typeof(T).FullName, maxCapacity, defaultQuantity, createFunc, pushAction);
        }
        private void CreatePoolData<T>(string poolName,int maxCapacity = -1, Func<T> createFunc = null, Action<T> getAction = null, Action<T> pushAction = null) where T : class
        {
            var poolData = PoolSystem.GetObject<ObjectPoolData<T>>() ?? new ObjectPoolData<T>();
            poolData.Init(maxCapacity, createFunc, getAction, pushAction);
            PoolDic.Add(poolName, poolData);
        }
        #endregion

        #region 物件存取
        public T GetObject<T>(string poolName) where T : class
        {
            if (!PoolDic.TryGetValue(poolName, out ObjectPoolData poolData)) return null;
            return poolData.GetObject<T>();
        }

        public T GetObject<T>()where T : class
        {
            if (!PoolDic.TryGetValue(typeof(T).FullName, out ObjectPoolData poolData)) return null;
            return poolData.GetObject<T>();
        }

        public void PushObject<T>(T obj) where T : class
        {
            if (obj == null)
            {
                Debug.LogError("空对象无法入池");
                return;
            }
            var typeFullName = obj.GetType().FullName;
            if (typeFullName == null) return;
            if (!PoolDic.TryGetValue(typeFullName, out ObjectPoolData poolData))
            {
                InitPoolData<T>(typeFullName);
                PoolDic[typeFullName].PushObject(obj);
                return;
            }
            poolData.PushObject(obj);
        }

        public void PushObject<T>(T obj, string poolName) where T : class
        {
            if (!PoolDic.TryGetValue(poolName, out ObjectPoolData poolData))
            {
                Debug.LogError($"没有找到名为{poolName}的对象池，无法入池");
                return;
            }
            if (obj == null)
            {
                Debug.LogError("空对象无法入池");
                return;
            }
            poolData.PushObject(obj);
        }

        public void RemovePool(string poolName)
        {
            PoolDic.Remove(poolName);
        }        
        
        public void RemovePool<T>()
        {
            PoolDic.Remove(typeof(T).FullName);
        }

        public void ClearPool()
        {
            PoolDic.Clear();
        }
        #endregion
    }
}