using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace MyFrame
{
    /// <summary>
    /// 一个存档的数据
    /// </summary>
    [Serializable]
    public class SaveItem
    {
        public int saveID;
        private DateTime lastSaveTime;
        public DateTime LastSaveTime
        {
            get
            {
                if (lastSaveTime == default(DateTime))
                {
                    DateTime.TryParse(lastSaveTimeString, out lastSaveTime);
                }
                return lastSaveTime;
            }
        }
        [SerializeField] private string lastSaveTimeString; // Json不支持DateTime，用来持久化的
        public SaveItem(int saveID, DateTime lastSaveTime)
        {
            this.saveID = saveID;
            this.lastSaveTime = lastSaveTime;
            lastSaveTimeString = lastSaveTime.ToString();
        }

        public void UpdateTime(DateTime lastSaveTime)
        {
            this.lastSaveTime = lastSaveTime;
            lastSaveTimeString = lastSaveTime.ToString();
        }
    }

    public static class SaveSystem
    {
        [Serializable]
        private class SaveSystemData
        {
            // 当前的存档ID
            public int currID = 0;

            // 所有存档的列表
            public List<SaveItem> saveItemList = new List<SaveItem>();
            public SaveItem testItem = new(-1, DateTime.Now);
        }

        private static SaveSystemData saveSystemData;

        //存档和设置文件夹的名称
        private const string SaveDirName = "SaveData";
        private const string SettingDirName = "Setting";

        //路径
        private static string SaveDirPath;
        private static string SettingDirPath;

        private static Dictionary<int, Dictionary<string, GameDataContainer>> cacheDic = new ();
        private static Dictionary<Type,SettingDataContainer> settingDataDic = new ();
#if UNITY_EDITOR
        static SaveSystem()
        {
            Init();
        }
#endif

        public static void Init()
        {
            SaveDirPath = Application.persistentDataPath + "/" + SaveDirName;
            SettingDirPath = Application.persistentDataPath + "/" + SettingDirName;
#if UNITY_EDITOR
            // 
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            Debug.Log(SettingDirPath);
#endif
            //检查当前文件夹
            CheckAndCreateDir();
            // 初始化SaveSystemData
            InitSaveSystemData();
        }


        #region 存档管理

        #region 维护存档整体

        public static List<SaveItem> GetAllSaveItems()
        {
            List<SaveItem> saveItems = new List<SaveItem>(saveSystemData.saveItemList.Count);

            for (int i = 0; i < saveSystemData.saveItemList.Count; i++)
            {
                saveItems.Add(saveSystemData.saveItemList[^(i + 1)]);
            }
            return saveItems;
        }
        
        public static List<SaveItem> GetAllSaveItemByUpdateTime()
        {
            List<SaveItem> saveItems = new List<SaveItem>(saveSystemData.saveItemList.Count);
            for (int i = 0; i < saveSystemData.saveItemList.Count; i++)
            {
                saveItems.Add(saveSystemData.saveItemList[i]);
            }
            OrderByUpdateTimeComparer orderBy = new OrderByUpdateTimeComparer();
            saveItems.Sort(orderBy);
            return saveItems;
        }

        private class OrderByUpdateTimeComparer : IComparer<SaveItem>
        {
            public int Compare(SaveItem x, SaveItem y)
            {
                if (x.LastSaveTime > y.LastSaveTime)
                {
                    return -1;
                }
                if (x.LastSaveTime < y.LastSaveTime)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        public static List<SaveItem> GetAllSaveItems<T>(Func<SaveItem, T> orderFunc, bool isDescending = false)
        {
            if (isDescending)
            {
                return saveSystemData.saveItemList.OrderByDescending(orderFunc).ToList();
            }
            else
            {
                return  saveSystemData.saveItemList.OrderBy(orderFunc).ToList();
            }
        }

        public static void DeleteAllSaveItem()
        {
            if (Directory.Exists(SaveDirPath))
            {
                Directory.Delete(SaveDirPath, true);
            }
            CheckAndCreateDir();
            InitSaveSystemData();
        }
        
        public static void DeleteAll()
        {
            CleanCache();
            DeleteAllSaveItem();
            DeleteAllSetting();
        }

        #endregion


        #region 维护使用某一个存档

        public static SaveItem GetSaveItem(int id)
        {
            if (id == -1)
            {
                return saveSystemData.testItem;
            }
            for (int i = 0; i < saveSystemData.saveItemList.Count; i++)
            {
                if (saveSystemData.saveItemList[i].saveID == id)
                {
                    return saveSystemData.saveItemList[i];
                }
            }
            return null;
        }
        
        
        /// <summary>
        /// 添加一个指定ID的存档,如果存在则覆盖
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static SaveItem CreateSaveItem(int id)
        {
            if (GetSaveItem(id) != null)
            {
                DeleteSaveItem(id);
            }
            SaveItem saveItem = new SaveItem(id, DateTime.Now);
            if (id == -1)
            {
                saveSystemData.testItem = saveItem;
                UpdateSaveSystemData();
                return saveItem;
            }
            saveSystemData.saveItemList.Add(saveItem);
            UpdateSaveSystemData();
            return saveItem;
        }

        public static void DeleteSaveItem(int saveID)
        {
            string itemDir = GetSavePath(saveID, false);
            // 如果路径存在 且 有效
            if (itemDir != null)
            {
                // 把这个存档下的文件递归删除
                Directory.Delete(itemDir, true);
            }
            saveSystemData.saveItemList.Remove(GetSaveItem(saveID));
            // 移除缓存
            RemoveCache(saveID);
            // 更新SaveSystemData 写入磁盘
            UpdateSaveSystemData();
        }
        
        public static void DeleteSaveItem(SaveItem saveItem)
        {
            DeleteSaveItem(saveItem.saveID);
        }
#endregion

        #endregion

        #region 缓存管理

        private static void SetCache(int saveID, string fileName, GameDataContainer data)
        {
            if (cacheDic.ContainsKey(saveID))
            {
                if (cacheDic[saveID].ContainsKey(fileName))
                {
                    cacheDic[saveID][fileName] = data;
                }
                else
                {
                    cacheDic[saveID].Add(fileName, data);
                }
            }
            else
            {
                cacheDic.Add(saveID, new Dictionary<string,GameDataContainer>() { { fileName, data} });
            }
        }


        private static bool TryGetCache<T>(int saveID, string fileName, ref T data) where T : GameDataContainer
        {
            if (cacheDic.TryGetValue(saveID, out var dict))
            {
                if (dict.TryGetValue(fileName, out var value))
                {
                    data = value as T;
                    return data != null;
                }
            }
            return false;
        }

        private static void RemoveCache(int saveID)
        {
            cacheDic.Remove(saveID);
        }
        
        private static void RemoveCache(int saveID, string fileName)
        {
            if (!cacheDic.TryGetValue(saveID, out var dict)) return;
            dict.Remove(fileName);
        }
        
        public static void CleanCache()
        {
            cacheDic.Clear();
        }
        #endregion

        #region 存档数据

        private static void SaveObject<T>(T data,string fileName,int saveID = 0) where T : GameDataContainer
        {
            var saveItem = GetSaveItem(saveID);
            if (saveItem == null)
            {
                throw new Exception("当前存档并未创建无法保存");
            }
            var dirPath = GetSavePath(saveID, true);
            var savePath = dirPath + "/" + fileName;
            SaveFile(data, savePath);
            saveItem.UpdateTime(DateTime.Now);
            UpdateSaveSystemData();
            SetCache(saveID, fileName, data);
        }

        public static void SaveObject<T>(T data, int saveID = 0) where T : GameDataContainer
        {
            SaveObject(data,data.GetType().Name, saveID);
        }

        public static T LoadObject<T>(string fileName, int saveID,bool ifUseCache = true) where T : GameDataContainer
        {
            T data = null;
            if(!ifUseCache || !TryGetCache(saveID, fileName, ref data))
            {
                var saveItem = GetSaveItem(saveID);
                if (saveItem == null)
                {
                    return null;
                }

                var dirPath = GetSavePath(saveID, false);
                if (dirPath == null)
                {
                    throw new Exception("当前存档路径不存在无法加载");
                }

                var loadPath = dirPath + "/" + fileName;
                data = LoadFile<T>(loadPath);
            }
            return data;
        }
        
        public static T LoadObject<T>(int saveID,bool ifUseCache = true) where T : GameDataContainer
        {
            return LoadObject<T>(typeof(T).Name, saveID,ifUseCache);
        }

        public static void DeleteObject(string fileName, int saveID)
        {
            var saveItem = GetSaveItem(saveID);
            if (saveItem == null)
            {
                throw new Exception("当前存档并未创建无法加载");
            }
            var dirPath = GetSavePath(saveID, false);
            var savePath = dirPath + "/" + fileName;
            RemoveCache(saveID,fileName);
            File.Delete(savePath);
        }

        public static void DeleteObject<T>(int saveID) where T : GameDataContainer
        {
            DeleteObject(typeof(T).Name, saveID);
        }
        #endregion

        #region 设置数据

        public static T LoadSetting<T>(string fileName) where T : SettingDataContainer
        {
            if(!settingDataDic.TryGetValue(typeof(T), out var settingData))
            {
                settingData = LoadFile<T>(SettingDirPath + "/" + fileName);
                Debug.Log(SettingDirPath + "/" + fileName);
                if (settingData != null)
                {
                    settingDataDic[typeof(T)] = settingData;
                }
            }
            return (T)settingData;
        }

        public static T LoadSetting<T>() where T : SettingDataContainer
        {
           return LoadSetting<T>(typeof(T).Name);
        }
        
        public static void SaveSetting<T>(T setting, string fileName) where T : SettingDataContainer
        {
            SaveFile(setting, SettingDirPath + "/" + fileName);
            settingDataDic[typeof(T)] = setting;
        }

        public static void SaveSetting<T>(T setting) where T : SettingDataContainer
        {
            SaveSetting(setting, typeof(T).Name);
        }

        public static void DeleteSetting<T>(string fileName) where T : SettingDataContainer
        {
            File.Delete(SettingDirPath + "/" + fileName);
            settingDataDic.Remove(typeof(T));
        }

        public static void DeleteSetting<T>() where T : SettingDataContainer
        {
            DeleteSetting<T>(typeof(T).Name);
        }
        public static void DeleteAllSetting()
        {
            if (Directory.Exists(SettingDirPath))
            {
                // 直接删除目录
                Directory.Delete(SettingDirPath, true);
            }
            CheckAndCreateDir();
            settingDataDic.Clear();
        }

        #endregion

        #region 内部工具函数

        private static void CheckAndCreateDir()
        {
            if (!Directory.Exists(SaveDirPath))
            {
                Directory.CreateDirectory(SaveDirPath);
            }

            if (!Directory.Exists(SettingDirPath))
            {
                Directory.CreateDirectory(SettingDirPath);
            }
        }

        private static string GetSavePath(int saveID,bool createDir = false)
        {
            if (GetSaveItem(saveID) == null) return null;
            var saveDirPath = SaveDirPath +  "/" + saveID;
            if (!Directory.Exists(saveDirPath))
            {
                if (createDir)
                {
                    Directory.CreateDirectory(saveDirPath);
                }
                else
                {
                    return null;
                }
            }
            return saveDirPath;
        }
        
        
        private static void SaveFile(object saveObject, string path)
        {
            switch (MyFrameRoot.Setting.SaveSystemType)
            {
                case SaveSystemType.Binary:
                    IOTool.SaveFile(saveObject, path);
                    break;
                case SaveSystemType.Json:
                    string jsonData = JsonUtility.ToJson(saveObject);
                    IOTool.SaveJson(jsonData, path);
                    break;
            }
        }
        
        public static void InitSaveSystemData()
        {
            saveSystemData = LoadFile<SaveSystemData>(SaveDirPath + "/SaveSystemData");
            if (saveSystemData == null)
            {
                saveSystemData = new SaveSystemData();
                UpdateSaveSystemData();
            }
        }

        private static void UpdateSaveSystemData()
        {
            SaveFile(saveSystemData,SaveDirPath + "/SaveSystemData");
        }

        private static T LoadFile<T>(string path) where T : class
        {
            switch (MyFrameRoot.Setting.SaveSystemType)
            {
                case SaveSystemType.Binary:
                    return IOTool.LoadFile<T>(path);
                case SaveSystemType.Json:
                    return IOTool.LoadJson<T>(path);
            }
            return null;
        }
        #endregion


        #region DataManager

        //Manager直接使用即可,会自动在系统内创建
        private static Dictionary<Type,DataManager> dataManagerDic = new Dictionary<Type, DataManager>();
        
        public static M CreateDataManager<M>() where M : DataManager, new()
        {
            if (dataManagerDic.ContainsKey(typeof(M)))
            {
                throw new Exception("重复创建了一个已经存在的DataManager");
            }
            var manager = new M();
            manager.Initialize();
            dataManagerDic.Add(typeof(M), manager);
            return manager;
        }

        public static M GetDataManager<M>() where M : DataManager, new()
        {
            if (!dataManagerDic.TryGetValue(typeof(M), out var manager))
            {
                manager  = CreateDataManager<M>();
            }
            return (M)manager;
        }
        #endregion
    }
}