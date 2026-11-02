using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SkillSingleLineTrackStyle: SkillTrackStyleBase
{
    private const string MenuAssetPath = "Assets/MyFrame/Battle/SkillEditor/Editor/Track/Assets/SingLineTrackStyle/SingleLineTrackMenu.uxml";
    private const string TrackAssetPath = "Assets/MyFrame/Battle/SkillEditor/Editor/Track/Assets/SingLineTrackStyle/SingleLineTrackContent.uxml";

    public void Init(VisualElement menuParent,VisualElement contentParent,string title)
    {
        MenuParent = menuParent;
        ContentParent = contentParent;
        MenuRoot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MenuAssetPath).Instantiate().Query().ToList()[1];
        TitleLabel = (Label)MenuRoot;
        TitleLabel.text = title;
        MenuParent.Add(MenuRoot);
        ContentRoot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TrackAssetPath).Instantiate().Query().ToList()[1];
        ContentParent.Add(ContentRoot);
    }
    
}
