using System;
using System.Collections.Generic;
using MyFrame;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ResSystem
{
    #region 可new的class对象池的使用

    public static T GetOrNew<T>() where T : class, new()
    {
        var obj = PoolSystem.GetObject<T>();
        if(obj == null) return new T();
        return obj;
    }

    public static T GetOrNew<T>(string keyName) where T : class, new()
    {
        T obj = PoolSystem.GetObject<T>(keyName);
        if (obj == null) obj = new T();
        return obj;
    }

    public static void PushObject<T>(T obj) where T : class
    {
        PoolSystem.PushObject(obj);
    }

    public static void PushObject<T>(T obj, string keyName) where T : class, new()
    {
        PoolSystem.PushObject(obj, keyName);
    }

    public static void InitObjectPool<T>(string poolName = null, int maxCapacity = -1, int defaultQuantity = 0) where T : class, new()
    {
       if(poolName != null) PoolSystem.InitObjectPool<T>(poolName,maxCapacity,defaultQuantity);
       else PoolSystem.InitObjectPool<T>(typeof(T).FullName,maxCapacity,defaultQuantity);
    }
    
    
    #endregion

    #region GameObject的对象池使用

    public static void InitGameObjectPoolForKeyName(string poolName,string objectName,int maxCapacity = -1, string assetName = null, int defaultQuantity = 0)
    {
        if (defaultQuantity <= 0 || assetName == null)
        {
            PoolSystem.InitGameObjectPool(poolName,objectName,maxCapacity, defaultQuantity);
        }
        else
        {
            GameObject[] gameObjects = new GameObject[defaultQuantity];
            for (int i = 0; i < defaultQuantity; i++)
            {
                gameObjects[i] = Addressables.InstantiateAsync(assetName).WaitForCompletion();
            }
            PoolSystem.InitGameObjectPool(poolName,objectName,gameObjects,maxCapacity);
        }
    }

    public static void InitGameObjectPoolForAssetName(string assetName, int maxCapacity = -1, int defaultQuantity = 0)
    {
        InitGameObjectPoolForKeyName($"{assetName}_Pool",assetName,maxCapacity, assetName, defaultQuantity);
    }

    public static void PushGameObject(string poolName, GameObject go)
    {
        PoolSystem.PushGameObject(poolName, go);
    }

    public static void PushGameObject(GameObject go)
    {
        PoolSystem.PushGameObject(go);
    }
    
    public static GameObject InstantiateGameObject(Transform parent,string objName, string poolName = null, bool autoRelease = true)
    {
        GameObject go = null;
        if(poolName != null)go = PoolSystem.GetGameObject(poolName, parent);
        if(!go)
        {
            go = Addressables.InstantiateAsync(objName, parent).WaitForCompletion();
            if (autoRelease)
            {
                go.transform.OnReleaseAddressableAsset(AutomaticReleaseAssetAction);
            }
            go.name = objName;
        }
        return go;
    }
    
    public static void InstantiateGameObjectAsync(Transform parent, string poolName, string objName,Action<GameObject> callback, bool autoRelease = true)
    {
        GameObject go;
        go = PoolSystem.GetGameObject(poolName, parent);
        if (go) callback?.Invoke(go);
        else
        {
            Addressables.InstantiateAsync(objName, parent).Completed += (handle) =>
            {
                OnInstantiateGameObjectAsyncCompleted(handle, callback, objName, autoRelease);
            };
        }
    }
    
    private static void OnInstantiateGameObjectAsyncCompleted(AsyncOperationHandle<GameObject> handle, Action<GameObject> callback, string gameObjectName, bool autoRelease = true)
    {
        handle.Result.name = gameObjectName;
        if (autoRelease)
        {
            handle.Result.transform.OnReleaseAddressableAsset(AutomaticReleaseAssetAction);
        }
        callback?.Invoke(handle.Result);
    }
    
    private static void OnInstantiateGameObjectAsyncCompleted<T>(AsyncOperationHandle<GameObject> handle, Action<T> callback, string gameObjectName, bool autoRelease = true) where T : Component
    {
        handle.Result.name = gameObjectName;
        if (autoRelease)
        {
            handle.Result.transform.OnReleaseAddressableAsset(AutomaticReleaseAssetAction);
        }
        callback?.Invoke(handle.Result.GetComponent<T>());
    }
    
    public static GameObject InstantiateGameObject(string assetName, Transform parent = null, string poolName = null,string objName = null, bool autoRelease = true)
    {
        GameObject go;
        if (poolName == null) go = PoolSystem.GetGameObject(assetName, parent);
        else go = PoolSystem.GetGameObject(poolName, parent);
        if(!go)
        {
            go = Addressables.InstantiateAsync(assetName, parent).WaitForCompletion();
            if (autoRelease)
            {
                go.transform.OnReleaseAddressableAsset(AutomaticReleaseAssetAction);
            }
            go.name = objName ?? assetName;
        }
        return go;
    }
    
    public static void InstantiateGameObjectAsync(string assetName, Action<GameObject> callback, Transform parent = null, string poolName = null,string objName = null, bool autoRelease = true)
    {
        GameObject go;
        if (poolName == null) go = PoolSystem.GetGameObject(assetName, parent);
        else go = PoolSystem.GetGameObject(poolName, parent);
        if (go) callback?.Invoke(go);
        else
        {
            Addressables.InstantiateAsync(assetName, parent).Completed += (handle) =>
            {
                OnInstantiateGameObjectAsyncCompleted(handle, callback, objName ?? assetName, autoRelease);
            };
        }
    }
    
    public static T InstantiateGameObject<T>(Transform parent, string poolName,string objName, bool autoRelease = true) where T : Component
    {
        GameObject go = InstantiateGameObject(parent,objName, poolName,autoRelease);
        if (go) return go.GetComponent<T>();
        else return null;
    }
    
    public static T InstantiateGameObject<T>(string assetName, Transform parent = null, string poolName = null,string objName =null,bool autoRelease = true) where T : Component
    {
        GameObject go = InstantiateGameObject(assetName, parent, poolName,objName,autoRelease);
        if (go) return go.GetComponent<T>();
        else return null;
    }
    
    public static void InstantiateGameObjectAsync<T>(string assetName, Action<T> callback = null, Transform parent = null, string keyName = null, bool autoRelease = true) where T : Component
    {
        GameObject go;
        if (keyName == null) go = PoolSystem.GetGameObject(assetName, parent);
        else go = PoolSystem.GetGameObject(keyName, parent);
        // 对象池中有
        if (go)
        {
            if (autoRelease) go.transform.OnReleaseAddressableAsset(AutomaticReleaseAssetAction);
            callback?.Invoke(go.GetComponent<T>());
            return;
        }
        // 不通过缓存池
        Addressables.InstantiateAsync(assetName, parent).Completed += (handle) =>
        {
            OnInstantiateGameObjectAsyncCompleted<T>(handle, callback, keyName ?? assetName, autoRelease);
        };
    }
    public static bool UnloadInstance(GameObject obj)
    {
        return Addressables.ReleaseInstance(obj);
    }
    
    private static void AutomaticReleaseAssetAction(GameObject obj)
    {
        Addressables.ReleaseInstance(obj);
    }
    #endregion

    #region 游戏内Asset

    public static T LoadAsset<T>(string assetName) where T : UnityEngine.Object
    {
        return Addressables.LoadAssetAsync<T>(assetName).WaitForCompletion();
    }
    
    public static void LoadAssetAsync<T>(string assetName, Action<T> callback) where T : UnityEngine.Object
    {
        Addressables.LoadAssetAsync<T>(assetName).Completed += (handle) =>
        {
            OnLoadAssetAsyncCompleted<T>(handle, callback);
        };
    }

    public static void OnLoadAssetAsyncCompleted<T>(AsyncOperationHandle<T> handle, Action<T> callback) where T : UnityEngine.Object
    {
        callback?.Invoke(handle.Result);
    }
    
    public static IList<T> LoadAssets<T>(string keyName, out AsyncOperationHandle<IList<T>> handle, Action<T> callBackOnEveryOne = null) where T : UnityEngine.Object
    {
        handle = Addressables.LoadAssetsAsync<T>(keyName, callBackOnEveryOne, true);
        return handle.WaitForCompletion();
    }
    
    public static void UnloadAsset<T>(T obj)
    {
        Addressables.Release(obj);
    }
    
    public static void UnLoadAssetsHandle<TObject>(AsyncOperationHandle<TObject> handle)
    {
        Addressables.Release(handle);
    }
    #endregion
}
