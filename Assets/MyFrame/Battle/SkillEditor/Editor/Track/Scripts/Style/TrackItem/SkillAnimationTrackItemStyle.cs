using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SkillAnimationTrackItemStyle : SkillTrackItemStyleBase
{
    private const string TrackItemAssetPath = "Assets/MyFrame/Battle/SkillEditor/Editor/Track/Assets/TrackItem/AnimationTrackItem.uxml";
    private Label _titleLabel;
    public VisualElement MainDragArea { get; private set; }
    public VisualElement AnimationOverLine{get;private set;}
    public void Init(SkillTrackStyleBase trackStyle, int startFrameIndex, float frameUnitWidth)
    {
        _titleLabel = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TrackItemAssetPath).Instantiate().Query<Label>();
        Root = _titleLabel;
        MainDragArea = Root.Q("Main");
        AnimationOverLine = Root.Q("OverLine");
        trackStyle.AddItem(Root);
    }
    
    public void SetTitle(string title)
    {
        _titleLabel.text = title;
    }
}
