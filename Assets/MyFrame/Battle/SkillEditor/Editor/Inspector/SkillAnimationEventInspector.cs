using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class SkillAnimationEventInspector : SkillEventDataInspectorBase<AnimationTrackItem,AnimationTrack>
{
    private Label _clipFrameLabel;
    private Label _isLoopLabel;
    private IntegerField _durationField;
    private FloatField _transitionTimeField;
    private FloatField _animationSpeedField;
    private Toggle _rootMotionToggle;
    private Toggle _mainWeaponOnLeftToggle;

    public override void OnDraw()
    {
       var animationEvent = trackItem.AnimationEvent;
        //动画资源
        ObjectField animationClipAssetField = new("动画资源");
        animationClipAssetField.objectType = typeof(AnimationClip);
        animationClipAssetField.value = animationEvent.AnimationClip;
        animationClipAssetField.RegisterValueChangedCallback(AnimationClipAssetFieldValueChanged);
        Root.Add(animationClipAssetField);

        _animationSpeedField = new FloatField("动画播放速度");
        _animationSpeedField.SetValueWithoutNotify(animationEvent.AnimationSpeed);
        _animationSpeedField.RegisterValueChangedCallback(AnimationSpeedFieldValueChanged);
        Root.Add(_animationSpeedField);
        
        //根运动
        _rootMotionToggle = new Toggle("应用根运动");
        _rootMotionToggle.value = animationEvent.ApplyRootMotion;
        _rootMotionToggle.RegisterValueChangedCallback(RootMotionToggleValueChanged);
        Root.Add(_rootMotionToggle);
        
        //主武器左右手
        _mainWeaponOnLeftToggle = new Toggle("主武器是否在左手位置");
        _mainWeaponOnLeftToggle.value = animationEvent.MainWeaponOnLeftHand;
        _mainWeaponOnLeftToggle.RegisterValueChangedCallback(MainWeaponToggleValueChanged);
        Root.Add(_mainWeaponOnLeftToggle);
        
        //轨道长度
        _durationField = new("轨道长度");
        _durationField.SetValueWithoutNotify(animationEvent.DurationFrame);
        _durationField.RegisterCallback<FocusInEvent>(DurationFieldFocusIn);
        _durationField.RegisterCallback<FocusOutEvent>(DurationFieldFocusOut);
        /*_durationField.RegisterValueChangedCallback(DurationFieldValueChanged);*/
        Root.Add(_durationField);
        
        //过渡时间
        FloatField transitionTimeField = new("过渡时间");
        transitionTimeField.SetValueWithoutNotify(animationEvent.TransitionTime);
        /*transitionTimeField.RegisterValueChangedCallback(TransitionTimeFieldValueChanged);*/
        _transitionTimeField = transitionTimeField;
        _transitionTimeField.RegisterCallback<FocusInEvent>(TransitionFieldFocusIn);
        _transitionTimeField.RegisterCallback<FocusOutEvent>(TransitionFieldFocusOut);
        Root.Add(transitionTimeField);
        
        //动画相关的信息
        var clipFrameCount = (int)(animationEvent.AnimationClip.frameRate * animationEvent.AnimationClip.length);
        _clipFrameLabel = new Label($"动画资源长度:{clipFrameCount}");
        Root.Add(_clipFrameLabel);
        _isLoopLabel = new Label($"是否循环:{animationEvent.AnimationClip.isLooping}");
        Root.Add(_isLoopLabel);

        Button deleteButton = new(AnimationDeleteButtonClicked);
        deleteButton.text = "删除";
        deleteButton.style.backgroundColor = new Color(1, 0, 0, 0.5f);
        Root.Add(deleteButton);
        
        //设置持续帧到选中帧
        var setFrameButton = new Button(SetAnimationDurationFrameButtonClick);
        setFrameButton.text = "设置帧数到当前选中帧";
        Root.Add(setFrameButton);
    }

    private void AnimationClipAssetFieldValueChanged(ChangeEvent<Object> evt)
    {
        if (evt.newValue is not AnimationClip clip) return;
        //修改自身显示效果
        _clipFrameLabel.text = "动画资源长度:" + (int)(clip.length * clip.frameRate);
        _isLoopLabel.text = "是否循环" + clip.isLooping;
        //保存到配置
        trackItem.AnimationEvent.AnimationClip = clip;
        SkillEditorWindow.Instance.SaveConfig();
        trackItem.ResetView();
        SkillEditorWindow.Instance.TickSkill();
    }

    private void RootMotionToggleValueChanged(ChangeEvent<bool> evt)
    {
        trackItem.AnimationEvent.ApplyRootMotion = evt.newValue;
        SkillEditorWindow.Instance.SaveConfig();
        SkillEditorWindow.Instance.ResetTrack();
        SkillEditorWindow.Instance.TickSkill();
    }
    
    private void MainWeaponToggleValueChanged(ChangeEvent<bool> evt)
    {
       trackItem.AnimationEvent.MainWeaponOnLeftHand = evt.newValue;
        SkillEditorWindow.Instance.SaveConfig();
        SkillEditorWindow.Instance.TickSkill();
    }

    private void AnimationSpeedFieldValueChanged(ChangeEvent<float> evt)
    {
        var speed = Mathf.Max(0.01f, evt.newValue);
        _animationSpeedField.SetValueWithoutNotify(speed);
        trackItem.AnimationEvent.AnimationSpeed = speed;
        SkillEditorWindow.Instance.SaveConfig();
        trackItem.ResetView();
        SkillEditorWindow.Instance.TickSkill();
    }
    
    private int _oldDurationValue;
    private void DurationFieldFocusIn(FocusInEvent evt)
    {
        _oldDurationValue = _durationField.value;
    }

    private void DurationFieldFocusOut(FocusOutEvent evt)
    {
        if (_durationField.value != _oldDurationValue)
        {
            var newEndFrameIndex = ItemFrameIndex + _durationField.value;
            if (trackItem != null&& track.CheckFrameIndexOnDrag(newEndFrameIndex,ItemFrameIndex)&& track.CheckFrameIndexOnDrag(ItemFrameIndex,newEndFrameIndex,ItemFrameIndex))
            {
                //修改数据,刷新视图
                trackItem.AnimationEvent.DurationFrame = _durationField.value;
                trackItem.CheckFrameCount();
                SkillEditorWindow.Instance.SaveConfig();
                trackItem.ResetView();
            }
            else
            {
                _durationField.value = _oldDurationValue;
            }
            SkillEditorWindow.Instance.TickSkill();
        }
    }

    private void SetAnimationDurationFrameButtonClick()
    {
        DurationFieldFocusIn(null);
        var frame = SkillEditorWindow.Instance.CurrentSelectFrameIndex -
                    trackItem.FrameIndex;
        _durationField.value = frame > 0 ? frame : _durationField.value;
        DurationFieldFocusOut(null);
    }
    
    private int _oldTransitionValue;
    private void TransitionFieldFocusIn(FocusInEvent evt)
    {
        _oldTransitionValue = _durationField.value;
    }

    private void TransitionFieldFocusOut(FocusOutEvent evt)
    {
        if (!Mathf.Approximately(_transitionTimeField.value, _oldTransitionValue))
        {
            if(Mathf.Approximately(_oldTransitionValue, _transitionTimeField.value)) return;
            SkillEditorWindow.Instance.SkillClip.SkillAnimationData.FrameData[ItemFrameIndex].TransitionTime = _transitionTimeField.value;
            SkillEditorWindow.Instance.SaveConfig();
        }
    }

    public void AnimationDeleteButtonClicked()
    {
        track.DeleteTrackItem(ItemFrameIndex);//函数已提供保存和刷新逻辑
        Selection.activeObject = null;
    }
    
    
}
