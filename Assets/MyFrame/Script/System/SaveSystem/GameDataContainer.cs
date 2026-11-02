using System;

namespace MyFrame
{
    [Serializable]
    public abstract class GameDataContainer
    {
        protected abstract void Initialize(params object[] initParams);
        public static T Create<T>(params object[] initParams) where T : GameDataContainer<T>,new()
        {
            var gameDataContainer = new T();
            gameDataContainer.Initialize(initParams);
            return gameDataContainer;
        }
    }
    [Serializable]
    public abstract class GameDataContainer<T>: GameDataContainer where T : GameDataContainer<T>
    {
    }
}