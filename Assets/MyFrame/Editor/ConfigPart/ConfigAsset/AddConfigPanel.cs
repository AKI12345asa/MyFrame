using System;
using System.Collections.Generic;
using System.Linq;
using MyFrame.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


public class AddConfigPanel : EditorWindow
{
    public static AddConfigPanel Instance;
    public VisualElement Root;
    private TextField _pathField;
    private DropdownField _typeDropdown;
    private TextField _nameField;
    private List<Type> _createdTypes = new List<Type>();
    public static void Open(IEnumerable<Type> types)
    {
        if (Instance == null)
        {
            Instance = GetWindow<AddConfigPanel>();
            Instance.Init();
        }
        Instance.Refresh(types);
    }

    private void Init()
    {
        Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/MyFrame/Editor/ConfigPart/ConfigAsset/AddConfigPanel.uxml").Instantiate().Q<VisualElement>("AddConfigPanel");
        _pathField = Root.Q<TextField>("PathField");
        _typeDropdown = Root.Q<DropdownField>("TypeDropdown");
        var createdButton = Root.Q<Button>("CreateConfigButton");
        _nameField = Root.Q<TextField>("ConfigNameField");
        Root.Q<Button>("ChoosePathButton").clicked += () =>
        {
            var path = EditorUtility.OpenFolderPanel("选择配置文件保存路径", Application.dataPath, "");
            if (string.IsNullOrEmpty(path)) return;
            path = FileUtil.GetProjectRelativePath(path);
            _pathField.value = path;
        };
        createdButton.clicked += () =>
        {
            if (_createdTypes.Count == 0) return;
            if (string.IsNullOrEmpty(_pathField.value) || string.IsNullOrEmpty(_nameField.value)) return;

            var selectedType = _createdTypes[_typeDropdown.index];
            var asset = CreateInstance(selectedType);
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(_pathField.value + "/" + _nameField.value + ".asset");
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Close();
            ConfigWindow.Instance.Refresh();
        };
        rootVisualElement.Add(Root);
    }
    
    private void Refresh(IEnumerable<Type> types)
    {
        var width = 300f;
        var height = 100f;
        var mainWindowPos = EditorGUIUtility.GetMainWindowPosition();
        position = new Rect(
            mainWindowPos.x + (mainWindowPos.width - width) * 0.5f,
            mainWindowPos.y + (mainWindowPos.height - height) * 0.5f,
            width,
            height);
        _nameField.value = "Config";
        _createdTypes.Clear();
        foreach (var type in types)
        {
            if(type.IsAbstract)  continue;
            _createdTypes.Add(type);
        }
        _typeDropdown.choices = _createdTypes.Select(t => t.Name).ToList();
        if (_createdTypes.Count > 0)
        {
            _typeDropdown.value = _createdTypes[0].Name;
        }
        else
        {
            _typeDropdown.value = string.Empty;
        }
    }
}
