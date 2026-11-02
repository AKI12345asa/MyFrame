using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif
    namespace MyFrame
    {
        [DisallowMultipleComponent]
        [DefaultExecutionOrder(-20)]
        public class MyFrameRoot : MonoBehaviour
        {
        public static MyFrameRoot Instance;
        private static bool IsInit = false;
        public static MyFrameSetting Setting => Instance.setting;
        public static float framePerSecond => Setting.FramePerSecond;
        public static Transform FrameRootTransform => Instance.transform;
        [SerializeField] private MyFrameSetting setting;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitSystems();
                return;
            }
            Destroy(gameObject);
        }
        
        #region 初始化部分
        private static void InitSystems()
        {
            SaveSystem.Init();
            PoolSystem.Init();
            EventSystem.Init();
            UISystem.Init();
            MonoSystem.Init();
            AudioSystem.Init();
            TimeSystem.Init();
        }

        private static void InitSetting()
        {
            InitWindowDataInConfig();
        }
        #endregion
        
        #region Window部分
        public static Dictionary<string, UIWindowData> WindowDataDic
        {
            get
            {
                if (Instance == null || Instance.setting == null)
                {
                    return null;
                }

                if (Instance.setting.UIWindowDataDic == null)
                {
                    Instance.setting.UIWindowDataDic = new Dictionary<string, UIWindowData>();
                }

                return Instance.setting.UIWindowDataDic;
            }
        }
        public static List<UILayoutData> UILayoutDataList => Instance.setting.UILayoutDataList;
        private static void InitWindowDataInConfig()
        {
            var windowDataDic = WindowDataDic;
            if (windowDataDic == null)
            {
                Debug.LogWarning("MyFrameRoot: missing setting on MyFrameRoot, skip window config init.");
                return;
            }
            windowDataDic.Clear();
            FrameUtility.GetTypesByBaseTypeAndAttribute<UIWindowBase,UIWindowDataAttribute>(out var attributes);
            for (var i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];
                UIWindowData windowData = new UIWindowData(attribute.ifCache,attribute.layerNum,attribute.assetPath,attribute.ifPreInit);
                windowDataDic.TryAdd(attribute.windowKey, windowData);
            }
        }
        #endregion

        #region Pool部分

        

        #endregion
        
        private void Reset()
        {
            if(Instance != null)
            {
                Debug.LogWarning("已经存在MyFrameRoot实例了");
#if UNITY_EDITOR
                EditorApplication.delayCall += RemoveDuplicateComponent;
#endif
                return;
            }
            Instance = this;
        }
        
#region Editor部分



#if UNITY_EDITOR
        private void RemoveDuplicateComponent()
        {
            if (this == null)
            {
                return;
            }
            DestroyImmediate(this);
        }
#endif
#if UNITY_EDITOR
        private static bool s_EditorCallbacksRegistered;
        private static bool s_EditorMissingSettingWarned;

        [InitializeOnLoad]
        private static class EditorBootstrap
        {
            static EditorBootstrap()
            {
                RegisterEditorCallbacks();
            }
        }

        [InitializeOnLoadMethod]
        private static void RegisterEditorCallbacks()
        {
            if (s_EditorCallbacksRegistered)
            {
                return;
            }
            s_EditorCallbacksRegistered = true;
            EditorApplication.delayCall += TryInitFrameRootOnEditor;
            EditorApplication.hierarchyChanged += TryInitFrameRootOnEditor;//当前层级中发生修改事件就一定会触发
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            TryInitFrameRootOnEditor();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }
            EditorApplication.delayCall += TryInitFrameRootOnEditor;
            EditorApplication.hierarchyChanged -= TryInitFrameRootOnEditor;
            EditorApplication.hierarchyChanged += TryInitFrameRootOnEditor;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            TryInitFrameRootOnEditor();
        }

        private static void TryInitFrameRootOnEditor()
        {
            if (Application.isPlaying) return;
            if (Instance == null)
            {
                var roots = Resources.FindObjectsOfTypeAll<MyFrameRoot>();
                for (var i = 0; i < roots.Length; i++)
                {
                    var root = roots[i];
                    if (root == null || EditorUtility.IsPersistent(root))
                    {
                        continue;
                    }

                    Instance = root;
                    break;
                }
            }

            if (Instance == null)
            {
                return;
            }

            if (Instance.setting == null)
            {
                if (!s_EditorMissingSettingWarned)
                {
                    Debug.LogWarning("缺少RootSetting配置，无法初始化窗口数据");
                    s_EditorMissingSettingWarned = true;
                }
                return;
            }
            InitSetting();
            InitSystems();
            EditorApplication.delayCall -= TryInitFrameRootOnEditor;
            EditorApplication.hierarchyChanged -= TryInitFrameRootOnEditor;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        [Button]
        private void Refresh()
        {
            if (Instance == null)
            {
                Instance = this;
                return;
            }
            UISystem.Instance.RefreshUISystem();
        }
#endif

        #endregion
    }
}
