using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class SkillAudioEventInspector : SkillEventDataInspectorBase<AudioTrackItem,AudioTrack>
{
    private FloatField _volumeField;
    public override void OnDraw()
    {
        //动画资源
        ObjectField animationClipAssetField = new("动画资源");
        animationClipAssetField.objectType = typeof(AudioClip);
        animationClipAssetField.value = trackItem.AudioEvent.AudioClip;
        animationClipAssetField.RegisterValueChangedCallback(AudioClipAssetFieldValueChanged);
        Root.Add(animationClipAssetField);
        
        //音量
        _volumeField = new("播放音量");
        _volumeField.value = trackItem.AudioEvent.Volume;
        _volumeField.RegisterCallback<FocusInEvent>(VolumeFieldFocusIn);
        _volumeField.RegisterCallback<FocusOutEvent>(VolumeFieldFocusOut);
         Root.Add(_volumeField);
    }
    
    private void AudioClipAssetFieldValueChanged(ChangeEvent<Object> evt)
    {
        var audioClip = evt.newValue as AudioClip;
        //保存到配置中
        trackItem.AudioEvent.AudioClip = audioClip;
        trackItem.ResetView();
    }

    
    private float _oldVolumeValue;
    
    private void VolumeFieldFocusIn(FocusInEvent evt)
    {
        _oldVolumeValue = _volumeField.value;
    }

    private void VolumeFieldFocusOut(FocusOutEvent evt)
    {
        if (!Mathf.Approximately(_volumeField.value, _oldVolumeValue))
        {
            //修改数据,刷新视图
            trackItem.AudioEvent.Volume = _volumeField.value;
            trackItem.CheckFrameCount();
            SkillEditorWindow.Instance.SaveConfig();
            trackItem.ResetView();
            
        }
    }
}
