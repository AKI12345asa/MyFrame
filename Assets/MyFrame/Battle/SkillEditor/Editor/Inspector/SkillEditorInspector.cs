using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

[CustomEditor(typeof(SkillEditorWindow))]
public class SkillEditorInspector : Editor
{
    public static SkillEditorInspector Instance;
    public static TrackItemBase CurrentTrackItem { get; private set; }
    private static SkillTrackBase _currentSkillTrack;
    public static void SetTrackItem(TrackItemBase trackItem,SkillTrackBase skillTrack)
    {
        if (CurrentTrackItem != null)
        {
            CurrentTrackItem.OnUnSelect();
        }
        CurrentTrackItem = trackItem;
        CurrentTrackItem.OnSelect();
        _currentSkillTrack = skillTrack;
        if (Instance != null) Instance.Show();
        
    }
    private void OnDestroy()
    {
        if (CurrentTrackItem != null)
        {
            CurrentTrackItem.OnUnSelect();
            CurrentTrackItem = null;
            _currentSkillTrack = null;
        }
    }
    private VisualElement _root;   
    
    public override VisualElement CreateInspectorGUI()
    {
        Instance = this;
        _root = new VisualElement();
        Show();
        return _root;
    }

    private SkillEventDataInspectorBase _eventDataInspector;
    public void Show()
    {
        Clean();
        if (CurrentTrackItem == null) return;
        _eventDataInspector = null;
        switch (CurrentTrackItem)
        {
            case EventTrackItem:
                _eventDataInspector = new SkillCustomEventInspector();
                break;
            case AnimationTrackItem :
                _eventDataInspector = new SkillAnimationEventInspector();
                break;
            case AudioTrackItem :
                _eventDataInspector = new SkillAudioEventInspector();
                break;
            case EffectTrackItem effectTrackItem:
               _eventDataInspector = new SkillEffectEventInspector();
                break;
            case AttackDetectionTrackItem attackTrackItem:
                _eventDataInspector = new SkillAttackDetectionEventInspector();
                break;
            case CameraEventTrackItem:
                _eventDataInspector = new CameraEventInspector();
                break;
        }
        _eventDataInspector?.Draw(_root, CurrentTrackItem, _currentSkillTrack);
        _eventDataInspector?.OnDraw();
    }

    public void Clean()
    {
        if (_root != null)
        {
           _root.Clear();
        }
    }
    

    public void SetTrackItemFrameIndex(int trackItemFrameIndex)
    {
        _eventDataInspector.ItemFrameIndex = trackItemFrameIndex;
    }
    
}
