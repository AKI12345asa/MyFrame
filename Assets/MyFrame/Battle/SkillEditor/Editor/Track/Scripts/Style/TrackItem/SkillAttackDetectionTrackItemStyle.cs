using UnityEditor;
using UnityEngine.UIElements;

public class SkillAttackDetectionTrackItemStyle : SkillTrackItemStyleBase
{
    private const string TrackItemAssetPath = "Assets/MyFrame/Battle/SkillEditor/Editor/Track/Assets/TrackItem/AudioTrackItem.uxml";
    private SkillMutilineTrackStyle.ChildTrack _childTrack;
    private Label _titleLabel;
    public bool IsInit { get; private set; } = false;
    public VisualElement MainDragArea { get; private set; }
    public void Init(float frameUnitWidth,AttackDetectionEvent skillAttackEvent,SkillMutilineTrackStyle.ChildTrack childTrack)
    {
        if (IsInit) return;
        _titleLabel = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TrackItemAssetPath).Instantiate().Query<Label>();
        Root = _titleLabel;
        MainDragArea = Root.Q("Main");
        _childTrack = childTrack;
        _childTrack.InitContent(Root);
        ResetView(frameUnitWidth,skillAttackEvent);
        IsInit = true;
    }
    
    public void SetTitle(string title)
    {
        _titleLabel.text = title;
    }

    public void ResetView(float frameUnitWidth, AttackDetectionEvent attackEvent)
    {
        if (!IsInit) return;
            SetTitle("");
            SetWidth(attackEvent.DurationFrame * frameUnitWidth);
            SetPositionX(frameUnitWidth * attackEvent.FrameIndex);
        
    }
    
}
