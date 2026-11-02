using System;

namespace MyFrame
{
    [Serializable]
    public abstract class SettingDataContainer
    {
        protected abstract void Initialize();
        public T Create<T>() where T : SettingDataContainer<T>, new()
        {
            var settingData = new T();
            settingData.Initialize();
            return settingData;
        }
    }

    [Serializable]
    public abstract class SettingDataContainer<T> : SettingDataContainer where T : SettingDataContainer<T>,new()
    {
        
    }
}
