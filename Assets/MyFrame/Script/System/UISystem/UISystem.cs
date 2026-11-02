using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MyFrame
{
    public class UISystem : MonoBehaviour
    {
        public static UISystem Instance;
        private static List<UILayout> _layouts = new ();
        private static Dictionary<string, UIWindowData> UIWindowDataDic => MyFrameRoot.WindowDataDic;
        private static List<UILayoutData> UILayoutDataList => MyFrameRoot.UILayoutDataList;
        private static readonly Dictionary<string,UIWindowBase> WindowCacheDic = new();
        [SerializeField] private RectTransform dragLayer;
        public static RectTransform DragLayer => Instance.dragLayer;


        public static void Init()
        {
            Instance = MyFrameRoot.Instance.GetComponentInChildren<UISystem>();
            if(Instance != null) Instance.InitInstance();
        }

        private void InitInstance()
        {
            _layouts.Clear();
            _layouts = GetComponentsInChildren<UILayout>().ToList();
            PreInitWindows();
        }
#if UNITY_EDITOR
        public void RefreshUISystem()
        {  
            var layoutPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MyFrame/Prefab/LayoutPrefab.prefab");
            if (layoutPrefab == null)
            {
                Debug.Log("Layout预制体没有在指定路径下");
            }
            var layoutParent = gameObject.transform.Find("Layouts");
            var childCount = layoutParent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = layoutParent.GetChild(0);
                DestroyImmediate(child.gameObject);
            }
            for (int i = 0; i < UILayoutDataList.Count; i++)
            {
                var layout = UILayoutDataList[i];
                var layoutMono =  Instantiate(layoutPrefab, layoutParent).GetComponent<UILayout>();
                layoutMono.Init(layout);
                layoutMono.gameObject.name = $"Layout{i}";
            }
        }
#endif
        #region 窗口预初始化

        private void PreInitWindows()
        {
            if (!Application.isPlaying) return;
            if (UIWindowDataDic.Count == 0) return;
            foreach (var windowDataWithKey in UIWindowDataDic)
            {
                var key =  windowDataWithKey.Key;
                var windowData = windowDataWithKey.Value;
                if (!windowData.ifCache || !windowData.ifPreInit) continue;
                if(windowData.layerNum > _layouts.Count - 1) throw new Exception("超出最大层数，无法预初始化");
                var layout = _layouts[windowData.layerNum];
                var window = ResSystem.InstantiateGameObject<UIWindowBase>(windowData.assetPath,_layouts[windowData.layerNum].root,objName: key);
                window.Init();
                window.gameObject.SetActive(false);
                window.transform.SetParent(layout.transform);
                window.transform.SetAsLastSibling();
                layout.OnWindowShow();
                WindowCacheDic[key] = window;
            }
        }

        #endregion
        
        #region 窗口显示
        public static T ShowWindow<T>(int layer = -1) where T : UIWindowBase
        {
           return ShowWindow(typeof(T).FullName, layer) as T;
        }

        public static UIWindowBase ShowWindow(Type type,int layer = -1)
        {
           return ShowWindow(type.FullName, layer);
        }
        public static UIWindowBase ShowWindow(string windowKey, int layer = -1)
        {
            if (!UIWindowDataDic.TryGetValue(windowKey, out var windowData)) throw new Exception("当前的Window数据没有被注册过,开启失败");
            layer = layer == -1 ? windowData.layerNum : layer;
            if (layer > _layouts.Count - 1) throw new Exception("超出最大层数，无法显示");
            var layout = _layouts[layer];
            if (WindowCacheDic.TryGetValue(windowKey, out var window))
            {
                if (window.UIEnabled)
                {
                    _layouts[window.CurrentLayer].OnWindowClose();
                }
            }
            else
            {
                window = ResSystem.InstantiateGameObject<UIWindowBase>(windowData.assetPath,_layouts[layer].root,objName: windowKey);
                WindowCacheDic[windowKey] = window;
                window.Init();
            }
            window.gameObject.SetActive(true);
            window.transform.SetParent(layout.transform);
            window.transform.SetAsLastSibling();
            window.ShowGeneralLogic(layer);
            layout.OnWindowShow();
            return window;
        }
        #endregion

        #region 窗口关闭
        public static bool Close<T>() where T : UIWindowBase
        {
           return Close(typeof(T));
        }
        public static bool Close(Type windowType)
        {
          return  Close(windowType.Name);
        }

        public static bool Close(string windowKey)
        {
            if (!UIWindowDataDic.TryGetValue(windowKey, out var windowData)) throw new Exception("当前的Window数据没有被注册过,无法关闭");
            if (!WindowCacheDic.TryGetValue(windowKey, out var window) || !window.UIEnabled) return false;
            var layout = _layouts[window.CurrentLayer];
            window.CloseGeneralLogic();
            layout.OnWindowClose();
            if (windowData.ifCache) return true;
            ResSystem.UnloadInstance(window.gameObject);//UnloadInstance会管理该对象的生命周期不需要再Destroy
            WindowCacheDic.Remove(windowKey);
            return true;
        }

        public static void CloseAllWindow()
        {
            var keyList = WindowCacheDic.Keys.ToList();
            foreach (var key in keyList)
            {
                Close(key);
            }
        }
        #endregion

        #region 窗口获取

        public static T GetWindow<T>() where T : UIWindowBase
        {
            return GetWindow(typeof(T)) as T;
        }

        public static UIWindowBase GetWindow(Type windowType)
        {
            return GetWindow(windowType.Name);
        }
        
        public static UIWindowBase GetWindow(string windowKey)
        {
            return WindowCacheDic.GetValueOrDefault(windowKey);
        }

        public static bool TryGetWindow<T>(out T window) where T : UIWindowBase
        {
            window = GetWindow<T>();
            return window != null;
        }
        #endregion
    }
}
