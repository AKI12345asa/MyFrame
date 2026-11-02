using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;

[CustomEditor(typeof(GOAPEditorWindow))]
public class GOAPInspector : Editor
{
  public static GOAPInspector Instance;
  private static VisualElement _root;
  private static GOAPItemBase _selectedItem;
  private static Dictionary<Type, GOAPActionInspectorItem> _itemDic = new();
  public static GOAPItemBase SelectedItem => _selectedItem;
  private Foldout _comparerFoldOut;
  private DropdownField _stateDropDownField;
  private CheckerRightClickMenu _rightClickMenu;
  private static GOAPAgent Agent
  {
    get
    {
      if (GOAPEditorWindow.Window == null) return null;
      return GOAPEditorWindow.Window.Agent;
    }
  }
  
  public static void SetSelectedItem(GOAPItemBase item)
  {
    if (_selectedItem != null)
    {
      _selectedItem.UnSelected();
    }
    _selectedItem = item;
    _selectedItem.Selected();
    if(Instance != null)Instance.Show();
  }
  
  public override VisualElement CreateInspectorGUI()
  {
    Instance = this;
    _root = new VisualElement();
    InitComparerFoldOut();
    _rightClickMenu = new();
    _rightClickMenu.OnMenuItemClicked += EntryClicked;
    InitStateDropDownField();
    Show();
    return _root;
  }

  private  void Show()
  {
    Clear();
    if (_selectedItem == null) return;
    switch (_selectedItem)
    {
      case GoalItem goalItem:
        DrawGoalItem(goalItem);
        break;
      case StateItem stateItem:
        DrawStateItem(stateItem);
        break;
      case GOAPActionItem actionItem:
        DrawAction(actionItem);
        break;
      case PreConditionItem preConditionItem:
        DrawPreConditionItem(preConditionItem);
        break;
      case EffectItem effectItem:
        DrawEffectItem(effectItem);
        break;
    }
  }

  public void Clear()
  {
    if (_root != null)
    {
      _root.Clear();
      _comparerFoldOut.contentContainer.Clear();
      GOAPEditorWindow.Window?.ClearOnFreshInspector();
    }
  }

  public void ResetInspector()
  {
    _selectedItem = null;
    Clear();
  }
  #region 比较器查看

  private void InitComparerFoldOut()
  {
    _comparerFoldOut = new Foldout();
  }

  private void DrawItemComparerType(GOAPStateComparer comparer)
  {
    _comparerFoldOut.contentContainer.Clear();
    switch (comparer)
    {
      case FloatStateComparer floatStateComparer:
        _comparerFoldOut.text = "Float Comparer";
        var floatNumberCompareSymbol = new DropdownField(Enum.GetNames(typeof(NumberCompareSymbol)).ToList(),(int)floatStateComparer.Symbol);
        floatNumberCompareSymbol.RegisterValueChangedCallback(evt =>
        {
          switch (evt.newValue)
          {
            case "大于":
              floatStateComparer.Symbol = NumberCompareSymbol.大于;
              break;
            case "小于":
              floatStateComparer.Symbol = NumberCompareSymbol.小于;
              break;
            case "大于等于":
              floatStateComparer.Symbol = NumberCompareSymbol.大于等于;
              break;
            case "小于等于":
              floatStateComparer.Symbol = NumberCompareSymbol.小于等于;
              break;
            case "等于":
              floatStateComparer.Symbol = NumberCompareSymbol.等于;
              break;
            case "提升即可":
              floatStateComparer.Symbol = NumberCompareSymbol.提升即可;
              break;
            case "下降即可":
              floatStateComparer.Symbol = NumberCompareSymbol.下降即可;
              break;
          }
          GOAPEditorUtility.SaveData();
        });
        var compareValueField = new FloatField("实数值");
        compareValueField.value = floatStateComparer.Value;
        compareValueField.RegisterValueChangedCallback(evt =>
        {
          if (!Mathf.Approximately(evt.newValue, floatStateComparer.Value))
          {
            floatStateComparer.Value = evt.newValue;
            GOAPEditorUtility.SaveData();
          }
        });
        _comparerFoldOut.contentContainer.Add(floatNumberCompareSymbol);
        _comparerFoldOut.contentContainer.Add(compareValueField);
        break;
      case IntStateComparer intStateComparer:
        _comparerFoldOut.text = "Int Comparer";
        var intNumberCompareSymbol = new DropdownField(Enum.GetNames(typeof(NumberCompareSymbol)).ToList(),(int)intStateComparer.Symbol);
        intNumberCompareSymbol.RegisterValueChangedCallback(evt =>
        {
          switch (evt.newValue)
          {
            case "大于":
              intStateComparer.Symbol = NumberCompareSymbol.大于;
              break;
            case "小于":
              intStateComparer.Symbol = NumberCompareSymbol.小于;
              break;
            case "大于等于":
              intStateComparer.Symbol = NumberCompareSymbol.大于等于;
              break;
            case "小于等于":
              intStateComparer.Symbol = NumberCompareSymbol.小于等于;
              break;
            case "等于":
              intStateComparer.Symbol = NumberCompareSymbol.等于;
              break;
            case "提升即可":
              intStateComparer.Symbol = NumberCompareSymbol.提升即可;
              break;
            case "下降即可":
              intStateComparer.Symbol = NumberCompareSymbol.下降即可;
              break;
          }
          GOAPEditorUtility.SaveData();
        });
        var intCompareValueField = new IntegerField("整数值");
        intCompareValueField.value = intStateComparer.Value;
        intCompareValueField.RegisterValueChangedCallback(evt =>
        {
            intStateComparer.Value = evt.newValue;
        });
        _comparerFoldOut.contentContainer.Add(intNumberCompareSymbol);
        _comparerFoldOut.contentContainer.Add(intCompareValueField);
        break;
      case BoolStateComparer boolStateComparer:
        _comparerFoldOut.text = "Bool Comparer";
        var boolValueField = new DropdownField(Enum.GetNames(typeof(BoolValue)).ToList(), (int)boolStateComparer.Value);
        boolValueField.RegisterValueChangedCallback(evt =>
        {
          switch (evt.newValue)
          {
            case "否":
              boolStateComparer.Value = BoolValue.否;
              break;
            case "是":
              boolStateComparer.Value = BoolValue.是;
              break;
          }
        });
        _comparerFoldOut.contentContainer.Add(boolValueField);
        break;
      case UnityObjectStateComparer objectStateComparer:
        _comparerFoldOut.text = "Object Comparer";
        var objectField = new ObjectField("Object");
        var objectSymbolField = new DropdownField(Enum.GetNames(typeof(UnityObjectSymbol)).ToList(), (int)objectStateComparer.Symbol);
        objectField.value = objectStateComparer.Value;
        objectField.RegisterValueChangedCallback(evt =>
        {
              objectStateComparer.Value = evt.newValue;
              GOAPEditorUtility.SaveData();
        });
        objectSymbolField.RegisterValueChangedCallback(evt =>
        {
          switch (evt.newValue)
          {
            case "是":
              objectStateComparer.Symbol = UnityObjectSymbol.是;
              break;
            case "否":
              objectStateComparer.Symbol = UnityObjectSymbol.否;
              break;
            case "不为空":
              objectStateComparer.Symbol = UnityObjectSymbol.不为空;
              break;
            case "为空":
              objectStateComparer.Symbol = UnityObjectSymbol.为空;
              break;
          }
          GOAPEditorUtility.SaveData();
        });
        _comparerFoldOut.contentContainer.Add(objectField);
        _comparerFoldOut.contentContainer.Add(objectSymbolField);
        break;
    }
  }

  private void DrawItemStateType(GOAPStateBase stateType)
  {
    switch (stateType)
    {
      case FloatState floatState:
        _comparerFoldOut.text = "Float State";
        var floatValueField = new FloatField("实数值");
        floatValueField.value = floatState.Value;
        floatValueField.RegisterValueChangedCallback(evt =>
        {
            floatState.Value = evt.newValue;
        });
        _comparerFoldOut.contentContainer.Add(floatValueField);
        GOAPEditorWindow.Window.RegisterOnFreshInspector(() => floatValueField.value = floatState.Value);
        break;
      case IntState intState:
        _comparerFoldOut.text = "Int State";
        var intValueField = new IntegerField("整数值");
        intValueField.value = intState.Value;
        intValueField.RegisterValueChangedCallback(evt =>
        {
          intState.Value = evt.newValue;
          GOAPEditorUtility.SaveData();
        });
        _comparerFoldOut.contentContainer.Add(intValueField);
        GOAPEditorWindow.Window.RegisterOnFreshInspector(() => intValueField.value = intState.Value);
        break;
      case BoolState boolState:
        _comparerFoldOut.text = "Bool State";
        var boolValueField = new Toggle("布尔值");
        boolValueField.value = boolState.Value;
        boolValueField.RegisterValueChangedCallback(evt =>
        {
          boolState.Value = evt.newValue;
          GOAPEditorUtility.SaveData();
        });
        _comparerFoldOut.contentContainer.Add(boolValueField);
        GOAPEditorWindow.Window.RegisterOnFreshInspector(() => boolValueField.value = boolState.Value);
        break;
      case UnityObjectState objectState:
        _comparerFoldOut.text = "Object State";
        var objectField = new ObjectField("Object");
        objectField.value = objectState.Value;
        objectField.RegisterValueChangedCallback(evt =>
        {
              objectState.Value = evt.newValue;
              GOAPEditorUtility.SaveData();
        });
        _comparerFoldOut.contentContainer.Add(objectField);
        GOAPEditorWindow.Window.RegisterOnFreshInspector(() => objectField.value = objectState.Value);
        break;
    }
  }
  #endregion
  
  #region 状态查看

  private void InitStateDropDownField()
  {
    _stateDropDownField = new DropdownField();
    _stateDropDownField.SetEnabled(true);
    _stateDropDownField.RegisterValueChangedCallback(OnStateDropDownFieldValueChanged);
  }

  private void RefreshStateDropDownField()
  {
    _stateDropDownField.choices.Clear();
    if(GOAPEditorUtility.Global)_stateDropDownField.choices.AddRange(GOAPEditorUtility.Global.GlobalStates.StateDic.Keys);
    _stateDropDownField.choices.AddRange(Agent.States.StateDic.Keys);
    _stateDropDownField.SetEnabled(true);
  }
  
  private void OnStateDropDownFieldValueChanged(ChangeEvent<string> evt)
  {
    switch (_selectedItem)
    {
      case GoalItem goalItem:
          goalItem.Goal.TargetState = evt.newValue;
          goalItem.Goal.CheckState();
          DrawItemComparerType(goalItem.Goal.TargetValue);
         break;
      case EffectItem effectItem:
        effectItem.Effect.StateType = evt.newValue;
        effectItem.Effect.CheckState();
        DrawItemComparerType(effectItem.Effect.StateComparer);
        effectItem.RefreshItem();
        break;
      case PreConditionItem preConditionItem:
        preConditionItem.PreCondition.StateType = evt.newValue;
        preConditionItem.PreCondition.CheckState();
        DrawItemComparerType(preConditionItem.PreCondition.StateComparer);
        preConditionItem.RefreshItem();
        break;
    }
    GOAPEditorUtility.SaveData();
  }
  
  public void SetState(GOAPStateType state)
  {
    _stateDropDownField.SetValueWithoutNotify(state);
    GOAPEditorUtility.SaveData();
  }
  #endregion
  
  #region 绘制Goal
  private TextField _goalNameField;
  private FloatField _finalPriorityField;

  private void DrawGoalItem(GoalItem goalItem)
  {
      _goalNameField = new TextField("目标名称");
      _goalNameField.value = goalItem.GoalName;
      _goalNameField.RegisterCallback<FocusInEvent>(GoalNameFieldFocusIn);
      _goalNameField.RegisterCallback<FocusOutEvent>(GoalNameFieldFocusOut);
      _root.Add(_goalNameField);
      
      SetState(goalItem.Goal.TargetState);
      RefreshStateDropDownField();
      _root.Add(_stateDropDownField);
    
      DrawItemComparerType(goalItem.Goal.TargetValue);
      _root.Add(_comparerFoldOut);

      var priorityMultiplyField = new FloatField("优先级系数");
      priorityMultiplyField.value = goalItem.Goal.PriorityMultiply;
      priorityMultiplyField.RegisterValueChangedCallback(PriorityMultiplyFieldValueChanged); 
      _root.Add(priorityMultiplyField);
      
      var runtimePriorityField = new FloatField("实时优先级");
      runtimePriorityField.value = goalItem.Goal.RuntimePriority;
      runtimePriorityField.RegisterValueChangedCallback(RuntimePriorityFieldValueChanged);
      GOAPEditorWindow.Window.RegisterOnFreshInspector(() => {runtimePriorityField.value = goalItem.Goal.RuntimePriority;});
      _root.Add(runtimePriorityField);
      
      _finalPriorityField = new FloatField("最终系数");
      _finalPriorityField.value = goalItem.Goal.Priority;
      _finalPriorityField.SetEnabled(false);
      GOAPEditorWindow.Window.RegisterOnFreshInspector(() => {_finalPriorityField.value = goalItem.Goal.Priority;});
      _root.Add(_finalPriorityField);

      var canBeInterruptedToggle = new Toggle("可否打断其他目标");
      canBeInterruptedToggle.value = goalItem.Goal.CanBeInterrupted;
      canBeInterruptedToggle.RegisterValueChangedCallback(CanBeInterruptedToggleValueChanged);
      _root.Add(canBeInterruptedToggle);
      
      var canInterruptToggle = new Toggle("可否打断");
      canInterruptToggle.value = goalItem.Goal.CanInterrupt;
      canInterruptToggle.RegisterValueChangedCallback(CanInterruptToggleValueChanged);    
      _root.Add(canInterruptToggle);
      
      CreateCheckerDropdownField(goalItem);
  }

  private DropdownField _checkerDropDownField;
  private void CreateCheckerDropdownField(GoalItem goalItem)
  {
    var defaultCheckerName = goalItem.Goal.Checker == null? "None" : goalItem.Goal.Checker?.ToString();
    _checkerDropDownField = new DropdownField();
    _checkerDropDownField.SetValueWithoutNotify(defaultCheckerName);
    _root.Add(_checkerDropDownField);
    _checkerDropDownField.RegisterCallback<MouseDownEvent>(evt =>
    {
      var dropdownRect = _checkerDropDownField.worldBound; // 窗口坐标
      var anchor = new Vector2((dropdownRect.xMax + dropdownRect.xMin) / 2, dropdownRect.yMax); // 下边缘左侧
      // SearchWindowContext 需要屏幕坐标；GUIToScreenPoint 会把窗口坐标换算成屏幕坐标
      var screenPos = GUIUtility.GUIToScreenPoint(anchor);
      SearchWindow.Open(new SearchWindowContext(screenPos), _rightClickMenu);
    });
  }

  private bool EntryClicked(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
  {
    if (searchTreeEntry.userData == null)
    {
      _checkerDropDownField.SetValueWithoutNotify("None");
      ((GoalItem)_selectedItem).Goal.Checker = null;
      return true;
    }
    var checker = Activator.CreateInstance((Type)searchTreeEntry.userData) as IGOAPGoalChecker;
    if (checker != null && _selectedItem is GoalItem goalItem)
    {
      goalItem.Goal.Checker = checker;
      if(checker.GetType().GetAttribute<GOAPCheckerAttribute>() is { } checkerAttribute)
      {
        _checkerDropDownField.SetValueWithoutNotify(checkerAttribute.CheckerName);
      }
      else
      {
        _checkerDropDownField.SetValueWithoutNotify(checker.GetType().Name);
      }
      
    }
    return true;
  }
  
  private string _oldGoalName;
  private GOAPGoals.Goal _goal;

  private void GoalNameFieldFocusIn(FocusInEvent evt)
  {
    _oldGoalName = _goalNameField.value;
    _goal = ((GoalItem)_selectedItem).Goal;
  }

  private void GoalNameFieldFocusOut(FocusOutEvent evt)
  {
    if (_oldGoalName == _goalNameField.value) return;
    if (Agent.Goals.Dic.TryAdd(_goalNameField.value, _goal))
    {
      Agent.Goals.Dic.Remove(_oldGoalName);
      GOAPEditorWindow.Window.RefreshItems();
      ResetInspector();
      SetSelectedItem(GOAPEditorWindow.Window.GoalItems.Find(item => item.GoalName == _goalNameField.value));
    }
    else
    {
      _goalNameField.value = _oldGoalName;
    }
    GOAPEditorUtility.SaveData();
  }
  
  private void PriorityMultiplyFieldValueChanged(ChangeEvent<float> evt)
  {
      var gaolItem = (GoalItem)_selectedItem;
      if (!Mathf.Approximately(evt.newValue, gaolItem.Goal.PriorityMultiply))
      {
        gaolItem.Goal.PriorityMultiply = evt.newValue;
        UpdateFinalPriority();
        GOAPEditorUtility.SaveData();
      }  
  }
  
  private void RuntimePriorityFieldValueChanged(ChangeEvent<float> evt)
  {
    var gaolItem = (GoalItem)_selectedItem;
    if (!Mathf.Approximately(evt.newValue, gaolItem.Goal.RuntimePriority))
    {
      gaolItem.Goal.RuntimePriority = evt.newValue;
      UpdateFinalPriority();
    }  
    GOAPEditorUtility.SaveData();
  }

  public void UpdateFinalPriority()
  {
    _finalPriorityField.value = ((GoalItem)_selectedItem).Goal.Priority;
    GOAPEditorUtility.SaveData();
  }
  private void CanBeInterruptedToggleValueChanged(ChangeEvent<bool> evt)
  {
    var gaolItem = (GoalItem)_selectedItem;
    gaolItem.Goal.CanBeInterrupted = evt.newValue;
    GOAPEditorUtility.SaveData();
  }
  private void CanInterruptToggleValueChanged(ChangeEvent<bool> evt)
  {
    var gaolItem = (GoalItem)_selectedItem;
    gaolItem.Goal.CanInterrupt = evt.newValue;
    GOAPEditorUtility.SaveData();
  }
  #endregion
  
  #region 绘制State
  private void DrawStateItem(StateItem stateItem)
  {
    SetState(stateItem.StateType);
    _root.Add(_stateDropDownField);
    _stateDropDownField.SetEnabled(false);
    _root.Add(_comparerFoldOut);
    DrawItemStateType(stateItem.State);
  }
  #endregion

  #region 绘制Action

  private TextField _actionNameField;

  private void InitActionItem(GOAPActionItem goapActionItem)
  {
    _actionNameField = new TextField("名称");
    _actionNameField.value = goapActionItem.Action.ActionName;
    _actionNameField.RegisterCallback<FocusInEvent>(ActionNameFieldFocusIn);
    _actionNameField.RegisterCallback<FocusOutEvent>(ActionNameFieldFocusOut);
    _root.Add(_actionNameField);
    
    var costValueField = new FloatField("代价值");
    costValueField.value = goapActionItem.Action.CostValue;
    costValueField.RegisterValueChangedCallback(evt =>
    {
      goapActionItem.Action.CostValue = evt.newValue;
      GOAPEditorUtility.SaveData();
    }); 
    _root.Add(costValueField);
    
    var effectValueField = new FloatField("效果值");
    effectValueField.value = goapActionItem.Action.EffectValue;
    effectValueField.RegisterValueChangedCallback(evt =>
    {
      goapActionItem.Action.EffectValue = evt.newValue;
      GOAPEditorUtility.SaveData();
    });
    _root.Add(effectValueField);
    
    var actionUpdateTypeField = new EnumFlagsField("事件更新节点", goapActionItem.Action.GoapActionUpdateType);
    actionUpdateTypeField.RegisterValueChangedCallback(evt =>
    {
      goapActionItem.Action.GoapActionUpdateType = (GOAPActionUpdateType)evt.newValue;
      GOAPEditorUtility.SaveData();
    });
    _root.Add(actionUpdateTypeField);
  }

  private string _oldActionName;
  private GOAPActionItem _goapActionItem;
  private void ActionNameFieldFocusIn(FocusInEvent evt)
  {
      _oldActionName = _actionNameField.value;
      _goapActionItem = (GOAPActionItem)_selectedItem;
  }
  
  private void ActionNameFieldFocusOut(FocusOutEvent evt)
  {
    if (_actionNameField.value != _oldActionName)
    {
        _goapActionItem.Action.ActionName = _actionNameField.value;
        _goapActionItem.SetItemName(_actionNameField.value);
        GOAPEditorUtility.SaveData();
    }
  }

  private void DrawAction(GOAPActionItem goapActionItem)
  {
    if (goapActionItem == null) return;
    var action = goapActionItem.Action;
    if (_itemDic.TryGetValue(action.GetType(), out var item))
    {
      item.Init(action);
      _root.Add(item.GetInspectorContent());
    }
    else
    {
      InitActionItem(goapActionItem);
    }
  }

  [InitializeOnLoadMethod]
  public static void RegisterActionInspector()
  {
    var types = typeof(GOAPActionInspectorItem).GetSubClasses();
    foreach (var type in types)
    {
      var attr = type.GetAttribute<GOAPActionInspectorAttribute>();
      if (attr == null) return;
      var inspectorItem = (GOAPActionInspectorItem)Activator.CreateInstance(type);
      _itemDic.Add(attr.InspectorType, inspectorItem);
    }
  }
  #endregion

  #region 绘制PreCondition和Effect

  public void DrawPreConditionItem(PreConditionItem preConditionItem)
  {
    SetState(preConditionItem.PreCondition.StateType);
    _root.Add(_stateDropDownField);
    RefreshStateDropDownField();
    
    DrawItemComparerType(preConditionItem.PreCondition.StateComparer);
    _root.Add(_comparerFoldOut);
  }

  public void DrawEffectItem(EffectItem effectItem)
  {
    SetState(effectItem.Effect.StateType);
    _root.Add(_stateDropDownField);
    RefreshStateDropDownField();
    
    DrawItemComparerType(effectItem.Effect.StateComparer);
    _root.Add(_comparerFoldOut);
  }
  
  #endregion
}

public class CheckerRightClickMenu:ScriptableObject,ISearchWindowProvider
{
  public delegate bool MenuItemClicked(SearchTreeEntry searchTreeEntry, SearchWindowContext context);
    
  public MenuItemClicked OnMenuItemClicked;
    
  public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
  {
    var entries = new List<SearchTreeEntry>();
    var typeList = typeof(IGOAPGoalChecker).GetAbstractSubClasses();
    entries.Add(new SearchTreeGroupEntry(new GUIContent("Checker"), 0));
    foreach (var type in typeList)
    {
      if (type.GetAttribute<GOAPCheckerAttribute>() is {} checkerAttribute)
      {
        entries = AddSubType(entries,type,checkerAttribute.CheckerName);
      }
    }
    entries.Add(new SearchTreeEntry(new GUIContent("None")){level = 1,userData = null});
    return entries;
  }

  public List<SearchTreeEntry> AddSubType(List<SearchTreeEntry> entries,Type checkerType,string groupName)
  {
    var typeList = checkerType.GetSubClasses();
    entries.Add(new SearchTreeGroupEntry(new GUIContent(groupName),1));
    foreach (var type in typeList)
    {
      string typeName = type.Name;
      if (type.GetAttribute<GOAPCheckerAttribute>() is {} checkerAttribute)
      {
         typeName = checkerAttribute.CheckerName;
      }
      entries.Add(new SearchTreeEntry(new GUIContent(typeName)){userData = type,level = 2});
    }
    return entries;
  }
    
  public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
  {
    if(OnMenuItemClicked == null) return false;
    return OnMenuItemClicked(searchTreeEntry, context);
  }
}

