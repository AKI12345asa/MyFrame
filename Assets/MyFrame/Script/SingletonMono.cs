using UnityEngine;

namespace MyFrame
{
    public class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
    {
        public static T Instance { get; private set; }
        protected bool IsInstance;
        protected virtual void Awake()
        {
            IsInstance = Instance == null;
            if(!IsInstance)
            {
                DestroyImmediate(gameObject);
                return;
            }
            Instance = this as T;
        }
    }
}
