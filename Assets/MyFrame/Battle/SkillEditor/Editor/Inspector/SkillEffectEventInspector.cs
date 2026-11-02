using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class SkillEffectEventInspector : SkillEventDataInspectorBase<EffectTrackItem,EffectTrack>
{
    private IntegerField _effectDurationField;
    public override void OnDraw()
    {
         //特效预制体
        ObjectField effectAssetField = new("动画资源");
        effectAssetField.objectType = typeof(GameObject);
        effectAssetField.value = trackItem.EffectEvent.Prefab;
        effectAssetField.RegisterValueChangedCallback(EffectPrefabAssetFieldValueChanged);
        Root.Add(effectAssetField);

        Vector3Field posField = new("坐标");
        posField.value = trackItem.EffectEvent.Position;
        posField.RegisterValueChangedCallback(EffectPosFiledValueChanged);
        Root.Add(posField);   
        
        Vector3Field rotationField = new("旋转");
        rotationField.value = trackItem.EffectEvent.Rotation;
        rotationField.RegisterValueChangedCallback(EffectRotationFiledValueChanged);
        Root.Add(rotationField);  
        
        Vector3Field scaleField = new("缩放");
        scaleField.value = trackItem.EffectEvent.Scale;
        scaleField.RegisterValueChangedCallback(EffectScaleFiledValueChanged);
        Root.Add(scaleField);
        
        //自动销毁
        Toggle autoDestroyToggle = new("自动销毁");
        autoDestroyToggle.RegisterValueChangedCallback(EffectToggleValueChanged);
        Root.Add(autoDestroyToggle);
        
        //持续时间
        _effectDurationField = new("持续时间");
        _effectDurationField.value = trackItem.EffectEvent.Duration;
        _effectDurationField.RegisterCallback<FocusInEvent>(EffectDurationFieldFocusIn);
        _effectDurationField.RegisterCallback<FocusOutEvent>(EffectDurationFieldFocusOut);
        Root.Add(_effectDurationField);
        
        //时间计算按钮
        var calculateEffectButton = new Button(CalculateEffectDuration);
        calculateEffectButton.text = "重新计时";
        Root.Add(calculateEffectButton);  //时间计算按钮
        
        //引用模型Transform属性
        var applyModelTransformData = new Button(ApplyModelTransformData);
        applyModelTransformData.text = "引用模型Transform属性";
        Root.Add(applyModelTransformData);
        
        var setFrameButton = new Button(SetEffectDurationFrameButton);
        setFrameButton.text = "设置帧数到当前选中帧";
        Root.Add(setFrameButton);
    }
    
       private void ApplyModelTransformData()
    {
        var effectTrackItem = trackItem;
        effectTrackItem.ApplyModelTransformData();
        SkillEditorWindow.Instance.SaveConfig();
        SkillEditorInspector.Instance.Show();
    }

    private void CalculateEffectDuration()
    {
        var effectTrackItem = trackItem;
        var duration = effectTrackItem.EffectEvent.Prefab.GetComponentsInChildren<ParticleSystem>().Max(system => system.main.duration);
        effectTrackItem.EffectEvent.Duration = (int)(duration * SkillEditorWindow.Instance.SkillClip.FrameRate);
        _effectDurationField.value = effectTrackItem.EffectEvent.Duration;
        effectTrackItem.ResetView();
    }
    
    private void EffectPrefabAssetFieldValueChanged(ChangeEvent<Object> evt)
    {
        var effectTrackItem = trackItem;
        effectTrackItem.EffectEvent.Prefab = evt.newValue as GameObject;
        effectTrackItem.ResetView();
        CalculateEffectDuration();
        SkillEditorWindow.Instance.SaveConfig();
        SkillEditorWindow.Instance.TickSkill();
    }
    
    private void EffectPosFiledValueChanged(ChangeEvent<Vector3> evt)
    {
        var effectTrackItem = trackItem;
        effectTrackItem.EffectEvent.Position = evt.newValue;
        //TODO:预览部分的游戏物体位置
        trackItem.ResetView();
        SkillEditorWindow.Instance.SaveConfig();
        SkillEditorWindow.Instance.TickSkill();
    } 
    
    private void EffectRotationFiledValueChanged(ChangeEvent<Vector3> evt)
    {
        var effectTrackItem = trackItem;
        effectTrackItem.EffectEvent.Rotation = evt.newValue;
        trackItem.ResetView();
        SkillEditorWindow.Instance.SaveConfig();
        SkillEditorWindow.Instance.TickSkill();
    } 
    
    private void EffectScaleFiledValueChanged(ChangeEvent<Vector3> evt)
    {
        var effectTrackItem = trackItem;
        effectTrackItem.EffectEvent.Scale = evt.newValue;
        trackItem.ResetView();
        SkillEditorWindow.Instance.SaveConfig();
        SkillEditorWindow.Instance.TickSkill();

    }

    private void EffectToggleValueChanged(ChangeEvent<bool> evt)
    {
        var effectTrackItem = trackItem;
        effectTrackItem.EffectEvent.AutoDestruct = evt.newValue;
        SkillEditorWindow.Instance.SaveConfig();
    }

    private float _oldEffectDerationValue;
    private void EffectDurationFieldFocusIn(FocusInEvent evt)
    {
        _oldEffectDerationValue = _effectDurationField.value;
    }
    
    private void EffectDurationFieldFocusOut(FocusOutEvent evt)
    {
        if (Mathf.Approximately(_oldEffectDerationValue, _effectDurationField.value)) return;

        trackItem.EffectEvent.Duration = _effectDurationField.value;
        trackItem.ResetView();
        SkillEditorWindow.Instance.SaveConfig();
        SkillEditorWindow.Instance.TickSkill();
    }

    private void SetEffectDurationFrameButton()
    {
        EffectDurationFieldFocusIn(null);
        var frame =  SkillEditorWindow.Instance.CurrentSelectFrameIndex -
                     trackItem.FrameIndex; 
        _effectDurationField.value = frame > 0 ? frame : _effectDurationField.value;
        EffectDurationFieldFocusOut(null);
    }
}
