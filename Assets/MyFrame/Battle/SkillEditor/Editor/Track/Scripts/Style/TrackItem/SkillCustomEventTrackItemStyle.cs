using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SkillCustomEventTrackItemStyle : SkillTrackItemStyleBase
{
    private const string TrackItemAssetPath = "Assets/MyFrame/Battle/SkillEditor/Editor/Track/Assets/TrackItem/EventTrackItem.uxml";
    
    public void Init(SkillTrackStyleBase trackStyle)
    {
        Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TrackItemAssetPath).Instantiate().Query<Label>();
        trackStyle.AddItem(Root);
    }
    
}
