using System;
using System.Linq;
using MyFrame;
using MyFrame.Editor;
using Sirenix.OdinInspector.Editor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyFrame
{
    
public class FrameWindow:EditorWindow
    {
        public static FrameWindow Window;
        public static MyFrameSetting Setting => MyFrameRoot.Setting;
        private VisualElement _partRoot;
        private VisualElement _switchPartPanel;
        [MenuItem("MyFrame/Open FrameWindow")]
        public static void Open()
        {
            Window = GetWindow<FrameWindow>();
            Window.titleContent = new GUIContent("MyFrameWindow");
            Window.Init();
        }

        private void Init()
        {
            var root =  rootVisualElement;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/MyFrame/Editor/WindowAsset/FrameEditorWindow.uxml");
            visualTree.CloneTree(root);
            _switchPartPanel = root.Q<VisualElement>("SwitchPartPanel");
            _partRoot = root.Q<VisualElement>("PartRoot");
            InitSwitchPartPanel();
            InitConfigPart();
            InitSettingPart();
        }
        private void InitSwitchPartPanel()
        {
            _switchPartPanel.Q<ToolbarButton>("ConfigPartButton").clicked += AddConfigPart;
            _switchPartPanel.Q<ToolbarButton>("SettingPartButton").clicked += AddSettingPart;
        }
        
        #region Setting部分
        private ScrollView _settingScrollView;
        private ListView _layoutListView;
        private void AddSettingPart()
        {
            _partRoot.Clear();
            _partRoot.Add(_settingScrollView);
        }

        private void InitSettingPart()
        {
            _settingScrollView = new ScrollView();

            #region UI部分

            var windowDicFoldout = new Foldout();
            windowDicFoldout.text = "WindowDic(根据数据生成的窗口列表不支持修改)";
            windowDicFoldout.value = false;
            var windowDicList = new ListView();
            windowDicFoldout.Add(windowDicList);
            var valueList = Setting.UIWindowDataDic.Values.ToList();
            var keyList =  Setting.UIWindowDataDic.Keys.ToList();
            windowDicList.itemsSource = keyList;
            windowDicList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            windowDicList.showBorder = true;
            windowDicList.makeItem = () => new VisualElement();
            windowDicList.bindItem = (element, i) =>
            {
                element.SetEnabled(false);
                var windowName = keyList[i];
                element.Add(new Label(windowName));
                var valueFoldout = new Foldout();
                var value = valueList[i];
                element.Add(valueFoldout);
                valueFoldout.contentContainer.Add(new Toggle("是否需要缓存"){value = value.ifCache});
                valueFoldout.contentContainer.Add(new Toggle("是否会预初始化"){value = value.ifPreInit});
                valueFoldout.contentContainer.Add(new TextField("预制体路径"){value = value.assetPath});
                valueFoldout.contentContainer.Add(new IntegerField("UI层级"){value = value.layerNum});
            };
            _settingScrollView.Add(windowDicFoldout);   
            
            var layoutDicFoldout = new Foldout();
            layoutDicFoldout.text = "LayoutDic(可添加删除)";
            layoutDicFoldout.value = false;
            layoutDicFoldout.style.width = 800;
            _settingScrollView.Add(layoutDicFoldout);
            
            var layoutAddButton = new Button(AddLayoutItem);
            layoutAddButton.text = "Add Layout";
            layoutDicFoldout.Add(layoutAddButton);

            _layoutListView = new ListView();
            _layoutListView.itemsSource = Setting.UILayoutDataList;
            _layoutListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            _layoutListView.showBorder = true;
            _layoutListView.makeItem = () => new LayoutItemElement(RemoveLayoutItemAt);
            _layoutListView.bindItem = (element, i) =>
            {
                if (i < 0 || i >= Setting.UILayoutDataList.Count) return;
                var layoutItem = (LayoutItemElement)element;
                layoutItem.Bind(Setting.UILayoutDataList[i], i);
            };
            layoutDicFoldout.Add(_layoutListView);
            var refreshRootUILayoutButton = new Button(() =>
            {
                UISystem.Instance.RefreshUISystem();
            });
            refreshRootUILayoutButton.text = "刷新UI层";
            layoutDicFoldout.Add(refreshRootUILayoutButton);
            

            #endregion
            
            #region 存档部分

            var saveTypeDropdownField = new DropdownField("存档保存方式(修改可能丢失存档)");
            saveTypeDropdownField.style.width = 300;
            saveTypeDropdownField.choices = Enum.GetNames(typeof(SaveSystemType)).ToList();
            saveTypeDropdownField.value = Setting.SaveSystemType.ToString();
            saveTypeDropdownField.RegisterValueChangedCallback(evt =>
            {
                Setting.SaveSystemType = Enum.Parse<SaveSystemType>(evt.newValue);
            });
            _settingScrollView.Add(saveTypeDropdownField);

            #endregion

            #region 数据配置部分

            var configFolderPathFoldout = new Foldout();
            configFolderPathFoldout.text = "ConfigFolderPathList(可修改)";
            configFolderPathFoldout.value = false;
            var configFolderPathList = new ListView();
            configFolderPathList.itemsSource = Setting.ConfigFolderPathList;
            var getFolderPathButton = new Button(() =>
            {
                var addFolderPath = EditorUtility.OpenFolderPanel("选择要指定的文件夹", Application.dataPath, "");
                if (string.IsNullOrEmpty(addFolderPath)) return;
                Setting.ConfigFolderPathList.Add(FileUtil.GetProjectRelativePath(addFolderPath));
                EditorUtility.SetDirty(Setting);
                configFolderPathList.Rebuild();
            });
            getFolderPathButton.text = "添加配置文件夹";
            configFolderPathFoldout.contentContainer.Add(getFolderPathButton);
            
    
            configFolderPathFoldout.contentContainer.Add(configFolderPathList);
            configFolderPathList.makeItem = () =>
            {
                return new ConfigFolderPathItem(path =>
                {
                    Setting.ConfigFolderPathList.Remove(path);
                    EditorUtility.SetDirty(Setting);
                    configFolderPathList.Rebuild();
                });
            };
            configFolderPathList.bindItem = (element, i) =>
            {
                if (i < 0 || i >= Setting.ConfigFolderPathList.Count) return;
                var configFolderPathItem = (ConfigFolderPathItem)element;
                configFolderPathItem.Bind(Setting.ConfigFolderPathList[i]);
            };
            configFolderPathList.showBorder = true;
            
            configFolderPathFoldout.style.width = 800;
            _settingScrollView.Add(configFolderPathFoldout);
            #endregion
        }
        
       #region Layout相关

        private void AddLayoutItem()
        {
            Setting.UILayoutDataList.Add(new UILayoutData());
            EditorUtility.SetDirty(Setting);
            _layoutListView.Rebuild();
        }

        private void RemoveLayoutItemAt(int index)
        {
            if (index < 0 || index >= Setting.UILayoutDataList.Count) return;
            Setting.UILayoutDataList.RemoveAt(index);
            EditorUtility.SetDirty(Setting);
            _layoutListView.ClearSelection();
            _layoutListView.Rebuild();
        }


        #endregion
        
        #region 内部类

         private sealed class LayoutItemElement : VisualElement
        {
            private readonly Foldout _foldout;
            private readonly Toggle _enableMaskToggle;
            private readonly Toggle _blockRayCastToggle;
            private readonly ObjectField _maskSpriteField;
            private readonly ColorField _maskColorField;
            private readonly Button _deleteButton;
            private readonly Action<int> _deleteAction;

            private UILayoutData _data;
            private int _index;
            private bool _isBinding;

            public LayoutItemElement(Action<int> deleteAction)
            {
                _deleteAction = deleteAction;

                _foldout = new Foldout { value = false };
                Add(_foldout);
                
                _enableMaskToggle = new Toggle("启用Mask");
                _enableMaskToggle.RegisterValueChangedCallback(evt =>
                {
                    if (_isBinding || _data == null) return;
                    _data.enableMask = evt.newValue;
                    EditorUtility.SetDirty(Setting);
                });
                _foldout.contentContainer.Add(_enableMaskToggle);

                _blockRayCastToggle = new Toggle("阻挡点击");
                _blockRayCastToggle.RegisterValueChangedCallback(evt =>
                {
                    if (_isBinding || _data == null) return;
                    _data.ifBlockRayCast = evt.newValue;
                    EditorUtility.SetDirty(Setting);
                });
                _foldout.contentContainer.Add(_blockRayCastToggle);

                _maskSpriteField = new ObjectField("Mask Sprite");
                _maskSpriteField.objectType = typeof(Sprite);
                _maskSpriteField.allowSceneObjects = false;
                _maskSpriteField.RegisterValueChangedCallback(evt =>
                {
                    if (_isBinding || _data == null) return;
                    _data.maskSprite = evt.newValue as Sprite;
                    EditorUtility.SetDirty(Setting);
                });
                _foldout.contentContainer.Add(_maskSpriteField);

                _maskColorField = new ColorField("Mask Color");
                _maskColorField.RegisterValueChangedCallback(evt =>
                {
                    if (_isBinding || _data == null) return;
                    _data.maskColor = evt.newValue;
                    EditorUtility.SetDirty(Setting);
                });
                _foldout.contentContainer.Add(_maskColorField);
                _deleteButton = new Button(() => _deleteAction?.Invoke(_index));
                _deleteButton.text = "Delete";
                _foldout.contentContainer.Add(_deleteButton);
            }

            public void Bind(UILayoutData data, int index)
            {
                _data = data;
                _index = index;
                _isBinding = true;
                _foldout.text = $"Layout {index}";
                _enableMaskToggle.SetValueWithoutNotify(data.enableMask);
                _blockRayCastToggle.SetValueWithoutNotify(data.ifBlockRayCast);
                _maskSpriteField.SetValueWithoutNotify(data.maskSprite);
                _maskColorField.SetValueWithoutNotify(data.maskColor);
                _isBinding = false;
            }
        }
        
        private sealed class ConfigFolderPathItem : VisualElement
        {
            private  string _folderPath;
            private Action<string> _removePathAction;

            public ConfigFolderPathItem(Action<string> removePathAction)
            { 
                _removePathAction = removePathAction;
            }

            public void Bind(string folderPath)
            {
                Clear();
                _folderPath = folderPath;
                style.flexDirection = FlexDirection.Row;
                var pathField = new TextField { value = folderPath };
                pathField.SetEnabled(false);
                pathField.style.flexGrow = 1;
                Add(pathField);
                var deleteButton = new Button(() => _removePathAction?.Invoke(_folderPath));
                deleteButton.text = "X";
                deleteButton.style.width = 25;
                Add(deleteButton);
            }
        }

        #endregion
        #endregion
        
        #region Config部分
        private ConfigWindow _configWindow;
        private void InitConfigPart()
        {
            _configWindow = new ConfigWindow();
            _configWindow.Init();
        }

        private void AddConfigPart()
        {
            _partRoot.Clear();
            _configWindow.Refresh();
            _partRoot.Add(_configWindow.Root);
        }
        #endregion
    }
}
