using System;

namespace MyFrame
{
    public abstract class DataManager
    {
        public abstract void Initialize();
    }
    
    
    public abstract class DataManager<G,S>: DataManager where G : GameDataContainer<G>,new() where S : SettingDataContainer<S>,new()
    {
        protected G GameData;
        public S SettingData;
        protected G TestGameData;
        protected event Action<G> OnGameDataSave;
        protected event Action<G> OnGameDataLoaded;
        protected event Action<G> OnGameDataCreated;

        #region 事件的注册和执行

        protected void InvokeOnGameDataSave(G gameData)
        {
            OnGameDataSave?.Invoke(gameData);
        }

        public void RegisterOnGameDataSave(Action<G> onGameDataSave)
        {
            OnGameDataSave += onGameDataSave;
        }

        public void UnRegisterOnGameDataSave(Action<G> onGameDataSave)
        {
            OnGameDataSave -= onGameDataSave;
        }

        protected void InvokeOnGameDataLoaded(G gameData)
        {
            OnGameDataLoaded?.Invoke(gameData);
        }

        public void RegisterOnGameDataLoaded(Action<G> onGameDataLoaded)
        {
            OnGameDataLoaded += onGameDataLoaded;
        }

        public void RemoveOnGameDataLoaded(Action<G> onGameDataLoaded)
        {
            OnGameDataLoaded -= onGameDataLoaded;
        }
        
        protected void InvokeOnGameDataCreated(G gameData)
        {
            OnGameDataCreated?.Invoke(gameData);
        }

        public void RegisterOnGameDataCreated(Action<G> onGameDataCreated)
        {
            OnGameDataCreated += onGameDataCreated;
        }

        public void RemoveOnGameDataCreated(Action<G> onGameDataCreated)
        {
            OnGameDataCreated -= onGameDataCreated;
        }
        #endregion

        public virtual G GetCurrentGameData()
        {
            return GameData;
        }
        
        public override void Initialize()
        {
            SettingData = SaveSystem.LoadSetting<S>() ?? new S();
        }

        public virtual void CreateArchive(params object[] initParams)
        {
            GameData = GameDataContainer.Create<G>(initParams);
            OnGameDataCreated?.Invoke(GameData);
        }

        public virtual G LoadArchive(int index,bool ifUsedCache = true)
        {
            GameData = SaveSystem.LoadObject<G>(index,ifUsedCache);
            OnGameDataLoaded?.Invoke(GameData);
            return GameData;
        }
        
        public virtual void SaveGameDataIntoArchive(int index)
        {
           OnGameDataSave?.Invoke(GameData);
           SaveSystem.SaveObject(GameData,index);
        }

#if UNITY_EDITOR
        public virtual void CreateArchiveForTest()
        {
            CreateArchive();
            SaveGameDataIntoArchive(-1);
        }
        
#endif
    }
}