using UnityEditor;
using UnityEngine.UIElements;

public class SkillEffectTrackItemStyle : SkillTrackItemStyleBase
{
    private const string TrackItemAssetPath = "Assets/MyFrame/Battle/SkillEditor/Editor/Track/Assets/TrackItem/AudioTrackItem.uxml";
    private SkillMutilineTrackStyle.ChildTrack _childTrack;
    private Label _titleLabel;
    public bool IsInit { get; private set; } = false;
    public VisualElement MainDragArea { get; private set; }
    public void Init(float frameUnitWidth,SkillEffectEvent skillEffectEvent,SkillMutilineTrackStyle.ChildTrack childTrack)
    {
        if (skillEffectEvent.Prefab == null) return;
        _titleLabel = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TrackItemAssetPath).Instantiate().Query<Label>();
        Root = _titleLabel;
        MainDragArea = Root.Q("Main");
        _childTrack = childTrack;
        _childTrack.InitContent(Root);
        ResetView(frameUnitWidth,skillEffectEvent);
        IsInit = true;
    }
    
    public void SetTitle(string title)
    {
        _titleLabel.text = title;
    }

    public void ResetView(float frameUnitWidth, SkillEffectEvent skillEffectEvent)
    {
        if (!IsInit) return;
        if (skillEffectEvent.Prefab != null)
        {
            
            SetTitle(skillEffectEvent.Prefab.name);
            SetWidth(frameUnitWidth * skillEffectEvent.Duration);
            SetPositionX(frameUnitWidth * skillEffectEvent.FrameIndex);
        }
        else
        {
            SetTitle(null);
            SetWidth(frameUnitWidth);
            SetPositionX(0);
        }

    }
}
