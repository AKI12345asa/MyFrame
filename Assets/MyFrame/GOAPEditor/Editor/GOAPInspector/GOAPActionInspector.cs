using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class GOAPActionInspectorAttribute : Attribute
{
    public Type InspectorType;

    public GOAPActionInspectorAttribute(Type inspectorType)
    {
        InspectorType = inspectorType;
    }
}

public abstract class GOAPActionInspectorItem
{
    public abstract void Init(GOAPActionBase actionBase);
        
    public abstract VisualElement GetInspectorContent();
}

public abstract class GOAPActionInspectorItem<T> : GOAPActionInspectorItem  where T : GOAPActionBase
{
    protected T Action;
    protected VisualElement Root;
    protected TextField ActionNameField;
    
    public override void Init(GOAPActionBase actionBase)
    {
        if(actionBase is not T action) throw new Exception($"类型匹配失败请检查{typeof(T).Name}");
        Action = action;
        Root = new VisualElement();
        ActionNameField = new TextField("名称");
        ActionNameField.value = Action.ActionName;
        ActionNameField.RegisterCallback<FocusInEvent>(ActionNameFieldFocusIn);
        ActionNameField.RegisterCallback<FocusOutEvent>(ActionNameFieldFocusOut);
        Root.Add(ActionNameField);
    
        var costValueField = new FloatField("代价值");
        costValueField.value = Action.CostValue;
        costValueField.RegisterValueChangedCallback(evt =>
        {
            Action.CostValue = evt.newValue;
            GOAPEditorUtility.SaveData();
        }); 
        Root.Add(costValueField);
    
        var effectValueField = new FloatField("效果值");
        effectValueField.value = Action.EffectValue;
        effectValueField.RegisterValueChangedCallback(evt =>
        {
            Action.EffectValue = evt.newValue;
            GOAPEditorUtility.SaveData();
        });
        Root.Add(effectValueField);
    
        var actionUpdateTypeField = new EnumFlagsField("事件更新节点", Action.GoapActionUpdateType);
        actionUpdateTypeField.RegisterValueChangedCallback(evt =>
        {
            Action.GoapActionUpdateType = (GOAPActionUpdateType)evt.newValue;
            GOAPEditorUtility.SaveData();
        });
        Root.Add(actionUpdateTypeField);
    }

    public override VisualElement GetInspectorContent()
    {
        return Root;
    }
    
    private string _oldActionName;
    private GOAPActionItem _actionItem;
    private void ActionNameFieldFocusIn(FocusInEvent evt)
    {
        _oldActionName = ActionNameField.value;
        _actionItem = (GOAPActionItem)GOAPInspector.SelectedItem;
    }
  
    private void ActionNameFieldFocusOut(FocusOutEvent evt)
    {
        if (ActionNameField.value != _oldActionName)
        {
            Action.ActionName = ActionNameField.value;
            _actionItem.SetItemName(ActionNameField.value);
            GOAPEditorUtility.SaveData();
        }
    }
}