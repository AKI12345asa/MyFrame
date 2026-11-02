using System;
using System.Collections.Generic;
using System.Linq;
using MyFrame;
using NodeSystemTree.Editor.SpeakTreeView;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeSystemTree.Editor.SpeakTreeView
{
    public class Inspector : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<Inspector, UxmlTraits>{}

        public VisualElement InspectorBar;
        
        private List<Type> _inspectorItemTypes = new();
        
        private List<InspectorItemBase> _inspectorItems = new();
        
        private static InspectorDataView InspectorDataView;
        
        public ParameterListView ParameterListView;

        public bool SwitchSignal;
        
        public ConditionListView ConditionListView = new ConditionListView();
        
        public Inspector()
        {
            Init();
        }

        void Init()
        {
            SwitchSignal = false;
            InspectorBar = new VisualElement(){name = "InspectorBar"};
            ParameterListView = new ParameterListView(){name = "ParameterListView"};
            InspectorBar.style.flexGrow = 1;
            if(NodeTreeSystemWindow.TreeData != null)
            {
                ParameterListView.Init();
            }
            _inspectorItemTypes = typeof(InspectorItemBase).GetDerivedClasses();
            ParameterListView.style.flexGrow = 1;
            Add(InspectorBar);
        }

        public void RefreshInspector()
        {
            foreach (var item in _inspectorItems)
            {
                item.UpdateInspectorGUI();
            }
        }
        
        public void UpdateInspector()
        {
            InspectorBar.Clear();
            if (!SwitchSignal)
            {
                if(Contains(ParameterListView))   
                    Remove(ParameterListView);
                Add(InspectorBar);
                Add(ConditionListView);
                ConditionListView.visible = false;
                _inspectorItems.Clear();
                NodeTreeSystemWindow.Window.WindowRoot.TreeView.selection.OfType<NodeView>()//OfType可以过滤掉不符合类型要求的元素 
                    .ForEach(n =>
                    {
                        if (n == null && n.NodeData == null) return;
                        var inspectorType = _inspectorItemTypes.FirstOrDefault(t =>
                        {
                            if (t.GetAttribute<ShowNodeAttribute>() is {} attribute &&
                                attribute.NodeType == n.NodeData.GetType())
                            {
                                return true;
                            }
                            return false;
                        });
                        if (inspectorType != null)
                        {
                            var inspectorItem = Activator.CreateInstance(inspectorType) as InspectorItemBase;
                            inspectorItem?.Init(InspectorBar, n.NodeData);
                            inspectorItem?.CreateInspectorGUI();
                            _inspectorItems.Add(inspectorItem);
                        }
                        if (n.NodeData.TryToGetConditions(out var conditions))
                        {
                            InspectorBar.style.flexGrow = 0.5f;
                            ConditionListView.style.flexGrow = 1f;
                            ConditionListView.Init(conditions);
                            ConditionListView.visible = true;
                        }
                        else
                        {
                            if (Contains(ConditionListView))
                            {
                                Remove(ConditionListView);
                            }
                            InspectorBar.style.flexGrow = 1f;
                        }
                    });
            }
            else
            {
                if(Contains(InspectorBar))
                {
                    Remove(InspectorBar);
                }
                if (Contains(ConditionListView))
                {
                    Remove(ConditionListView);
                }
                Add(ParameterListView);
            }
        }
    }
    public class ParameterListView : VisualElement
    {
        public ListView ParameterList;
        public ToolbarMenu ToolbarMenu;
        public ToolbarButton DeleteButton;
        private List<ParameterListItem> _items = new List<ParameterListItem>();
        private List<ParameterForTree> _parameterList = new();
        private int _editingIndex = -1;
        public void Init()
        {
            var ListView = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/MyFrame/NodeSystemTree/Editor/Resources/ParameterList.uxml");
            _parameterList = NodeTreeSystemWindow.TreeData.GetParameterForTree();
            ListView.CloneTree(this);
            ParameterList = this.Q<ListView>("ParameterList");
            ParameterList.itemsSource = _parameterList;
            ParameterList.bindItem = BindItem;
            ParameterList.makeItem = MakeItem;
            ParameterList.onItemsChosen += _ =>
            {
                if(ParameterList.itemsSource == null) return;
                var index = ParameterList.selectedIndex;
                _editingIndex = index;
                ParameterList.RefreshItems();
            };
            ParameterList.onSelectionChange += _ =>
            {
                if ( ParameterList.selectedItem == null || ParameterList.selectedIndex != _editingIndex)
                {
                    foreach (var item in _items)
                    {
                        item.UnSelected();
                        _editingIndex = -1;
                    }
                }
            };
            ToolbarMenu = this.Q<ToolbarMenu>("AddMenu");
            ToolbarMenu.menu.AppendAction("Bool", action =>
            {
                _parameterList.Add(new ParameterForTree(ParameterType.Bool,"New Parameter"));
                ParameterList.RefreshItems();
            });
            ToolbarMenu.menu.AppendAction("Int", action =>
            {
                _parameterList.Add(new ParameterForTree(ParameterType.Int,"New Parameter"));
                ParameterList.RefreshItems();
            });
            ToolbarMenu.menu.AppendAction("Float", action =>
            {
                _parameterList.Add(new ParameterForTree(ParameterType.Float, "New Parameter"));
                ParameterList.RefreshItems();
            });
            ToolbarMenu.menu.AppendAction("Trigger", action =>
            {
                _parameterList.Add(new ParameterForTree(ParameterType.Trigger,"New Parameter"));
                ParameterList.RefreshItems();
            });
            DeleteButton = this.Q<ToolbarButton>("DeleteButton");
            DeleteButton.clicked += () =>
            {
                if(ParameterList.selectedItem != null && ParameterList.selectedIndex >= 0)
                {
                    _parameterList.RemoveAt(ParameterList.selectedIndex);
                    _editingIndex = -1;
                    ParameterList.RefreshItems();
                }
                else
                {
                    _parameterList.RemoveAt(_parameterList.Count - 1);
                    _editingIndex = -1;
                    ParameterList.RefreshItems();
                }
                if(_editingIndex >= _parameterList.Count)
                    _editingIndex = -1;
                ParameterList.selectedIndex = -1;
                ParameterList.itemsSource = _parameterList;
            };
        
        }
        
        private void BindItem(VisualElement element, int index)
        {
            var item = element as ParameterListItem;
            item.Init(_parameterList[index]);
            item.NameLabel.BindItem(_parameterList[index]);
            if(index == _editingIndex)
                item.Selected();
            else
                item.UnSelected();
        }
        
        
        private VisualElement MakeItem()
        {
            var item = new ParameterListItem();
            item.style.flexGrow = 1;
            item.style.flexDirection = FlexDirection.Row;
            item.NameLabel.UnSelected();
            item.NameLabel.Init();
            item.NameLabel.NameField.RegisterValueChangedCallback((evt) =>
            {
                ParameterList.RefreshItems();
            });
            _items.Add(item);
            return item;
        }
        
    }
    public class ParameterListItem : VisualElement
    {
        public ParameterLabelField NameLabel;
        private VisualElement _element;
        public ParameterListItem()
        {
            NameLabel = new ParameterLabelField();
            Add(NameLabel);
            NameLabel.style.flexGrow = 1f;
            style.borderBottomWidth = 0.5f;
            style.borderBottomColor = Color.black;
        }
        public void Init(ParameterForTree parameter)
        {
            if(_element != null)
                Remove(_element);
            switch (parameter.Type)
            {
                case ParameterType.Bool:
                    var toggle = new Toggle("");
                    toggle.style.marginLeft = new Length(30, LengthUnit.Percent);
                    toggle.value = (bool)parameter.GetValue();
                    toggle.RegisterValueChangedCallback(evt =>
                    {
                        parameter.SetValue(evt.newValue);
                        EditorUtility.SetDirty(NodeTreeSystemWindow.TreeData.NodeTreeDataOwner);
                    });
                    Add(toggle);
                    _element = toggle;
                    break;
                case ParameterType.Int:
                    var intField = new IntegerField("");
                    intField.style.marginLeft = new Length(30, LengthUnit.Percent);
                    intField.style.alignSelf = Align.Center;
                    intField.style.width = 80;
                    intField.style.height = 30;
                    intField.value = (int)parameter.GetValue();
                    intField.RegisterValueChangedCallback(evt =>
                    {
                        parameter.SetValue(evt.newValue);
                        EditorUtility.SetDirty(NodeTreeSystemWindow.TreeData.NodeTreeDataOwner);
                    });
                    Add(intField);
                    _element = intField;
                    break;
                case ParameterType.Float:
                    var floatField = new FloatField("");
                    floatField.style.marginLeft = new Length(30, LengthUnit.Percent);
                    floatField.style.alignSelf = Align.Center;
                    floatField.style.width = 80;
                    floatField.style.height = 30;
                    floatField.value = (float)parameter.GetValue();
                    floatField.RegisterValueChangedCallback(evt =>
                    {
                        parameter.SetValue(evt.newValue);
                        EditorUtility.SetDirty(NodeTreeSystemWindow.TreeData.NodeTreeDataOwner);
                    });
                    Add(floatField);
                    _element = floatField;
                    break;
                case ParameterType.Trigger:
                    var radioButton = new RadioButton("");
                    radioButton.style.marginLeft = new Length(30, LengthUnit.Percent);
                    radioButton.value = (bool)parameter.GetValue();
                    radioButton.RegisterValueChangedCallback(evt =>
                    {
                        parameter.SetValue(evt.newValue);
                        EditorUtility.SetDirty(NodeTreeSystemWindow.TreeData.NodeTreeDataOwner);
                    });
                    Add(radioButton);
                    _element = radioButton;
                    break;
            }
        }
        public void Selected()
        {
            NameLabel.Selected();
        }
        
        public void UnSelected()
        {
            NameLabel.UnSelected();
        }
    }
    public class ParameterLabelField : VisualElement
    {
        public TextField NameField;
        
        public Label ParameterNameLabel;
        
        public ParameterForTree ParameterForTree;
        
        
        public void Init()
        {
            style.flexDirection = FlexDirection.Row;
            NameField = new TextField();
            NameField.style.flexGrow = 1;
            ParameterNameLabel = new ();
            ParameterNameLabel.style.flexGrow = 1;
            ParameterNameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            Add(ParameterNameLabel);
            NameField.RegisterValueChangedCallback(evt =>
            {
                if (ParameterForTree == null) return;
                ParameterForTree.Name = evt.newValue;
            });
            NameField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
                {
                    UnSelected();
                    evt.StopPropagation();
                }
            });
        }
        
        public void Selected()
        {
            if(Contains(ParameterNameLabel))
                Remove(ParameterNameLabel);
            Add(NameField);
        }

        public void UnSelected()
        {   
            if(Contains(NameField))
                Remove(NameField);
            Add(ParameterNameLabel);
        }
        
        public void BindItem(ParameterForTree info)
        {
            ParameterForTree = info;
            NameField.SetValueWithoutNotify(ParameterForTree.Name ??= "New Hierarchy");
            ParameterNameLabel.text = ParameterForTree.Name ??= "New Hierarchy";
        }
    }
}


public class ConditionListView : VisualElement
{
    public ListView ConditionList;
    
    public List<Condition> Conditions;

    public void Init(List<Condition> conditions)
    {
        if (ConditionList != null)
        {
            Remove(ConditionList);
        }
        var conditionListView = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/MyFrame/NodeSystemTree/Editor/Resources/ConditionListView.uxml");
        conditionListView.CloneTree(this);
        ConditionList = this.Q<ListView>("ConditionListView");
        ConditionList.Clear();
        Conditions = conditions;
        ConditionList.itemsSource = Conditions;
        ConditionList.bindItem = BindItem;
        ConditionList.makeItem = MakeItem;
        ConditionList.itemsAdded += indices =>
        {
            foreach (int index in indices)
            {
                var parameters = NodeTreeSystemWindow.TreeData.GetParameterForTree();
                if (parameters == null || parameters.Count == 0)
                {
                    return;
                }

                if (Conditions[index] == null)
                {
                    Conditions[index] = new Condition()
                        { ParameterID = parameters.FirstOrDefault()?.Guid };
                    ConditionList.RefreshItems();
                }
            }
        };
        ConditionList.itemsRemoved += indices =>
        {
            ConditionList.RefreshItems();
        };
    }
    
    private void BindItem(VisualElement element, int index)
    {
        if(Conditions[index] == null) return;
        var item = element as ConditionItem;
        item.Init(Conditions[index]);
        item.SearchWindowButton.SearchWindowForParameter.OnMenuClicked = MenuClickedEvt;
    }

    private bool MenuClickedEvt(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
    {
       var windowContext = (SearchWindowForParameterContext)searchTreeEntry.userData;
       var parameter = NodeTreeSystemWindow.GetParameter(windowContext.Guid);
       windowContext.SearchWindowForParameter.Condition.ParameterID = parameter.Guid;
       if (parameter.Type == ParameterType.Float || 
           parameter.Type == ParameterType.Int)
       {
           windowContext.SearchWindowForParameter.Condition.CompareValueForFloat = 0;
           windowContext.SearchWindowForParameter.Condition.CompareValueForInt = 0;
       }
       ConditionList.RefreshItems();
       return true;
    }

    private VisualElement MakeItem()
    {
        var item = new ConditionItem();
        item.style.flexGrow = 1;
        item.style.flexDirection = FlexDirection.Row;
        return item;
    }
}


public class ConditionItem : VisualElement
{
    public SearchConditionWindowButton SearchWindowButton = new();
    
    public List<VisualElement> Fields = new List<VisualElement>();

    public Condition Condition;

    public void Init(Condition condition)
    {
        Condition = condition;
        SearchWindowButton.style.width = new Length(60,LengthUnit.Percent);
        SearchWindowButton.Init(Condition);
        if(!Contains(SearchWindowButton))
        {
            Add(SearchWindowButton);
        }
        ProduceField();
    }

    private void ProduceField()
    {
        var parameter = NodeTreeSystemWindow.GetParameter(Condition.ParameterID);
        
        foreach (var field in Fields)
        {
            Remove(field);
        }
        Fields.Clear();
        
        if(parameter == null)
        {
           var Label = new Label("当前变量已被删除");
            Add(Label);
            Fields.Add(Label);
            return;
        }
        
        switch (parameter.Type)
        {
            case ParameterType.Bool:
                var boolDropdown = new DropdownField();
                boolDropdown.style.width = new Length(40,LengthUnit.Percent);
                boolDropdown.choices.Add("True");
                boolDropdown.choices.Add("False");
                if (Condition.Contrast != ContrastType.True && Condition.Contrast != ContrastType.False)
                {
                    Condition.Contrast = ContrastType.True;
                }
                boolDropdown.SetValueWithoutNotify(Condition.Contrast.ToString());
                boolDropdown.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue is "True")
                    {
                        Condition.Contrast = ContrastType.True;
                    }
                    if (evt.newValue is "False")
                    {
                        Condition.Contrast = ContrastType.False;
                    }
                });
                Fields.Add(boolDropdown);
                Add(boolDropdown);
                break;
            case ParameterType.Int:
                var intDropdown = new DropdownField();
                intDropdown.style.width = new Length(20,LengthUnit.Percent);
                intDropdown.choices.Add("Greater");
                intDropdown.choices.Add("Less");
                intDropdown.choices.Add("Equal");
                if (Condition.Contrast != ContrastType.Greater && Condition.Contrast != ContrastType.Less &&
                    Condition.Contrast != ContrastType.Equal)
                {
                    Condition.Contrast = ContrastType.Greater;
                }
                intDropdown.SetValueWithoutNotify(Condition.Contrast.ToString());
                intDropdown.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue is "Greater")
                    {
                        Condition.Contrast = ContrastType.Greater;
                    }
                    else if (evt.newValue is "Less")
                    {
                        Condition.Contrast = ContrastType.Less;
                    }
                    else if (evt.newValue is "Equal")
                    {
                        Condition.Contrast = ContrastType.Equal;
                    }
                });
                var intField = new IntegerField();
                intField.style.width = new Length(20,LengthUnit.Percent);
                intField.SetValueWithoutNotify(Condition.CompareValueForInt);
                intField.RegisterValueChangedCallback(evt =>
                {
                    Condition.CompareValueForInt = evt.newValue;
                });
                Fields.Add(intField);
                Fields.Add(intDropdown);
                Add(intDropdown);
                Add(intField);
                break;
            case ParameterType.Float:
                var floatDropdown = new DropdownField();
                floatDropdown.style.width = new Length(20,LengthUnit.Percent);
                floatDropdown.choices.Add("Greater");
                floatDropdown.choices.Add("Less");
                if (Condition.Contrast != ContrastType.Greater && Condition.Contrast != ContrastType.Less)
                {
                    Condition.Contrast = ContrastType.Greater;
                }
                floatDropdown.SetValueWithoutNotify(Condition.Contrast.ToString());
                floatDropdown.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue is "Greater")
                    {
                        Condition.Contrast = ContrastType.Greater;
                    }
                    else if (evt.newValue is "Less")
                    {
                        Condition.Contrast = ContrastType.Less;
                    }
                });
                var floatField = new FloatField();
                floatField.style.width = new Length(20,LengthUnit.Percent);
                floatField.SetValueWithoutNotify(Condition.CompareValueForFloat);
                floatField.RegisterValueChangedCallback(evt =>
                {
                    Condition.CompareValueForFloat = evt.newValue;
                });
                Fields.Add(floatField);
                Fields.Add(floatDropdown);
                Add(floatDropdown);
                Add(floatField);
                break;
            case ParameterType.Trigger:
                Condition.Contrast = ContrastType.Trigger;
                SearchWindowButton.DropdownField.style.flexGrow = 1;
                break;
        }

        foreach (var element in Fields)
        {
           element.RegisterCallback<MouseDownEvent>(evt =>
           {
               evt.StopPropagation();
           });
           element.RegisterCallback<MouseUpEvent>(evt =>
           {
               evt.StopPropagation();
           });
           element.RegisterCallback<PointerDownEvent>(evt =>
           {
               evt.StopPropagation();
           });
           element.RegisterCallback<PointerUpEvent>(evt =>
           {
               evt.StopPropagation();
           });
        }
    }
}

public class SearchConditionWindowButton : VisualElement
{
    public Condition Condition;

    public DropdownField DropdownField = new DropdownField();
    
    public SearchWindowForParameter SearchWindowForParameter;

    public void Init(Condition condition)
    {
        Condition = condition;
        var parameter = NodeTreeSystemWindow.GetParameter(Condition.ParameterID);
        if (parameter == null) return;
        DropdownField.style.flexGrow = 1;
        DropdownField.SetValueWithoutNotify(parameter.Name);
        SearchWindowForParameter = ScriptableObject.CreateInstance<SearchWindowForParameter>();
        DropdownField.RegisterCallback<MouseDownEvent>(OpenSearchWindow);
        Add(DropdownField);
    }

    private void OpenSearchWindow(MouseDownEvent evt)
    {
        var buttonRect = DropdownField.worldBound;
        var screenPos = GUIUtility.GUIToScreenPoint(new Vector2(buttonRect.x,buttonRect.y));
        var SearchWindowWidth = buttonRect.width;
         SearchWindowForParameter.Condition = Condition;
         SearchWindow.Open(new SearchWindowContext(screenPos,requestedWidth:SearchWindowWidth),SearchWindowForParameter);
    }
}

public struct SearchWindowForParameterContext
{
    public SearchWindowForParameter SearchWindowForParameter;

    public string Guid;

    public SearchWindowForParameterContext(SearchWindowForParameter searchWindowForParameter,
        string guid)
    {
        SearchWindowForParameter = searchWindowForParameter;
        Guid = guid;
    }
}

public class SearchWindowForParameter : ScriptableObject, ISearchWindowProvider
{
    public delegate bool SelectEntryDelegate(SearchTreeEntry searchTreeEntry, SearchWindowContext context);

    public SelectEntryDelegate OnMenuClicked;

    public Condition Condition;

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var entries = new List<SearchTreeEntry>();
        entries.Add(new SearchTreeGroupEntry(new GUIContent($"{NodeTreeSystemWindow.GetParameter(Condition.ParameterID).Name}")){level = 0});
        var  parameterNames = NodeTreeSystemWindow.TreeData.GetParameterForTree().Select(x => x.Name).ToList();
        foreach (var name in parameterNames)
        {
            var parameter = NodeTreeSystemWindow.TreeData.GetParameterForTree().FirstOrDefault(x => x.Name == name);
            entries.Add(new SearchTreeEntry(new GUIContent(name))
                { level = 1, userData = new SearchWindowForParameterContext(this,parameter.Guid)});

        }
        return entries;
    }

    public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
    {
        if (OnMenuClicked == null)
        {
            return false;
        }

        return OnMenuClicked.Invoke(SearchTreeEntry, context);
    }
}
