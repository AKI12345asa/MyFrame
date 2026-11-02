using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

public class SkillCustomEventInspector : SkillEventDataInspectorBase<EventTrackItem, EventTrack>
{
    private List<string> _eventTypeChoiceList;
    public override void OnDraw()
    {
        //类型枚举
        _eventTypeChoiceList = new List<string>(Enum.GetNames(typeof(SkillEventType)));
        var eventTypeDropDownField = new DropdownField("事件类型",_eventTypeChoiceList,(int)trackItem.CustomEvent.EventType);
        eventTypeDropDownField.RegisterValueChangedCallback(OnEventTypeDropDownFieldValue);
        Root.Add(eventTypeDropDownField);
        //名称
        if (trackItem.CustomEvent.EventType == SkillEventType.Custom)
        {
            var nameField = new TextField("事件名称");
            nameField.value = trackItem.CustomEvent.CustomEventName;
            nameField.RegisterValueChangedCallback(OnEventNameFieldValueChanged);
            Root.Add(nameField);
        }
        
        //参数
        var intArgField = new IntegerField("Int参数");
        intArgField.value = trackItem.CustomEvent.IntArg;
        intArgField.RegisterValueChangedCallback(OnEventIntegerArgFieldValueChanged);
        Root.Add(intArgField);
        
        var floatArgField = new FloatField("Float参数");
        floatArgField.value = trackItem.CustomEvent.FloatArg;
        floatArgField.RegisterValueChangedCallback(OnEventFloatArgFieldValueChanged);
        Root.Add(floatArgField);
        
        var stringArgField = new TextField("String参数");
        stringArgField.value = trackItem.CustomEvent.StringArg;
        stringArgField.RegisterValueChangedCallback(OnEventStringArgFieldValueChanged);
        Root.Add(stringArgField);

        var objectArgField = new ObjectField("Object参数");
        objectArgField.objectType = typeof(Object);
        objectArgField.value = trackItem.CustomEvent.ObjectArg;
        objectArgField.RegisterValueChangedCallback(OnEventObjectArgFieldValueChanged);
        Root.Add(objectArgField);
        
        //删除
        Button deleteButton = new(DeleteEventTrackItemButtonClicked);
        deleteButton.text = "删除";
        deleteButton.style.backgroundColor = new Color(1, 0, 0, 0.5f);
       Root.Add(deleteButton);

    }
    
    private void OnEventTypeDropDownFieldValue(ChangeEvent<string> evt)
    {
        var eventTrackItem = trackItem;
        eventTrackItem.CustomEvent.EventType = (SkillEventType)_eventTypeChoiceList.IndexOf(evt.newValue);
        if (eventTrackItem.CustomEvent.EventType != SkillEventType.Custom)
        {
            eventTrackItem.CustomEvent.CustomEventName = null;
        }
        SkillEditorInspector.Instance.Show();
    }
    
    private void OnEventIntegerArgFieldValueChanged(ChangeEvent<int> evt)
    {
        trackItem.CustomEvent.IntArg = evt.newValue;
    }

    private void OnEventFloatArgFieldValueChanged(ChangeEvent<float> evt)
    {
       trackItem.CustomEvent.FloatArg = evt.newValue;
    }

    private void OnEventStringArgFieldValueChanged(ChangeEvent<string> evt)
    {
        trackItem.CustomEvent.StringArg = evt.newValue;
    }

    private void OnEventObjectArgFieldValueChanged(ChangeEvent<UnityEngine.Object> evt)
    {
        trackItem.CustomEvent.ObjectArg = evt.newValue;
    }

    private void OnEventNameFieldValueChanged(ChangeEvent<string> evt)
    {
        trackItem.CustomEvent.CustomEventName = evt.newValue;
    }

    private void DeleteEventTrackItemButtonClicked()
    {
        track.DeleteTrackItem(ItemFrameIndex);
        SkillEditorWindow.Instance.SaveConfig();
        Selection.activeObject = null;
    }

}
