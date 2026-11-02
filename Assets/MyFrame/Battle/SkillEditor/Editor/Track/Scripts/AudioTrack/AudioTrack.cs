using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class AudioTrack : SkillTrackBase
{
    private SkillMutilineTrackStyle _trackStyle;
    
    public SkillAudioData AudioData => SkillEditorWindow.Instance.SkillClip.SkillAudioData;

    private List<AudioTrackItem> _trackItems = new();

    public override void Init(VisualElement menuParent, VisualElement trackParent, float frameWidth)
    {
        base.Init(menuParent, trackParent, frameWidth);
        _trackStyle = new  SkillMutilineTrackStyle();
        _trackStyle.Init(menuParent, trackParent, "音效配置",AddChildTrack,CheckDeleteChildTrack,SwapChildTrack,UpdateChildTrackName);
        //TODO:根据音效数据初始化音效子轨道
       AudioData.FrameData.ForEach(CreateItem);
    }

       
    
    public override void ResetView(float frameWidth)
    {
        base.ResetView(frameWidth);
        _trackItems.ForEach(item => item.Destroy());
        _trackItems.Clear();
        if (SkillEditorWindow.Instance.SkillClip == null) return;
        AudioData.FrameData.ForEach(CreateItem);
    }

    private void CreateItem(SkillAudioEvent audioEvent)
    {
        var item = new AudioTrackItem();
        item.Init(this,FrameUnitWidth,audioEvent,_trackStyle.AddChildTrack());
        item.SetTrackName(audioEvent.TrackName);
        _trackItems.Add(item);
    }

    private void SwapChildTrack(int index1, int index2)
    {
        var data1 = AudioData.FrameData[index1];
        var data2 = AudioData.FrameData[index2];
        
        AudioData.FrameData[index1] = data2;
        AudioData.FrameData[index2] = data1;
        
    }
    
    public override void Destroy()
    {
        _trackStyle.Destroy();
    }
    
    
    private void AddChildTrack()
    {
        var skillAudioEvent = new SkillAudioEvent();
        AudioData.FrameData.Add(skillAudioEvent);
        CreateItem(skillAudioEvent);
        SkillEditorWindow.Instance.SaveConfig();
    }

    private bool CheckDeleteChildTrack(int index)
    {
        if(index < 0 || index >= AudioData.FrameData.Count) return false;
        if (AudioData.FrameData[index] == null) return false;
        AudioData.FrameData.RemoveAt(index);
        _trackItems.RemoveAt(index);
        SkillEditorWindow.Instance.SaveConfig();
        return true;
    }
    
    private void UpdateChildTrackName(SkillMutilineTrackStyle.ChildTrack childTrack, string newName)
    {
        AudioData.FrameData[childTrack.GetIndex()].TrackName = newName;
        SkillEditorWindow.Instance.SaveConfig();
    }

    public override void OnPlay(int startFrameIndex)
    {
        foreach (var audioEvent in AudioData.FrameData)
        {
            if (audioEvent.AudioClip == null ) continue;

            var audioFrameCount = (int)(audioEvent.AudioClip.length * SkillEditorWindow.Instance.SkillClip.FrameRate);
            var audioClipLastFrameIndex = audioFrameCount + audioEvent.FrameIndex;
            //代表选中轴在轨道中间部分
            if (audioEvent.FrameIndex < startFrameIndex && audioClipLastFrameIndex > startFrameIndex)
            {
                var offset = startFrameIndex - audioEvent.FrameIndex;
                var rate = (float)offset / audioFrameCount;
                EditorAudioUtility.PlayAudio(audioEvent.AudioClip,rate);
            }
            else if (audioEvent.FrameIndex == startFrameIndex)
            {
                EditorAudioUtility.PlayAudio(audioEvent.AudioClip,0);
            }
        }
    }

    public override void TickView(int frameIndex)
    {
        if (SkillEditorWindow.Instance.IsPlaying)
        {
            foreach (var audioEvent in AudioData.FrameData)
            {
                if (audioEvent.AudioClip != null && audioEvent.FrameIndex == frameIndex)
                {
                    //播放音效从头播放
                    EditorAudioUtility.PlayAudio(audioEvent.AudioClip,0);
                }
            }
        }
    }

    public override void OnStop()
    {
        base.OnStop();
    }
}
