using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Callbacks;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;
using Object = UnityEngine.Object;


public class SkillEditorWindow : EditorWindow
{
    public static SkillEditorWindow Instance;
    private const double ConfigSaveDebounceSeconds = 0.3d;
    private bool _configSaveScheduled;
    private double _lastConfigSaveRequestTime;
    private bool _currentIsPreviewScene;
    [MenuItem("SkillEditor/UI Toolkit/SkillEditorWindow")]
    public static void ShowExample()
    {
        var wnd = GetWindow<SkillEditorWindow>();
        wnd.titleContent = new GUIContent("技能编辑器");
    }

    [DidReloadScripts]
    private static void ResetPreviewAfterScriptsReload()
    {
        EditorApplication.delayCall += () =>
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<SkillEditorWindow>())
            {
                window.ClearPreviewAfterScriptsReload();
            }
        };
    }

    private VisualElement _root;
    public void CreateGUI()
    {
        SkillClip.SetValidateAction(ResetTrackData);
        _root = rootVisualElement;
        Instance = this;
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/MyFrame/Battle/SkillEditor/Editor/EditorWindow/SkillEditorWindow.uxml");
        visualTree.CloneTree(_root);
        InitTopMenu();
        InitTimeShaft();
        InitConsole();
        InitContent();
        InitSkillConfigInspector();
        RefreshSkillConfigInspector();
        if (_skillClip != null)
        {
            _skillClipObjectField.value = _skillClip;
        }
        if(_skillClip == null)CurrentFrameCount = 100;
        if(_currentPreviewCharacterPrefab != null) _previewCharacterPrefabObjectField.value = _currentPreviewCharacterPrefab;
        if (_currentPreviewCharacterObj != null)
        {
            _previewCharacterObjectField.value = _currentPreviewCharacterObj;
        }
        CurrentSelectFrameIndex = 0;
        if (_skillOwner != null)
        {
            _characterConfigField.value = (Object)_skillOwner;
            _skillConfigChoices.value = _chosenSKillConfig?.SkillName;
        }
    }

    private void ResetTrackData()
    {
        var tempSkillConfig = _skillClip;
        _skillClipObjectField.value = null;
        _skillClipObjectField.value = tempSkillConfig;
    }

    //窗口销毁会调用,Unity关闭时不会调用,因此可以用OnDisable保存数据
    /*private void OnDestroy()
    {
        if (SkillClip != null) SaveConfig();
        
    }*/

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        SceneManager.activeSceneChanged += RefreshScene;
        RefreshScene(SceneManager.GetActiveScene(), SceneManager.GetActiveScene());
    }

    private void RefreshScene(Scene oldScene, Scene newScene)
    {
        if (newScene.path != SkillEditorScenePath)
        {
            _currentIsPreviewScene = false;
            return;
        }

        var gameObjects = newScene.GetRootGameObjects();
        _currentIsPreviewScene = true;
        _previewRoot = null;

        foreach (var gameObject in gameObjects)
        {
            if (gameObject.name == "PreviewCharacterRoot")
            {
                _previewRoot = gameObject;
                break;
            }
        }
        if (_previewRoot == null)
        {
            Debug.LogWarning("PreviewCharacterRoot not found in editor scene: " + newScene.path);
            return;
        }
    }

    private void OnDisable()
    {
        if (_configSaveScheduled) FlushPendingConfigSave();
        else if (SkillClip != null) SaveConfig();
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneManager.activeSceneChanged -= RefreshScene;
    }

    private void ClearPreviewAfterScriptsReload()
    {
        ClearCameraPreviewState();
        DestroyPreviewRootChildren();
        _currentPreviewCharacterObj = null;
        _currentPreviewCharacterPrefab = null;
        _currentSkillCamera = null;
        _skillOwner = null;
        _chosenSKillConfig = null;
        _chosenClips = null;
        _skillClip = null;
        DestroyTrack();
        SkillEditorInspector.Instance?.Clean();

        if (_root == null || _frameCountField == null || _currentFrameField == null)
        {
            _currentFrameCount = 100;
            _currentSelectFrameIndex = 0;
            return;
        }
        _previewCharacterObjectField?.SetValueWithoutNotify(null);
        _previewCharacterPrefabObjectField?.SetValueWithoutNotify(null);
        _characterConfigField?.SetValueWithoutNotify(null);
        _skillConfigChoices?.SetValueWithoutNotify(string.Empty);
        if (_skillClipObjectField != null) _skillClipObjectField.SetValueWithoutNotify(null);
        if (_skillClipView != null) _skillClipView.itemsSource = null;
        CurrentFrameCount = 100;
        CurrentSelectFrameIndex = 0;
    }
    
    #region TopMenu

    private const string SkillEditorScenePath = "Assets/MyFrame/Battle/SkillEditor/SkillEditorScene.unity";
    private const string PreviewCharacterParentPath = "PreviewCharacterRoot";
    private string _oldScenePath;
    
    private Button _loadEditorSceneButton;
    private Button _loadOldSceneButton;
    private Button _skillBasicButton;

    private ObjectField _previewCharacterPrefabObjectField;
    private ObjectField _previewCharacterObjectField;
    private ObjectField _skillClipObjectField;
    private GameObject _currentPreviewCharacterPrefab;
    private GameObject _currentPreviewCharacterObj;
    private Cinemachine.CinemachineVirtualCameraBase _currentSkillCamera;
    public GameObject CurrentPreviewCharacterObj => _currentPreviewCharacterObj;
    private void InitTopMenu()
    {
        _loadEditorSceneButton = _root.Q<Button>("LoadEditorSceneButton");
        _loadEditorSceneButton.clicked += LoadEditorButtonClick;
        _loadOldSceneButton = _root.Q<Button>("LoadOldSceneButton");
        _loadOldSceneButton.clicked += LoadOldSceneButtonClick;
        _skillBasicButton = _root.Q<Button>("SkillBasicButton");
        _skillBasicButton.clicked += SkillBasicButtonClick;
        
        _previewCharacterPrefabObjectField = _root.Q<ObjectField>("PreviewCharacterPrefabObjectField");
        _previewCharacterPrefabObjectField.RegisterValueChangedCallback(PreviewCharacterPrefabObjectFieldValueChanged);
        
        _skillClipObjectField = _root.Q<ObjectField>("SkillConfigObjectField");
        _skillClipObjectField.RegisterValueChangedCallback(SkillConfigObjectFieldValueChanged);
        _skillClipObjectField.objectType = typeof(SkillClip);
        
        _previewCharacterObjectField = _root.Q<ObjectField>("PreviewCharacterObjectField");
        _previewCharacterObjectField.RegisterValueChangedCallback(PreviewCharacterObjectFieldValueChanged);
    }

    public bool OnEditorScene
    {
        get
        {
            var currentScenePath = SceneManager.GetActiveScene().path;
            return currentScenePath == SkillEditorScenePath;
        }
    }


    private void OnGUI()
    {
        var currentScenePath = SceneManager.GetActiveScene().path;
        if (currentScenePath != SkillEditorScenePath) return;
        
    }

    //角色预制体修改
    private void PreviewCharacterPrefabObjectFieldValueChanged(ChangeEvent<Object> evt)
    {
        var currentScenePath = SceneManager.GetActiveScene().path;
        if (currentScenePath != SkillEditorScenePath)
        {
            _previewCharacterPrefabObjectField.value = null;
            return;
        }
        if (evt.newValue == _currentPreviewCharacterPrefab) return;
        _currentPreviewCharacterPrefab = evt.newValue as GameObject;
        ClearCameraPreviewState();
        _currentSkillCamera = null;
        if(_currentPreviewCharacterObj != null) DestroyImmediate(_currentPreviewCharacterObj);
        var parent = GameObject.Find("PreviewCharacterRoot").transform;
        if (parent != null && parent.childCount > 0)
        {
            DestroyImmediate(parent.GetChild(0).gameObject);
        }
        
        //实例化新的
        if (evt.newValue != null)
        {
            _currentPreviewCharacterObj = Instantiate(evt.newValue as GameObject,Vector3.zero,Quaternion.Euler(0,0,0),parent);
            _currentPreviewCharacterObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
            _previewCharacterObjectField.value = _currentPreviewCharacterObj;
        }
    }

    private void PreviewCharacterObjectFieldValueChanged(ChangeEvent<Object> evt)
    {
        _currentPreviewCharacterObj = (GameObject)evt.newValue;
    }
    //技能配置修改
    private void SkillConfigObjectFieldValueChanged(ChangeEvent<Object> evt)
    {
        SaveConfig();
        _skillClip = evt.newValue as SkillClip;
        _currentSelectFrameIndex = 0;
        CurrentFrameCount = _skillClip == null?  100 : _skillClip.FrameCount;
        //刷新轨道
        ResetTrack();
        TickSkill();
        if (SkillEditorInspector.Instance != null) SkillEditorInspector.Instance.Clean();
    }
    

    //加载编辑器场景
    private void LoadEditorButtonClick()
    {
        var currentScenePath = SceneManager.GetActiveScene().path;
        if (currentScenePath == SkillEditorScenePath) return;
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        _oldScenePath = currentScenePath;
        EditorSceneManager.OpenScene(SkillEditorScenePath);
        RefreshScene(SceneManager.GetSceneByName("SkillEditorScene"), SceneManager.GetActiveScene());
        RefreshSkillConfigInspector();
    }

   

    //加载旧场景
    private void LoadOldSceneButtonClick()
    {
        if (!string.IsNullOrEmpty(_oldScenePath))
        {
            var currentScenePath = SceneManager.GetActiveScene().path;
            if (currentScenePath == _oldScenePath) return;
            EditorSceneManager.OpenScene(_oldScenePath);
        }
        else Debug.Log("旧场景不存在");
        RefreshSkillConfigInspector();
    }
    //显示技能信息
    private void SkillBasicButtonClick()
    {
        if (_skillClip != null)
        {
            Selection.activeObject = _skillClip;
        }
    }

    
    #endregion

    #region TimerShaft

    private IMGUIContainer _timerShaft;
    private IMGUIContainer _selectLine;
    private VisualElement _contentContainer;
    private int _currentSelectFrameIndex;
    private VisualElement _contentViewPort;
    public int CurrentSelectFrameIndex
    {
        get => _currentSelectFrameIndex;
        
        private set
        {
            var old = _currentSelectFrameIndex;
            
            _currentFrameField.value = _currentSelectFrameIndex;
            if (_currentSelectFrameIndex == value) return;
            if (value > CurrentFrameCount) CurrentFrameCount = value;
            _currentSelectFrameIndex = Mathf.Clamp(value, 0, CurrentFrameCount);
            _currentFrameField.value = _currentSelectFrameIndex;
            
            if (old != _currentSelectFrameIndex)
            {
                //驱动动画表现
                UpdateTimerShaftView();
                TickSkill();
            }
        }
    }

    private int _currentFrameCount;
 
    public int CurrentFrameCount
    {
        get => _currentFrameCount;

        set
        {
            _frameCountField.value = _currentFrameCount;
            if(_currentFrameCount == value) return;
            _currentFrameCount = value;
            _frameCountField.value = _currentFrameCount;
            if (_skillClip != null)
            {
                _skillClip.FrameCount = _currentFrameCount;
            }
            //Content区域尺寸总数的变化
            UpdateContentSize();
        }
    }
    
    //当前帧的偏移量
    private float ContentOffsetPos => MathF.Abs(_contentContainer.transform.position.x);

    private float CurrentSelectFramePos => _currentSelectFrameIndex * _skillEditorConfig.FrameUnitWidth;

    private bool _timerShaftIsMouseEnter = false;
    
    private void InitTimeShaft()
    {
        _timerShaft = _root.Q<IMGUIContainer>("TimerShaft");
        var mainContentView = _root.Q<ScrollView>("MainContentView");
        _contentContainer = mainContentView.Q<VisualElement>("unity-content-container");
        _contentViewPort = mainContentView.Q<VisualElement>("unity-content-viewport");
        _timerShaft.onGUIHandler = DrawTimerShaft;
        _timerShaft.RegisterCallback<MouseDownEvent>(TimerShaftMouseDown);
        _timerShaft.RegisterCallback<MouseMoveEvent>(TimerShaftMouseMove);
        _timerShaft.RegisterCallback<MouseUpEvent>(TimerShaftMouseUp);
        _timerShaft.RegisterCallback<MouseOutEvent>(TimerShaftMouseOut);
        _timerShaft.RegisterCallback<WheelEvent>(TimerShaftWheel);
        _selectLine = _root.Q<IMGUIContainer>("SelectLine");
        _selectLine.onGUIHandler = DrawSelectLine;
    }

  
    private void DrawTimerShaft()
    {
        Handles.BeginGUI();
        Handles.color = Color.white;
        var rect = _timerShaft.contentRect;
        //起始起点帧
        int index = Mathf.CeilToInt(ContentOffsetPos / _skillEditorConfig.FrameUnitWidth);
        //计算距离
        float startOffset = 0;
        if (index > 0) startOffset = _skillEditorConfig.FrameUnitWidth - (ContentOffsetPos % _skillEditorConfig.FrameUnitWidth);
        var tickStep = SkillEditorConfig.MaxFrameWidthLv + 1 - (_skillEditorConfig.FrameUnitWidth / SkillEditorConfig.StandFrameUnitWidth);
        tickStep /= 2;
        if(tickStep == 0) tickStep = 1;
        for (var i = startOffset; i < rect.width; i+= _skillEditorConfig.FrameUnitWidth)//这里的i是用来定位的像素点
        {
            if (index % tickStep == 0)
            {
                Handles.DrawLine(new Vector3(i,rect.height - 10),new Vector3(i ,rect.height));
                var indexStr = index.ToString();
                GUI.Label(new Rect(i - indexStr.Length * 4.5f,rect.y,35,20),indexStr);
            }
            else
            {
                Handles.DrawLine(new Vector3(i,rect.height - 5),new Vector3(i ,rect.height));
            }
            index++;
        }
        
        Handles.EndGUI();
    }
    
    private void DrawSelectLine()
    {
        if (CurrentSelectFramePos >= ContentOffsetPos)
        {
            Handles.BeginGUI();
            Handles.color = Color.white;
            float x = CurrentSelectFramePos - ContentOffsetPos;
            Handles.DrawLine(new Vector3(x,0),new Vector3(x,_contentViewPort.contentRect.height + _timerShaft.contentRect.height));
            Handles.EndGUI();
        }
    }

    private void UpdateTimerShaftView()
    {
        _timerShaft.MarkDirtyLayout();
        _selectLine.MarkDirtyLayout();
    }
    
    #region DrawTimerShaftMouseEvent

    private void TimerShaftMouseDown(MouseDownEvent evt)
    {
        //让选中线的位置卡在帧上
        _timerShaftIsMouseEnter = true;
        _isPlaying = false;
        var newValue = GetFrameIndexByMousePos(evt.localMousePosition.x);
        if (newValue != _currentSelectFrameIndex)
        {
            CurrentSelectFrameIndex =  newValue;
        }
        evt.StopPropagation();
    }

    private void TimerShaftMouseUp(MouseUpEvent evt)
    {
        _timerShaftIsMouseEnter = false;
        evt.StopPropagation();
    }

    private void TimerShaftMouseMove(MouseMoveEvent evt)
    {
        if (_timerShaftIsMouseEnter)
        {
            var newValue = GetFrameIndexByMousePos(evt.localMousePosition.x);
            if (newValue != _currentSelectFrameIndex)
            {
                CurrentSelectFrameIndex =  newValue;
            }
        }
        evt.StopPropagation();
    }

    private void TimerShaftMouseOut(MouseOutEvent evt)
    {
        _timerShaftIsMouseEnter = false;
        evt.StopPropagation();
    }

    /// <summary>
    /// 根据鼠标位置获取当前帧的索引
    /// </summary>
    /// <param name="x"></param>
    /// <param name="ifAddOffset"></param>
    public int GetFrameIndexByMousePos(float x,bool ifAddOffset = true)
    {
        if(ifAddOffset)
        {
            x += ContentOffsetPos;
        }
        return GetFrameIndexByPos(x);
    }

    public int GetFrameIndexByPos(float x)
    {
        return Mathf.RoundToInt(x / _skillEditorConfig.FrameUnitWidth);
    }
    
    #endregion
    
    private void TimerShaftWheel(WheelEvent evt)
    {
        var delta = (int)evt.delta.y;
        _skillEditorConfig.FrameUnitWidth = Mathf.Clamp(_skillEditorConfig.FrameUnitWidth - delta, SkillEditorConfig.StandFrameUnitWidth, SkillEditorConfig.MaxFrameWidthLv * SkillEditorConfig.StandFrameUnitWidth);
        UpdateTimerShaftView();
        UpdateContentSize();
        ResetTrack();
    }
    
    #endregion
    
    #region Console

    private Button _previousFrameButton;
    private Button _playButton;
    private Button _nextFrameButton;
    private IntegerField _currentFrameField;
    private IntegerField _frameCountField;

    private void InitConsole()
    {
        _previousFrameButton = _root.Q<Button>("PreviousFrameButton");
        _previousFrameButton.clicked += PreviousFrameButtonClick;
        
        _playButton = _root.Q<Button>("PlayButton");
        _playButton.clicked += PlayButtonClicked;
        
        _nextFrameButton = _root.Q<Button>("NextFrameButton");
        _nextFrameButton.clicked += NextFrameButtonClicked;
        
        _currentFrameField = _root.Q<IntegerField>("CurrentFrameField");
        _currentFrameField.RegisterValueChangedCallback(CurrentFrameFieldValueChanged);
        
        
        _frameCountField = _root.Q<IntegerField>("FrameCountField");
        _frameCountField.RegisterValueChangedCallback(FrameCountFieldValueChanged);
   
    }



    private void PreviousFrameButtonClick()
    {
        if (_currentSelectFrameIndex == 0) return;
        IsPlaying = false;
        CurrentSelectFrameIndex -= 1;
    }
    private void PlayButtonClicked()
    {
      IsPlaying = !IsPlaying;
    }
    private void NextFrameButtonClicked()
    {
        IsPlaying = false;
       CurrentSelectFrameIndex += 1;
    }


    private void CurrentFrameFieldValueChanged(ChangeEvent<int> evt)
    {
        if(evt.newValue != evt.previousValue && evt.newValue > 0)
        {
            CurrentSelectFrameIndex = evt.newValue;
        }
    }
    
    private void FrameCountFieldValueChanged(ChangeEvent<int> evt)
    {
        if(evt.newValue != evt.previousValue && evt.newValue > 0)
        {
            CurrentFrameCount = evt.newValue;
        }
    }


    #endregion
    
    #region SkillConfigInspector
    private ObjectField _characterConfigField;
    private DropdownField _skillConfigChoices;
    private GameObject _previewRoot;
    private ISkillOwner _skillOwner;
    private List<SkillClip> _chosenClips;
    private SkillConfig _chosenSKillConfig;
    private ListView _skillClipView;
    public GameObject PreviewModel { get; private set; }

    public Transform CurrentSkillCameraTransform
    {
        get
        {
            if (_currentSkillCamera != null) return _currentSkillCamera.transform;
            if (_skillOwner == null || _currentPreviewCharacterObj == null) return null;
            var skillCamera = _skillOwner.GetSkillCamera(_currentPreviewCharacterObj);
            return skillCamera == null ? null : skillCamera.transform;
        }
    }

    private void InitSkillConfigInspector()
    {
        _characterConfigField = _root.Q<ObjectField>("CharacterConfigField");
        _characterConfigField.objectType = typeof(ISkillOwner);
        _characterConfigField.RegisterValueChangedCallback(evt =>
        {
            if(evt.newValue == null) return;
            ClearCameraPreviewState();
            _currentSkillCamera = null;
            if(PreviewModel != null) DestroyImmediate(PreviewModel);
            if(evt.newValue is not ISkillOwner skillOwner) return;
            if(_currentPreviewCharacterObj != null) DestroyImmediate(_currentPreviewCharacterObj);
            _skillOwner = skillOwner;
            _skillConfigChoices.SetValueWithoutNotify("");
            _skillClipView.itemsSource = null;
            _currentPreviewCharacterObj = Instantiate(_skillOwner.SkillOwnerGameObject,Vector3.zero,Quaternion.identity,_previewRoot.transform);
            _currentSkillCamera = _skillOwner.GetSkillCamera(_currentPreviewCharacterObj);
            StripPreviewComponents(_currentPreviewCharacterObj);
            PreviewModel = _currentPreviewCharacterObj.GetComponentInChildren<Animator>().gameObject;
            _skillConfigChoices.choices = _skillOwner.SkillOwnerSkillConfigs.ConvertAll(config => config.SkillName);
            
        });
        _skillConfigChoices = _root.Q<DropdownField>("SkillConfigChoices");
        _skillConfigChoices.RegisterValueChangedCallback(evt =>
        {
            _chosenSKillConfig = _skillOwner.SkillOwnerSkillConfigs.Find(config => config.SkillName == evt.newValue);
            _chosenClips = _chosenSKillConfig?.Clips.ToList(); 
            _skillClipView.itemsSource =  _chosenClips;
            EventTrackItem.CurrentSelectItem?.OnUnSelect();
        });
        _skillClipView = _root.Q<ListView>("SkillClipListView");
        _skillClipView.makeItem = () => new Button();
        _skillClipView.bindItem = (item, index) =>
        {
            var button = item as Button;
            var skillClip = _chosenClips[index];
            button.text = skillClip.name;
            EventTrackItem.CurrentSelectItem?.OnUnSelect();
            button.clicked += () =>
            {
                _skillClipObjectField.value = skillClip;
            };
        };
        
    }
    
    private void RefreshSkillConfigInspector()
    {
        if (_previewRoot != null) DestroyImmediate(PreviewModel);
        _characterConfigField.value = null;
         PreviewModel = null;
         _skillConfigChoices.choices = new List<string>();
         _skillConfigChoices.SetValueWithoutNotify("");
         _skillClipView.itemsSource = null;
        IsPlaying = false;
        CurrentSelectFrameIndex = 0;
    }

    #endregion

    private void DestroyPreviewRootChildren()
    {
        if (_previewRoot == null) return;
        for (var index = _previewRoot.transform.childCount - 1; index >= 0; index--)
        {
            DestroyImmediate(_previewRoot.transform.GetChild(index).gameObject);
        }
    }

    private void StripPreviewComponents(GameObject previewObject)
    {
        foreach (var component in previewObject.GetComponents<Component>())
        {
            if (component == null || component is Transform || component is Animator ||
                component is Animation_Controller ||
                component is Cinemachine.CinemachineVirtualCameraBase ||
                component is Cinemachine.CinemachineImpulseSource ||
                component is Cinemachine.CinemachineImpulseListener ||
                component is Cinemachine.CinemachineIndependentImpulseListener ||
                component is SkillChecker ||
                component is Collider ||
                _skillOwner.GetRemainingComponentWhenPlay().Contains(component.GetType()))
            {
                continue;
            }

            DestroyImmediate(component);
        }
    }

    private void ClearCameraPreviewState()
    {
        _trackList.OfType<CameraEventTrack>().ToList().ForEach(track => track.ClearPreviewState());
    }
    
    #region Config

    private SkillClip _skillClip;
    public SkillClip SkillClip => _skillClip;
    private SkillEditorConfig _skillEditorConfig = new SkillEditorConfig();

    public void SaveConfig()
    {
        CancelScheduledConfigSave();
        if (_skillClip != null)
        {
            EditorUtility.SetDirty(_skillClip);
            AssetDatabase.SaveAssetIfDirty(_skillClip);
            
           ResetFormConfig();
        }
    }

    public void RequestSaveConfig()
    {
        if (_skillClip == null) return;

        _lastConfigSaveRequestTime = EditorApplication.timeSinceStartup;
        if (_configSaveScheduled) return;

        _configSaveScheduled = true;
        EditorApplication.update += ProcessScheduledConfigSave;
    }

    private void ProcessScheduledConfigSave()
    {
        if (!_configSaveScheduled) return;
        if (EditorApplication.timeSinceStartup - _lastConfigSaveRequestTime < ConfigSaveDebounceSeconds) return;

        CancelScheduledConfigSave();
        SaveConfig();
    }

    private void FlushPendingConfigSave()
    {
        if (!_configSaveScheduled) return;

        CancelScheduledConfigSave();
        SaveConfig();
    }

    private void CancelScheduledConfigSave()
    {
        if (!_configSaveScheduled) return;

        EditorApplication.update -= ProcessScheduledConfigSave;
        _configSaveScheduled = false;
    }

    private void ResetFormConfig()
    {
        _trackList.ForEach(track =>
        {
            track.OnConfigChanged();
        });
    }
    #endregion

    #region Track
    private VisualElement _trackMenuParent;
    private VisualElement _contentListView;
    private List<SkillTrackBase> _trackList = new();
    
    private void InitContent()
    {
        _contentListView = _root.Q<VisualElement>("ContentListView");
        _trackMenuParent = _root.Q<VisualElement>("TrackMenuList");
        var mainContentView = _root.Q<ScrollView>("MainContentView");
        var trackMenuScrollView = _root.Q<ScrollView>("TrackMenuScrollView");

        trackMenuScrollView.verticalScroller.valueChanged += value =>
        {
            mainContentView.verticalScroller.value = value;
        };
        mainContentView.verticalScroller.valueChanged += value =>
        {
            trackMenuScrollView.verticalScroller.value = value;
        };
        UpdateContentSize();
        InitTrack();
    }

    public void ResetTrack()
    {
        if (_skillClip == null)
        {
            DestroyTrack();
        }
        else
        {
            
            if (_trackList.Count == 0)
            {
                InitTrack();    
            }
            
            _trackList.ForEach(track =>
            {
                track.ResetView(_skillEditorConfig.FrameUnitWidth);
            });
        }
    }

    private void DestroyTrack()
    {
        _trackList.ForEach(track =>
        {
            track.Destroy();
        });
        _trackList.Clear();
    }
    private void UpdateContentSize()
    {
        _contentListView.style.width = _skillEditorConfig.FrameUnitWidth * CurrentFrameCount;
    }

    private void InitTrack()
    {
        if (_skillClip == null) return;
        InitEventTrack();
        InitAnimationTrack();
        InitAudioTrack();
        InitEffectTrack();
        InitAttackTrack();
        InitCameraEventTrack();
    }
    
    private void InitAnimationTrack()
    {
        var animationTrack = new AnimationTrack();
        animationTrack.Init(_trackMenuParent, _contentListView,_skillEditorConfig.FrameUnitWidth);
        _trackList.Add(animationTrack);
        _getPositionForRootMotion = animationTrack.GetPositionForRootMotion;
    }

    private void InitAudioTrack()
    {
        var audioTrack = new AudioTrack();
        audioTrack.Init(_trackMenuParent, _contentListView, _skillEditorConfig.FrameUnitWidth);
        _trackList.Add(audioTrack);
    }
    
    private void InitEffectTrack()
    {
        var effectTrack = new EffectTrack();
        effectTrack.Init(_trackMenuParent, _contentListView, _skillEditorConfig.FrameUnitWidth);
        _trackList.Add(effectTrack);
        if (SceneManager.GetActiveScene().path != PreviewCharacterParentPath) return;
        effectTrack.ClearEffects();
    }

    private void InitAttackTrack()
    {
        var attackTrack = new AttackDetectionTrack();
        attackTrack.Init(_trackMenuParent, _contentListView, _skillEditorConfig.FrameUnitWidth);
        _trackList.Add(attackTrack);
    }

    private void InitCameraEventTrack()
    {
        var cameraEventTrack = new CameraEventTrack();
        cameraEventTrack.Init(_trackMenuParent, _contentListView, _skillEditorConfig.FrameUnitWidth);
        _trackList.Add(cameraEventTrack);
    }

    private void InitEventTrack()
    {
        var eventTrack = new EventTrack();
        eventTrack.Init(_trackMenuParent, _contentListView, _skillEditorConfig.FrameUnitWidth);
        _trackList.Add(eventTrack);
    }
    
    public void ShowTrackItemOnInspector(TrackItemBase trackItem,SkillTrackBase track)
    {
        SkillEditorInspector.SetTrackItem(trackItem,track);
        Selection.activeObject = this;
    }
    #endregion

    #region Preview

    private bool _isPlaying;

    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            _isPlaying = value;
            if (_isPlaying)
            {
                _startTime = DateTime.Now;
                _startFrameIndex = _currentSelectFrameIndex;
                
                //OnPlay
               _trackList.ForEach(track => track.OnPlay(CurrentSelectFrameIndex));
            }
            else
            {
                //OnStop
                _trackList.ForEach(track => track.OnStop());
            }
            
        }
        
    }

    private DateTime _startTime;
    private int _startFrameIndex;
    private void Update()
    {
        if (IsPlaying)
        {
            var time = (float)DateTime.Now.Subtract(_startTime).TotalSeconds;
            //确认时间轴帧率
            float frameRate;
            if (_skillClip != null) frameRate = _skillClip.FrameRate;
            else frameRate = _skillEditorConfig.DefaultFrameRate;
            
            //根据时间差计算当前的选中帧
            CurrentSelectFrameIndex = (int)(time * frameRate) + _startFrameIndex;

            //到达最后一帧自动暂停
            if (CurrentSelectFrameIndex == CurrentFrameCount)
            {
                IsPlaying = false;
            }
        }
    }

    public void TickSkill()
    {
        if (_skillClip != null && _currentPreviewCharacterObj != null)
        {
       
            _trackList.ForEach(track =>
            {
                track.TickView(_currentSelectFrameIndex);
            });
        }
    }

    private Func<int,bool,Vector3> _getPositionForRootMotion;
    
    public Vector3 GetPositionForRootMotion(int frameIndex, bool recover = false) => _getPositionForRootMotion(frameIndex, recover);
    #endregion

    #region Gizmos and SceneGUI
    [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Selected)]
    private static void DrawGizmos(Skill_Player skillPlayer, GizmoType gizmoType)
    {
        if (SceneManager.GetActiveScene().path != SkillEditorScenePath) return;
        if (!Instance||Instance._currentPreviewCharacterObj == null || Instance._currentPreviewCharacterObj?.GetComponentInChildren<Skill_Player>() != skillPlayer) return;
        Instance._trackList.ForEach(track =>
        {
            track.DrawGizmos();
        });
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (Instance == null) return;
        if (Instance._currentPreviewCharacterObj == null) return;
        Instance._trackList.ForEach(track =>
        {
            track.OnSceneGUI();
        });
    }
    #endregion
}

public class SkillEditorConfig
{
    public const int StandFrameUnitWidth = 10;
    public const int MaxFrameWidthLv = 10;
    public int FrameUnitWidth = 10; //每帧宽度
    public readonly int DefaultFrameRate = 10;//默认帧率
}