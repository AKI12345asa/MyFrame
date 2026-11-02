using UnityEditor;
using UnityEngine.UIElements;

public class SkillAudioTrackItemStyle : SkillTrackItemStyleBase
{
    private const string TrackItemAssetPath = "Assets/MyFrame/Battle/SkillEditor/Editor/Track/Assets/TrackItem/AudioTrackItem.uxml";
    private SkillMutilineTrackStyle.ChildTrack _childTrack;
    private Label _titleLabel;
    public bool IsInit { get; private set; } = false;
    public VisualElement MainDragArea { get; private set; }
    public void Init(float frameUnitWidth,SkillAudioEvent skillAudioEvent,SkillMutilineTrackStyle.ChildTrack childTrack)
    {
        if (skillAudioEvent.AudioClip == null) return;
        _titleLabel = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TrackItemAssetPath).Instantiate().Query<Label>();
        Root = _titleLabel;
        MainDragArea = Root.Q("Main");
        _childTrack = childTrack;
        _childTrack.InitContent(Root);
        ResetView(frameUnitWidth,skillAudioEvent);
        IsInit = true;
    }
    
    public void SetTitle(string title)
    {
        _titleLabel.text = title;
    }

    public void ResetView(float frameUnitWidth, SkillAudioEvent skillAudioEvent)
    {
        if (!IsInit) return;
        if (skillAudioEvent.AudioClip != null)
        {
            SetTitle(skillAudioEvent.AudioClip.name);
            SetWidth(frameUnitWidth * skillAudioEvent.AudioClip.length * SkillEditorWindow.Instance.SkillClip.FrameRate);
            SetPositionX(frameUnitWidth * skillAudioEvent.FrameIndex);
        }
        else
        {
            SetTitle(null);
            SetWidth(frameUnitWidth);
            SetPositionX(0);
        }

    }
}
