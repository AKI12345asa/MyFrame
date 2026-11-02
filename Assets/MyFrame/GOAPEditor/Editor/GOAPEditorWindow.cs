using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class GOAPEditorWindow : EditorWindow
{
    public static GOAPEditorWindow Window;
    private GOAPAgent _agent;
    public GOAPAgent Agent => _agent;
    private Action _onFreshInspector;

    [MenuItem("GOAP/GOAPEditorWindow")]
    public static void OpenWindow()
    {
        if (Window == null)
        {
            Window = GetWindow<GOAPEditorWindow>();
        }
    }
    
    public static void ReLoadWindow()
    {
        if (Window != null)
        {
            Window.InitWindow();
        }
    }
    
    #region InspectorRefresh

    public void RegisterOnFreshInspector(Action onFresh)
    {
        _onFreshInspector += onFresh;
    }
    
    public void UnRegisterOnFreshInspector(Action onFresh)
    {
        _onFreshInspector -= onFresh;
    }
    
    public void ClearOnFreshInspector()
    {
        _onFreshInspector = null;
    }

    public void OnInspectorUpdate()
    {
        _onFreshInspector?.Invoke();
    }
    #endregion
    public void CreateGUI()
    {
        InitWindow();
        SceneManager.activeSceneChanged += (scene1, scene2) =>
        {
            ClearAllItem();
        };
    }
    
    public VisualElement Root;
    private static Color _selectedColor = new Color(0.35f, 0.35f, 0.35f, 1);
    private static Color _normalColor = new Color(0.21f, 0.21f, 0.21f, 1);
    private VisualElement _selectedLabel;
    private ObjectField _agentField;
    private ScrollView _goalsTrack;
    private ScrollView _statesTrack;
    private ScrollView _actionsTrack;
    private VisualElement _goalsTrackContent;
    private VisualElement _statesTrackContent;
    private VisualElement _actionsTrackContent;
    private VisualElement _addWindowContent;
    private VisualElement _goalLabel;
    private VisualElement _stateLabel;
    private VisualElement _actionLabel;
    private VisualElement _effectLabel;
    private VisualElement _preConditionLabel;
    private TextField _textField;
    private DropdownField _stateDropdownField;
    private DropdownField _stateTypeDropdownField;
    private List<Type> _stateTypeList;
    private DropdownField _actionDropdownField;
    private string _selectedActionTypeName;
    private List<Type> _actionTypeList;
    private DropdownField _compareDropdownField;
    private List<Type> _comparerTypeList;
    private Button _addButton;
    private Button _saveAgentForPrefabButton;
    private EventCallback<MouseDownEvent> _clickAddWindowCallback;
    private List<GOAPItemBase> _items = new();
    public List<GoalItem> GoalItems = new();
    private ToolbarSearchField _actionSearchField;
    private ToolbarSearchField _stateSearchField;
    private string _actionSearchText = string.Empty;
    private string _stateSearchText = string.Empty;
    private ScrollView _runtimeStateTrack;
    private ScrollView _runtimePlanTrack;
    private Label _runtimeSummaryLabel;
    private VisualElement _addWindowPanel;
    private VisualElement _runtimePanel;

    private void InitWindow()
    {
        Root = rootVisualElement;
        Root.Clear();
        Window = this;
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/MyFrame/GOAPEditor/Style/WindowAsset/GOAPEditorWindow.uxml");
        visualTree.CloneTree(Root);
        _agentField = Root.Q<ObjectField>("AgentField");
        _agentField.objectType = typeof(GOAPAgent);
        _agentField.RegisterValueChangedCallback(OnAgentFieldChanged);
        _actionSearchField = Root.Q<ToolbarSearchField>("ActionSearchField");
        _stateSearchField = Root.Q<ToolbarSearchField>("StateSearchField");
        _actionSearchField?.RegisterValueChangedCallback(evt =>
        {
            _actionSearchText = evt.newValue ?? string.Empty;
            RefreshItems();
        });
        _stateSearchField?.RegisterValueChangedCallback(evt =>
        {
            _stateSearchText = evt.newValue ?? string.Empty;
            RefreshItems();
        });
        _runtimeStateTrack = Root.Q<ScrollView>("RuntimeStateTrack");
        _runtimePlanTrack = Root.Q<ScrollView>("RuntimePlanTrack");
        _runtimeSummaryLabel = Root.Q<Label>("RuntimeSummaryLabel");
        _addWindowPanel = Root.Q<VisualElement>("AddWindow");
        _runtimePanel = Root.Q<VisualElement>("RuntimePanel");
        _goalsTrack = Root.Q<ScrollView>("GoalsTrack");
        _goalsTrackContent = Root.Q<ScrollView>("GoalsTrack").Q<VisualElement>("unity-content-container");
        _statesTrack = Root.Q<ScrollView>("StatesTrack");
        _statesTrackContent = Root.Q<ScrollView>("StatesTrack").Q<VisualElement>("unity-content-container");
        _actionsTrack = Root.Q<ScrollView>("ActionsTrack");
        _actionsTrackContent = Root.Q<ScrollView>("ActionsTrack").Q<VisualElement>("unity-content-container");
        _addWindowContent = Root.Q<VisualElement>("AddWindowContent");
        _goalLabel = Root.Q<VisualElement>("GoalName");
        _goalLabel.RegisterCallback<MouseDownEvent>(GoalLabelSelected);
        _goalLabel.RegisterCallback<MouseDownEvent>(RefreshAddWindowForGoal);
        _stateLabel = Root.Q<VisualElement>("StateName");
        _stateLabel.RegisterCallback<MouseDownEvent>(StateLabelSelected);
        _stateLabel.RegisterCallback<MouseDownEvent>(RefreshAddWindowForState);
        _actionLabel = Root.Q<VisualElement>("ActionName");
        _actionLabel.RegisterCallback<MouseDownEvent>(RefreshAddWindowForAction);
        _actionLabel.RegisterCallback<MouseDownEvent>(SelectActionLabel);
        _effectLabel = Root.Q<VisualElement>("EffectName");
        _effectLabel.RegisterCallback<MouseDownEvent>(RefreshAddWindowForEffect);
        _effectLabel.RegisterCallback<MouseDownEvent>(SelectEffectLabel);
        _preConditionLabel = Root.Q<VisualElement>("PreConditionName");
        _preConditionLabel.RegisterCallback<MouseDownEvent>(SelectPreConditionLabel);
        _preConditionLabel.RegisterCallback<MouseDownEvent>(RefreshAddWindowForPreCondition);
        _agentField.SetValueWithoutNotify(null);
        var agent = GOAPWindowTool.GetAgent();
        if(agent != null) {_agentField.value = agent;}
        GOAPEditorUtility.Agent = Agent;
        EditorApplication.update -= TickRuntimePanel;
        EditorApplication.update += TickRuntimePanel;
        UpdateRuntimeAndEditorPanelsVisibility();
    }

    #region 添加窗口
    
    public  void InitAddWindow()
    {
        InitActionDropdownField();
        InitCompareDropdownField();
        InitStateDropdownField();
        InitStateTypeDropdownField();
        _addButton = new();
        _addButton.text = "Add";
        _textField = new();
        ClearAddWindow();
    }
    
    public void InitStateTypeDropdownField()
    {
        _stateTypeDropdownField = new();
        _stateTypeList = typeof(GOAPStateBase).GetSubClasses();
        foreach(var type in _stateTypeList)
        {
            _stateTypeDropdownField.choices.Add(type.Name);
        }
    }
    public void InitStateDropdownField()
    {
        _stateDropdownField = new();
        _stateDropdownField.choices.Clear();
        if(GOAPEditorUtility.Global != null)_stateDropdownField.choices.AddRange(GOAPEditorUtility.Global.GlobalStates.StateDic.Keys);
        _stateDropdownField.choices.AddRange(Agent.States.StateDic.Keys);
        _stateDropdownField.SetEnabled(true);
    }

    public void InitActionDropdownField()
    {
        _actionDropdownField = new();
        _actionTypeList = typeof(GOAPActionBase).GetSubClasses();
        _actionDropdownField.RegisterCallback<MouseDownEvent>(ClickActionDropdown);
        _rightClickMenu = CreateInstance<ActionRightClickMenu>();
        _rightClickMenu.OnMenuItemClicked += ActionCreatByMenu;
    }
    
    private ActionRightClickMenu _rightClickMenu;
    private void ClickActionDropdown(MouseDownEvent evt)
    {
        var dropdownRect = _actionDropdownField.worldBound; // 窗口坐标
        var anchor = new Vector2(dropdownRect.xMin, dropdownRect.yMax); // 下边缘左侧
        // SearchWindowContext 需要屏幕坐标；GUIToScreenPoint 会把窗口坐标换算成屏幕坐标
        var screenPos = GUIUtility.GUIToScreenPoint(anchor);
        SearchWindow.Open(new SearchWindowContext(screenPos), _rightClickMenu);
    }

    public void InitCompareDropdownField()
    {
        _compareDropdownField = new();
        _compareDropdownField.choices.Clear();
        _comparerTypeList = typeof(GOAPStateComparer).GetSubClasses();
        foreach(var type in _comparerTypeList)
        {
            _compareDropdownField.choices.Add(type.Name);
        }
        _compareDropdownField.SetEnabled(true);
    }
    public void ClearAddWindow()
    {
        _addWindowContent.Clear();
        _textField.SetValueWithoutNotify(null);
        _actionDropdownField.index = 0;
        _stateDropdownField.index = 0;
        _compareDropdownField.index = 0;
        _addButton = new();
        _addButton.text = "Add";
    }
    #endregion
    
    
    private void OnAgentFieldChanged(ChangeEvent<Object> evt)
    {
        _agent = evt.newValue as GOAPAgent;
        _items.Clear();
        if (_selectedLabel != null)
        {
            _selectedLabel.style.backgroundColor = _normalColor;
        }
        _addWindowContent.Clear();
        _goapActionItem = null;
        if (evt.newValue == null)
        {
            ClearAllItem();
            return;
        }
        GOAPConfig.RegisterValidateAction(InitAllItems);
        Agent.States.StateDic ??= new Dictionary<string, GOAPStateBase>();
        GOAPEditorUtility.Agent = Agent;
        GOAPWindowTool.CaptureAgent(_agent);
        InitAddWindow();
        RefreshItems();
        
    }


    private void InitAllItems()
    {
        InitGoals();
        InitStates();
        InitActions();
    }


    #region GoalItem
    private bool _isDraggingGoalItem = false;
    private GoalItem _targetGoalItem;
    public void InitGoals()
    {
        if (Agent.Goals == null) return;
        int index = 0;
        foreach(var item in Agent.Goals.Dic)
        {
            var gaolItem = new GoalItem();
            gaolItem.Init(item.Value,item.Key,index,OnDeleteGoal,OnItemSelected,_goalsTrackContent);
            _items.Add(gaolItem);
            GoalItems.Add(gaolItem);
            index++;
        }
        RefreshScrollView(_goalsTrack, Agent.Goals.Dic.Count);
    }
    
    private void OnDeleteGoal(GOAPItemBase item)
    {
        if (item == GOAPInspector.SelectedItem)
        {
            GOAPInspector.Instance.Clear();
        }
        Agent.Goals.Dic.Remove(((GoalItem)item).GoalName);
        RefreshItems();
        GOAPEditorUtility.SaveData();
    }

    private void RefreshAddWindowForGoal(MouseDownEvent evt)
    {
        if(Agent == null) return;
        ClearAddWindow();
        _textField.label = "Goal Name";
        _addWindowContent.Add(_textField);
        _addWindowContent.Add(_stateDropdownField);
        _addButton.clicked += AddGoal;
        _addWindowContent.Add(_addButton);
    }

    private void AddGoal()
    {
        var goal = new GOAPGoals.Goal();
        goal.TargetState = _stateDropdownField.value;
        goal.CheckState();
        if (!Agent.Goals.Dic.TryAdd(_textField.value,goal))
        {
            throw new Exception("目标名重复");
        }
        ClearAddWindow();
        RefreshItems();
        GOAPEditorUtility.SaveData();
        RefreshScrollView(_goalsTrack, Agent.Goals.Dic.Count);
    }

    private void GoalLabelSelected(MouseDownEvent evt)
    {
        if (Agent == null) return;
        if(_selectedLabel != null) _selectedLabel.style.backgroundColor = _normalColor;
        _selectedLabel = _goalLabel;
        _selectedLabel.style.backgroundColor = _selectedColor;
        _goapActionItem = null;
        RefreshItems();
    }
    #endregion

    #region StateItem

    public void InitStates()
    {
        if (Agent.States == null) return;
        int index = 0;
        foreach(var item in Agent.States.StateDic)
        {
            if (!string.IsNullOrEmpty(_stateSearchText) &&
                item.Key.IndexOf(_stateSearchText, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }
            var stateItem = new StateItem();
            stateItem.Init(item.Value,item.Key,index,OnDeleteState,OnItemSelected, _statesTrackContent);
            _items.Add(stateItem);
            index++;
        }
        RefreshScrollView(_statesTrack, Agent.States.StateDic.Count);
    }

    private void OnDeleteState(GOAPItemBase item)
    {
        if (item == GOAPInspector.SelectedItem)
        {
            GOAPInspector.Instance.Clear();
        }
        Agent.States.TryRemove(((StateItem)item).StateType);
        RefreshItems();
        GOAPEditorUtility.SaveData();
    }

    private void RefreshAddWindowForState(MouseDownEvent evt)
    {
        if(Agent == null) return;
        ClearAddWindow();
        _textField.label = "State Name";
        _addWindowContent.Add(_textField);
        _addWindowContent.Add(_stateTypeDropdownField);
        _addButton.clicked += AddState;
        _addWindowContent.Add(_addButton);
        _goapActionItem = null;
    }
    
    private void StateLabelSelected(MouseDownEvent evt)
    {
        if (Agent == null) return;
        if(_selectedLabel != null) _selectedLabel.style.backgroundColor = _normalColor;
        _selectedLabel = _stateLabel;
        _selectedLabel.style.backgroundColor = _selectedColor;
        RefreshItems();
    }
    private void AddState()
    {
        var state = Activator.CreateInstance(_stateTypeList.FirstOrDefault(type => type.Name == _stateTypeDropdownField.text));
        if (!Agent.States.TryAddState(_textField.value, (GOAPStateBase)state))
        {
            throw new Exception("状态名重复");
        }
        RefreshItems();
        ClearAddWindow();
        GOAPEditorUtility.SaveData();
        RefreshScrollView(_statesTrack, Agent.States.StateDic.Count);
    }

    #endregion
    
    
    

    #region ActionItem

    private ScrollView _preConditionTrack;
    private ScrollView _effectTrack;
    private VisualElement _preConditionParent;
    private VisualElement _effectParent;
    private List<GOAPItemBase> _preConditionAndEffectItems = new ();
    private GOAPActionItem _goapActionItem;
    private List<GOAPActionItem> _actionItems = new();
    private int _curSelectedIndex = -1;
    private bool _isDragging;
    private bool _isSwap;

    private void InitActions()
    {
        if (Agent.Actions == null) return;
        int index = 0;
        _preConditionTrack = Root.Q<ScrollView>("PreConditionTrack");
        _preConditionParent = _preConditionTrack.Q<VisualElement>("unity-content-container");
        _effectTrack = Root.Q<ScrollView>("EffectTrack");
        _effectParent = _effectTrack.Q<VisualElement>("unity-content-container");
        foreach(var action in Agent.Actions.Actions)
        {
            if (!string.IsNullOrEmpty(_actionSearchText) &&
                (action.ActionName?.IndexOf(_actionSearchText, StringComparison.OrdinalIgnoreCase) ?? -1) < 0)
            {
                continue;
            }
            var actionItem = new GOAPActionItem();
            actionItem.Init(action,index,OnDeleteAction,RefreshActionInspector, _actionsTrackContent);
            _items.Add(actionItem);
            _actionItems.Add(actionItem);
            index++;
        }
        RefreshScrollView(_actionsTrack, Agent.Actions.Actions.Count);
        _actionsTrackContent.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        _actionsTrackContent.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        _actionsTrackContent.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        _actionsTrackContent.UnregisterCallback<MouseOutEvent>(OnMouseOut);
        _actionsTrackContent.RegisterCallback<MouseDownEvent>(OnMouseDown);
        _actionsTrackContent.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        _actionsTrackContent.RegisterCallback<MouseUpEvent>(OnMouseUp);
        _actionsTrackContent.RegisterCallback<MouseOutEvent>(OnMouseOut);       
    }

    private void TickRuntimePanel()
    {
        if (this == null || Root == null) return;
        if (_runtimeSummaryLabel == null || _runtimeStateTrack == null || _runtimePlanTrack == null) return;
        UpdateRuntimeAndEditorPanelsVisibility();
        if (Agent == null)
        {
            _runtimeSummaryLabel.text = "GOAP Agent Runtime";
            _runtimeStateTrack.Clear();
            _runtimePlanTrack.Clear();
            return;
        }
        var runtime = Application.isPlaying;
        _runtimeSummaryLabel.text = runtime ? $"Playing | Plan Running: {Agent.Plan?.Running}" : "EditMode | Configure Data";
        BuildRuntimeStateView();
        BuildRuntimePlanView();
    }

    private void UpdateRuntimeAndEditorPanelsVisibility()
    {
        var isPlaying = Application.isPlaying;
        if (_addWindowPanel != null)
        {
            _addWindowPanel.style.display = isPlaying ? DisplayStyle.None : DisplayStyle.Flex;
        }
        if (_runtimePanel != null)
        {
            _runtimePanel.style.display = isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void BuildRuntimeStateView()
    {
        _runtimeStateTrack.Clear();
        if (Agent.States?.StateDic == null) return;
        foreach (var kv in Agent.States.StateDic)
        {
            var row = new Label($"{kv.Key}: {GetStateValueText(kv.Value)}");
            row.style.unityTextAlign = TextAnchor.MiddleLeft;
            row.style.height = 20;
            row.style.paddingLeft = 6;
            _runtimeStateTrack.Add(row);
        }
    }

    private string GetStateValueText(GOAPStateBase state)
    {
        if (state == null) return "null";
        var valueField = state.GetType().GetField("Value");
        return valueField == null ? state.GetType().Name : (valueField.GetValue(state)?.ToString() ?? "null");
    }

    private void BuildRuntimePlanView()
    {
        _runtimePlanTrack.Clear();
        if (!Application.isPlaying || Agent.Plan == null || Agent.Plan.StartNode == null)
        {
            _runtimePlanTrack.Add(new Label("No runtime plan"));
            return;
        }
        _runtimePlanTrack.Add(new Label($"Goal: {Agent.Plan.GoalName}"));
        AddPlanNodeRecursive(Agent.Plan.StartNode, 0, Agent.Plan.RunningNode);
    }

    private void AddPlanNodeRecursive(GOAPPlanNode node, int depth, GOAPPlanNode runningNode)
    {
        if (node == null) return;
        var prefix = new string(' ', depth * 2);
        var isRunning = node == runningNode;
        var label = new Label($"{prefix}- {(isRunning ? "[Running] " : string.Empty)}{node.Action?.ActionName}");
        label.style.height = 20;
        label.style.paddingLeft = 6;
        if (isRunning) label.style.color = new Color(0.3f, 1f, 0.3f, 1f);
        _runtimePlanTrack.Add(label);
        foreach (var child in node.Preconditions)
        {
            AddPlanNodeRecursive(child, depth + 1, runningNode);
        }
    }

    public void RefreshActionItems()
    {
        for (int i = 0; i < _actionItems.Count; i++)
        {
            _actionItems[i].SetIndex(i);
        }
    }

    #region Action的轨道托拽

    private void OnMouseDown(MouseDownEvent evt)
    {
        var index = GetItemIndexByPosition(evt.localMousePosition.y,Agent.Actions.Actions.Count);
        _curSelectedIndex = index;
        _isDragging = true;
    }
    
    private void OnMouseMove(MouseMoveEvent evt)
    {
        if (_curSelectedIndex == -1 || !_isDragging) return;
        var index = GetItemIndexByPosition(evt.localMousePosition.y,Agent.Actions.Actions.Count);
        if (index == _curSelectedIndex) return;
        (Agent.Actions.Actions[_curSelectedIndex],Agent.Actions.Actions[index]) = (Agent.Actions.Actions[index],Agent.Actions.Actions[_curSelectedIndex]);
        (_actionItems[_curSelectedIndex],_actionItems[index]) = (_actionItems[index],_actionItems[_curSelectedIndex]);
        _curSelectedIndex = index;
        _actionItems[_curSelectedIndex].Selected();
        _isSwap = true;
        RefreshActionItems();
    }

    private void OnMouseUp(MouseUpEvent evt)
    {
        if (!_isDragging) return;
        if(_isSwap)
        {
            GOAPEditorUtility.SaveData();
            _actionItems[_curSelectedIndex].UnSelected();
            _isSwap = false;
        }
        _curSelectedIndex = -1;
        _isDragging = false;
    }
    
    private void OnMouseOut(MouseOutEvent evt)
    {
        if (!_actionsTrackContent.ContainsPoint(evt.localMousePosition))
        {
            if(_isSwap)
            {
                GOAPEditorUtility.SaveData();
                _isSwap = false;
                _actionItems[_curSelectedIndex].UnSelected();
            }
            _isDragging = false;
            _curSelectedIndex = -1;
        }
    }
    #endregion
    
    public void RefreshActionInspector(GOAPItemBase item)
    {
        OnItemSelected(item);
        if (item is not GOAPActionItem actionItem) return;
        _goapActionItem = actionItem;
        var action = actionItem.Action;
        _preConditionAndEffectItems.ForEach(item =>
        {
            _items.Remove(item);
        });
        _preConditionAndEffectItems.Clear();
        _preConditionParent.Clear();
        _effectParent.Clear();
        int index = 0;
        foreach(var precondition in action.Precondition)
        {
           var preConditionItem = new PreConditionItem();
           preConditionItem.Init(precondition,index,OnDeletePrecondition, OnItemForActionSelected,_preConditionParent);
           _items.Add(preConditionItem);
           _preConditionAndEffectItems.Add(preConditionItem);
           index++;
        }
        RefreshScrollView(_preConditionTrack, action.Precondition.Count);
        index = 0;
        foreach(var effect in action.Effects)
        {
            var effectItem = new EffectItem();
            effectItem.Init(effect,index,OnDeleteEffect, OnItemForActionSelected,_effectParent);
            _items.Add(effectItem);
            _preConditionAndEffectItems.Add(effectItem);
            index++;
        }
        RefreshScrollView(_effectTrack, action.Effects.Count);
    }
    private void SelectActionLabel(MouseDownEvent evt)
    {
        if (Agent == null) return;
        if(_selectedLabel != null) _selectedLabel.style.backgroundColor = _normalColor;
        _selectedLabel = _actionLabel;
        _selectedLabel.style.backgroundColor = _selectedColor;
        _goapActionItem = null;
        RefreshItems();
    }
    private void SelectEffectLabel(MouseDownEvent evt)
    {
        if (Agent == null || _goapActionItem == null) return;
        if(_selectedLabel != null) _selectedLabel.style.backgroundColor = _normalColor;
        _selectedLabel = _effectLabel;
        _selectedLabel.style.backgroundColor = _selectedColor;
    }

    private void SelectPreConditionLabel(MouseDownEvent evt)
    {
        if (Agent == null || _goapActionItem == null) return;
        if(_selectedLabel != null) _selectedLabel.style.backgroundColor = _normalColor;
        _selectedLabel = _preConditionLabel;
        _selectedLabel.style.backgroundColor = _selectedColor;
    }
    public void OnDeleteAction(GOAPItemBase item)
    {
        if (item == GOAPInspector.SelectedItem)
        {
            GOAPInspector.Instance.Clear();
            RefreshItems();
        }
        Agent.Actions.Actions.Remove(((GOAPActionItem)item).Action);
        GOAPEditorUtility.SaveData();
    }
    
    public void OnDeletePrecondition(GOAPItemBase item)
    {
        if (item == GOAPInspector.SelectedItem)
        {
            GOAPInspector.Instance.Clear();
        }
        _goapActionItem.Action.Precondition.Remove(((PreConditionItem)item).PreCondition);
        RefreshItems();
        RefreshActionInspector(_goapActionItem);
        GOAPEditorUtility.SaveData();
    }

    public void OnDeleteEffect(GOAPItemBase item)
    {
        if (item == GOAPInspector.SelectedItem)
        {
            GOAPInspector.Instance.Clear();
        }
        _goapActionItem.Action.Effects.Remove(((EffectItem)item).Effect);
        RefreshItems();
        RefreshActionInspector(_goapActionItem);
        GOAPEditorUtility.SaveData();
    }
    
    private void RefreshAddWindowForAction(MouseDownEvent evt)
    {
        if(Agent == null) return;
        ClearAddWindow();
        _textField.label = "Action Name";
        _addWindowContent.Add(_textField);
        _addWindowContent.Add(_actionDropdownField);
        _addButton.clicked += AddAction;
        _addWindowContent.Add(_addButton);
    }

    private void AddAction()
    {
        var action = ((GOAPActionBase)Activator.CreateInstance(_actionTypeList.FirstOrDefault(type => type.Name == _selectedActionTypeName)));
        action.ActionName = _textField.value;
        action.Initialize();
        Agent.Actions.Actions.Add(action);
        RefreshItems();
        ClearAddWindow();
        GOAPEditorUtility.SaveData();
    }


    private void RefreshAddWindowForEffect(MouseDownEvent evt)
    {
        if (_goapActionItem == null) return;
        ClearAddWindow();
        _addWindowContent.Add(_stateDropdownField);
        _addButton.clicked += AddEffect;
        _addWindowContent.Add(_addButton);
    }

    private void AddEffect()
    {
        var typeAndComparer = new GOAPTypeAndComparer();
        typeAndComparer.StateType = _stateDropdownField.value;
        typeAndComparer.CheckState();
        _goapActionItem.Action.Effects.Add(typeAndComparer);
        RefreshActionInspector(_goapActionItem);
        ClearAddWindow();
        GOAPEditorUtility.SaveData();
    }

    private void RefreshAddWindowForPreCondition(MouseDownEvent evt)
    {
        if (_goapActionItem == null) return;
        ClearAddWindow();
        _addWindowContent.Add(_stateDropdownField);
        _addButton.clicked += AddPreCondition;
        _addWindowContent.Add(_addButton);
    }

    private void AddPreCondition()
    {
        var typeAndComparer = new GOAPTypeAndComparer();
        typeAndComparer.StateType = _stateDropdownField.value;
        typeAndComparer.CheckState();
        _goapActionItem.Action.Precondition.Add(typeAndComparer);
        RefreshActionInspector(_goapActionItem);
        ClearAddWindow();
        GOAPEditorUtility.SaveData();
    }

    #endregion

 
    private int GetItemIndexByPosition(float mouseY,int itemCount)
    {
        var itemIndex = Mathf.FloorToInt(mouseY / (GOAPItemBase.ItemDistance));
        itemIndex = Mathf.Clamp(itemIndex, 0, itemCount - 1);
        return itemIndex;
    }
    public void OnItemSelected(GOAPItemBase item)
    {
        _preConditionParent.Clear();
        _effectParent.Clear();
        if(_selectedLabel != null)
        {
            _selectedLabel.style.backgroundColor = _normalColor;
        }

        _goapActionItem = null;
        UpdateInspector(item);
        ClearAddWindow();
    } 
    public void OnItemForActionSelected(GOAPItemBase item)
    {
        UpdateInspector(item);
        if (_selectedLabel == null) return;
        _selectedLabel.style.backgroundColor = _normalColor;
        ClearAddWindow();
    }

    public void UpdateInspector(GOAPItemBase item)
    {
        Selection.activeObject = this;
        GOAPInspector.SetSelectedItem(item);
    }
    
    public void RefreshScrollView(ScrollView scrollView, int itemCount)
    {
        if (itemCount == 0)
        {
            scrollView.contentContainer.style.height = 300;
        }
        scrollView.contentContainer.style.height = itemCount * GOAPItemBase.ItemDistance;
    }
    public void ClearAllItem()
    {
        _goalsTrackContent?.Clear();
        _statesTrackContent?.Clear();
        _actionsTrackContent?.Clear();
        _actionItems.Clear();
        _effectParent?.Clear();
        _preConditionParent?.Clear();
        _items?.Clear();
        GoalItems?.Clear();
    }

    public void RefreshItems()
    {
        ClearAllItem();
        InitAllItems();
    }

    public bool ActionCreatByMenu(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
    {
        var type = (Type)searchTreeEntry.userData;
        _actionDropdownField.SetValueWithoutNotify(type.GetAttribute<GOAPActionAttribute>().ActionName);
        _selectedActionTypeName = type.Name;
        return true;
    }
}

public class ActionRightClickMenu:ScriptableObject,ISearchWindowProvider
{
    public delegate bool MenuItemClicked(SearchTreeEntry searchTreeEntry, SearchWindowContext context);
    
    public MenuItemClicked OnMenuItemClicked;
    
    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var entries = new List<SearchTreeEntry>();
        var typeList = typeof(GOAPActionBase).GetNextClasses();
        entries.Add(new SearchTreeGroupEntry(new GUIContent("Action"), 0));
        foreach (var type in typeList)
        {
            AddSubType(entries,type,1);
        }
        return entries;
    }

    public void AddSubType(List<SearchTreeEntry> entries,Type actionType,int level)
    {
        var entryName = GetSearchEntryName(actionType);
        if(!actionType.IsAbstract)
        {
            entries.Add(new SearchTreeEntry(new GUIContent(entryName)){level = level, userData = actionType});
            return;
        }
        entries.Add(new SearchTreeGroupEntry(new GUIContent(entryName), level));
        foreach (var type in actionType.GetNextClasses())
        {
            AddSubType(entries,type,level + 1);
        }
    }
    
    public string GetSearchEntryName(Type type)
    {
        if (type.GetAttribute<GOAPActionAttribute>() is { } attribute)
        {
            return attribute.ActionName;
        }
        return type.Name;
    }
    
    public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
    {
         if(OnMenuItemClicked == null) return false;
         return OnMenuItemClicked(searchTreeEntry, context);
    }
}