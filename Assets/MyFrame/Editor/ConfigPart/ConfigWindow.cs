using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyFrame.Editor
{
    public class ConfigWindow:TwoPaneSplitView
    {
        public new class UxmlFactory : UxmlFactory<ConfigWindow, UxmlTraits>{}
        public static ConfigWindow Instance;
        public VisualElement Root;
        public VisualElement Configs;
        public void Init()
        {
            Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/MyFrame/Editor/ConfigPart/ConfigAsset/ConfigWindow.uxml").Instantiate().Q("ConfigWindow");
            InitLeftPart();
            InitRightPart();
            Instance = this;
        }

        public void Refresh()
        {
            InitConfigs();
            RefreshRightPanel(null);
        }
        #region 左半部分
        private TreeView _configTypeTree;
        private Button _resetButton;
        private ToolbarSearchField _configsSearField;
        private List<FrameConfigBase> _totalConfigs;
        private readonly List<FrameConfigBase> _filteredConfigs = new();
        private ListView _configListView;
        private ConfigGroupItem  _currentConfigGroup;
        private string _currentSearchString;
        private void InitLeftPart()
        {
            _configTypeTree = Root.Q<TreeView>("ConfigTypeTree");
            _configsSearField = Root.Q<ToolbarSearchField>("ConfigsSearchField");
            Configs = Root.Q<VisualElement>("Configs");
            _configListView = Root.Q<ListView>("ConfigListView");
            _configListView.makeItem = () => new ConfigItem(RefreshRightPanel);
            _configListView.bindItem = BindConfigItem;
            _configListView.itemsSource = _filteredConfigs;
            _resetButton = Root.Q<Button>("ResetButton");
            InitConfigTypeTree();
            InitConfigs();
            _resetButton.clicked += () =>
            {
                _configListView.ClearSelection();
                _currentConfigGroup = null;
                RefreshConfigs();
            };  
        }

        public void InitConfigTypeTree()
        {
            var treeData = FrameWindowUtility.GenerateTreeViewData();
            _configTypeTree.SetRootItems(treeData);
            _configTypeTree.fixedItemHeight = 30;
            _configTypeTree.makeItem = () =>
            {
                var label = new Label();
                label.AddManipulator(new ContextualMenuManipulator(evt =>
                {
                    evt.menu.AppendAction("新建该类型资产", a =>
                    {
                        if(label.userData == null ||  label.userData is not ConfigGroupItem groupItem) return;
                        AddConfigPanel.Open(groupItem.ConfigTypes);
                    });
                }));
                return label;
            };
            _configTypeTree.bindItem = (element, i) =>
            {
                var groupData = _configTypeTree.GetItemDataForIndex<ConfigGroupItem>(i);
                var label = (Label)element;
                label.text = groupData.Name;
                label.userData = groupData;
            };
            _configTypeTree.selectionChanged += objs =>
            {
                foreach (var obj in objs)
                {
                    if (obj is not ConfigGroupItem groupItem) continue;
                    _currentConfigGroup = groupItem;
                    RefreshConfigs();
                }
            };
            _currentSearchString = null;
            _currentConfigGroup = null;
            _configsSearField.value = null;
            _configsSearField.RegisterValueChangedCallback(evt =>
            {
                _currentSearchString = evt.newValue;
                RefreshConfigs();
            });
        }

        private void InitConfigs()
        {
            _totalConfigs = FrameWindowUtility.FindAllConfigsInFolders();
            RefreshConfigs();
        }

        private void RefreshConfigs()
        {
            if (_totalConfigs == null) return;
            _filteredConfigs.Clear();
            _filteredConfigs.AddRange(_totalConfigs.FindAll(config =>
            {
                if (_currentConfigGroup == null)
                {
                    return CheckFilter(config, null, _currentSearchString);
                }
                else
                {
                    return _currentConfigGroup.ConfigTypes.Any(type => CheckFilter(config, type, _currentSearchString));
                }
            }));
            _configListView.RefreshItems();
            if (_filteredConfigs.Count == 0) RefreshRightPanel(null);
        }

        private void BindConfigItem(VisualElement element, int i)
        {
            if (i < 0 || i >= _filteredConfigs.Count) return;
            var configItem = (ConfigItem)element;
            configItem.Init(_filteredConfigs[i]);
        }

        
        public bool CheckFilter(FrameConfigBase config,Type type = null,string filter = null)
        {
            return (type == null || type.IsAssignableFrom(config.GetType())) && (string.IsNullOrEmpty(filter) || config.name.Contains(filter));
        }
        public sealed class ConfigItem : VisualElement
        {
            private static VisualTreeAsset ConfigItemTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/MyFrame/Editor/ConfigPart/ConfigAsset/ConfigItem.uxml");
            private static ConfigItem _curSelectedConfigItem;
            private static Color _mouseEnterColor = new Color(1, 1, 1, 0.5f);
            private static Color _selectedColor = new Color(0f,0.55f,1f,0.4f);
            private Action<FrameConfigBase> _onSelect;
            private readonly VisualElement _contentRoot;
            private readonly Label _nameLabel;
            private FrameConfigBase _config;
            public ConfigItem(Action<FrameConfigBase> selectedAction)
            {
                _onSelect = selectedAction;
                _contentRoot = ConfigItemTreeAsset.Instantiate().Q<VisualElement>("ConfigItem");
                _nameLabel = _contentRoot.Q<Label>("ConfigName");
                Add(_contentRoot);
                RegisterCallback<MouseEnterEvent>(_ =>
                {
                    if (_curSelectedConfigItem == this) return;
                    _contentRoot.style.backgroundColor = _mouseEnterColor;
                });
                RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    if (_curSelectedConfigItem == this) return;
                    _contentRoot.style.backgroundColor = Color.clear;
                });
                RegisterCallback<MouseDownEvent>(_ =>
                {
                    if(_curSelectedConfigItem != null) _curSelectedConfigItem.SetSelected(false);
                    _curSelectedConfigItem = this;
                    SetSelected(true);
                    _onSelect?.Invoke(_config);
                });
            }
            public void Init(FrameConfigBase config)
            {
                _config = config;
                _nameLabel.text = config != null ? config.name : string.Empty;
                SetSelected(false);
            }

            private void SetSelected(bool selected)
            {
                _contentRoot.style.backgroundColor = selected ? _selectedColor : Color.clear;
            }
        }
        #endregion
        
        #region 右半部分

        private VisualElement _inspectorContent;

        private void InitRightPart()
        {
          _inspectorContent = Root.Q<VisualElement>("InspectorContent");
          _inspectorContent.Clear();
        }

        private void RefreshRightPanel(FrameConfigBase config)
        {
            if (_inspectorContent == null) return;
            _inspectorContent.Clear();
            if (!config) return;
            if (FrameWindowUtility.ConfigInspectorDic.TryGetValue(config.GetType(), out var configInspector))
            {
                configInspector.Init(config);
                _inspectorContent.Add(configInspector.GetInspectorContent());
                return;
            }
            var propertyTree = PropertyTree.Create(config);
            var guiContainer = new IMGUIContainer(() =>
            {
                if (propertyTree == null) return;
                propertyTree.Draw();
            });
            _inspectorContent.Add(guiContainer);
        }

        #endregion
    }
}
