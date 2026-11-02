using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SkillMutilineTrackStyle : SkillTrackStyleBase
{
    #region 常量
    private const string MenuAssetPath = "Assets/MyFrame/Battle/SkillEditor/Editor/Track/Assets/MultilineTrack/MultilineTrackMenu.uxml";
    private const string TrackAssetPath = "Assets/MyFrame/Battle/SkillEditor/Editor/Track/Assets/MultilineTrack/MultilineTrackContent.uxml";
    private const float HeadHeight = 35;//多的5是外边距
    private const float ItemHeight = 32;//多的2是底部外距
    #endregion
    
   
    private Action _addChildTrackAction;
    private IList<string> _addTypeChoices;
    private Action<string> _addChildTrackWithTypeAction;
    private Func<int, bool> _deleteChildTrackFunc;
    private Action<int,int> _swapChildTrackAction;
    private Action<ChildTrack,string> _updateTrackNameAction;

    private VisualElement _menuItemParent;
    private Button _addButton;

    private List<ChildTrack> _childTrackList = new ();
    

    public void Init(VisualElement menuParent,VisualElement contentParent,string title,Action addChildTrackFunc,Func<int, bool> deleteChildTrackFunc,Action<int,int> swapChildTrackAction,Action<ChildTrack,string> updateTrackNameAction)
    {
        _addChildTrackAction = addChildTrackFunc;
        _deleteChildTrackFunc = deleteChildTrackFunc;
        _swapChildTrackAction = swapChildTrackAction;
        _updateTrackNameAction = updateTrackNameAction;
        
        MenuParent = menuParent;
        ContentParent = contentParent;

        MenuRoot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MenuAssetPath).Instantiate().Query().ToList()[1];
        MenuParent.Add(MenuRoot);
        
        TitleLabel = MenuRoot.Q<Label>("Title");
        TitleLabel.text = title;
        
        ContentRoot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TrackAssetPath).Instantiate().Query().ToList()[1];
        ContentParent.Add(ContentRoot);
        
        _menuItemParent = MenuRoot.Q<VisualElement>("ChildTrackMenuList");
        _menuItemParent.RegisterCallback<MouseDownEvent>(ItemParentMouseDown);
        _menuItemParent.RegisterCallback<MouseMoveEvent>(ItemParentMouseMove);
        _menuItemParent.RegisterCallback<MouseUpEvent>(ItemParentMouseUp);
        _menuItemParent.RegisterCallback<MouseOutEvent>(ItemParentMouseOut);
        
        
        _addButton = MenuRoot.Q<Button>("AddButton");
        _addButton.clicked += AddButtonClicked;
        
        UpdateSize();
    }

    #region 子轨道鼠标交互

    private bool _isDragging;
    private int _selectTrackIndex = -1;
    private bool _isSwap;
    private void ItemParentMouseDown(MouseDownEvent evt)
    {
        if (_selectTrackIndex != -1)
        {
            _childTrackList[_selectTrackIndex].UnSelect();
        }
        
        //通过高度计算当前鼠标指向的是在第几个轨道
        var mousePos = evt.localMousePosition.y - ItemHeight / 2;
        var trackIndex = GetChildTrackIndexByMousePosition(mousePos);
        _childTrackList[trackIndex].Select();
        _selectTrackIndex = trackIndex;
        _isDragging = true;
    }

    private void ItemParentMouseMove(MouseMoveEvent evt)
    {
        if (_selectTrackIndex == -1 || !_isDragging) return;
        var mousePosition = evt.localMousePosition.y - ItemHeight / 2;
        var mouseTrackIndex = GetChildTrackIndexByMousePosition(mousePosition);
        if (mouseTrackIndex != _selectTrackIndex)
        {
            SwapChildTrack(_selectTrackIndex,mouseTrackIndex);
            _selectTrackIndex = mouseTrackIndex; //把选中轨道更新到鼠标所在的轨道
            _isSwap = true;
        }
    }

    private void ItemParentMouseUp(MouseUpEvent evt)
    {
        _isDragging = false;
        if (!_isSwap) return;
        if (_selectTrackIndex != -1)
        {
            _childTrackList[_selectTrackIndex].UnSelect();
            _selectTrackIndex = -1;
        }
        SkillEditorWindow.Instance.SaveConfig();
        _isSwap = false;
    }

    private void ItemParentMouseOut(MouseOutEvent evt)
    {
        //该函数的经常会因为子物体的遮挡导致无意义的调用
        if (!_menuItemParent.ContainsPoint(evt.localMousePosition))
        {
            if (_selectTrackIndex != -1)
            {
                _childTrackList[_selectTrackIndex].UnSelect();
                _selectTrackIndex = -1;
            }
            _isDragging = false;
            if (!_isSwap) return;
            SkillEditorWindow.Instance.SaveConfig();
            _isSwap = false;
        }
    }

    private int GetChildTrackIndexByMousePosition(float mousePosY)
    {
        var trackIndex = Mathf.RoundToInt(mousePosY / ItemHeight);
        trackIndex = Mathf.Clamp(trackIndex, 0, _childTrackList.Count - 1);
        return trackIndex;
    }
    #endregion

    private void SwapChildTrack(int index1, int index2)
    {
        var childTrack1 = _childTrackList[index1]; 
        var childTrack2 = _childTrackList[index2];
        _childTrackList[index2] = childTrack1;
        _childTrackList[index1] = childTrack2;
        UpdateChildren();//UI方面的切换
       _swapChildTrackAction(index1, index2);//数据方面的切换
    }
    
    private void UpdateSize()
    {
        var height = HeadHeight + _childTrackList.Count * ItemHeight;
        ContentRoot.style.height = height;
        MenuRoot.style.height = height;
        _menuItemParent.style.height = height - HeadHeight;
    }
    
    /// <summary>
    /// 添加子轨道
    /// </summary>
    private void AddButtonClicked()
    {
        if (_addTypeChoices != null)
        {
            var menu = new GenericMenu();
            foreach (var choice in _addTypeChoices)
            {
                var selectedChoice = choice;
                menu.AddItem(new GUIContent(selectedChoice), false, () => _addChildTrackWithTypeAction?.Invoke(selectedChoice));
            }
            menu.DropDown(_addButton.worldBound);
            return;
        }

        _addChildTrackAction?.Invoke();
    }

    public void ConfigureAddTypeDropdown(IList<string> choices, Action<string> addChildTrackWithTypeAction)
    {
        if (choices == null || choices.Count == 0)
        {
            throw new ArgumentException("At least one track item type is required.", nameof(choices));
        }

        _addTypeChoices = new List<string>(choices);
        _addChildTrackWithTypeAction = addChildTrackWithTypeAction;
    }

    public ChildTrack AddChildTrack()
    {
        var childTrack = new  ChildTrack();
        //TODO:track部分的船体
        childTrack.Init(_menuItemParent,ContentRoot,_childTrackList.Count,DeleteChildTrackAndData,DestroyChildTrack,UpdateTrackName);
        _childTrackList.Add(childTrack);
        UpdateSize();
        return childTrack;
    }

    /// <summary>
    /// 删除子轨道及其数据
    /// </summary>
    /// <param name="childTrack"></param>
    private void DeleteChildTrackAndData(ChildTrack childTrack)
    {
        if(_deleteChildTrackFunc == null) return;
        var index = childTrack.GetIndex();
        if (_deleteChildTrackFunc(index))
        {
            childTrack.DoDestroy();
            _childTrackList.RemoveAt(index);
            UpdateChildren(index);
            UpdateSize();
        }
    }
    
    /// <summary>
    /// 删除轨道UI
    /// </summary>
    /// <param name="childTrack"></param>
    private void DestroyChildTrack(ChildTrack childTrack)
    {
        var index = childTrack.GetIndex();
            childTrack.DoDestroy();
            _childTrackList.RemoveAt(index);
            UpdateChildren(index);
            UpdateSize();
    }

    private void UpdateTrackName(ChildTrack childTrack, string newName)
    {
        _updateTrackNameAction(childTrack, newName);
    }
    
    private void UpdateChildren(int startIndex = 0)
    {
        for (var i = startIndex; i < _childTrackList.Count; i++)
        {
            _childTrackList[i].SetIndex(i);
        }
    }

    /// <summary>
    /// 多行轨道的子轨道
    /// </summary>
    public class ChildTrack
    {
        private const string ChildTrackMenuItemAssetPath = "Assets/MyFrame/Battle/SkillEditor/Editor/Track/Assets/MultilineTrack/MultilineTrackMenuItem.uxml";
        private const string ChildTrackContentAssetPath = "Assets/MyFrame/Battle/SkillEditor/Editor/Track/Assets/MultilineTrack/MultilineTrackContentItem.uxml";
        
        public VisualElement MenuRoot;
        public VisualElement ContentRoot;

        public VisualElement MenuParent;
        public VisualElement ContentParent;

        private TextField _trackNameField;
        
        private Action<ChildTrack> _deleteAction;//实现父项和子项的通信
        private Action<ChildTrack> _destroyAction;
        private Action<ChildTrack, string> _updateTrackNameAction;
        
        private static readonly Color NormalColor = new Color(0,0,0,0);
        private static readonly Color SelectColor = Color.green;

        private int _index;
        private VisualElement _content;
        public void Init(VisualElement menuParent, VisualElement trackParent,int index,Action<ChildTrack> deleteAction,Action<ChildTrack> destroyAction,Action<ChildTrack, string> updateTrackNameAction)
        {
            MenuParent = menuParent;
            ContentParent = trackParent;
            _deleteAction = deleteAction;
            _destroyAction = destroyAction;
            _updateTrackNameAction = updateTrackNameAction;
            
            MenuRoot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ChildTrackMenuItemAssetPath).Instantiate().Query().ToList()[1];
            MenuParent.Add(MenuRoot);
            
            _trackNameField = MenuRoot.Q<TextField>("NameField");
            _trackNameField.RegisterCallback<FocusInEvent>(TrackNameFileFieldFocusIn);
            _trackNameField.RegisterCallback<FocusOutEvent>(TrackNameFileFieldFocusOut);
            
            var deleteButton = MenuRoot.Q<Button>("DeleteButton");
            deleteButton.clicked += () => _deleteAction(this);

            ContentRoot = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ChildTrackContentAssetPath).Instantiate().Query().ToList()[1];
            ContentParent.Add(ContentRoot);
            
            SetIndex(index);
            UnSelect();
        }

        private string _oldTrackName;
        private void TrackNameFileFieldFocusIn(FocusInEvent evt)
        {
            _oldTrackName = _trackNameField.value;   
        }

        private void TrackNameFileFieldFocusOut(FocusOutEvent evt)
        {
           if(_oldTrackName == _trackNameField.value) return;
           _updateTrackNameAction(this,_trackNameField.value);
        }

        public void InitContent(VisualElement content)
        {
            _content = content;
            ContentRoot.Add(_content);
        }
        
        public void SetIndex(int index)
        {
            _index = index;
            float height = 0;
            var pos = MenuRoot.transform.position;
            height = index * ItemHeight;
            pos.y = height;
            MenuRoot.transform.position = pos;
            
            var trackPos = ContentRoot.transform.position;
            height = index * ItemHeight + HeadHeight;
            trackPos.y = height;
            ContentRoot.transform.position = trackPos;
        }

        public void SetTrackName(string name)
        {
            _trackNameField.value = name;
        }
        
        public int GetIndex()
        {
            return _index;
        }
        
        public void Destroy()
        {
            _destroyAction?.Invoke(this);
        }
        public void DoDestroy()
        {
            MenuParent.Remove(MenuRoot);
            ContentParent.Remove(ContentRoot);
        }
        public void Select()
        {
            MenuRoot.style.backgroundColor = SelectColor;
        }
        
        public void UnSelect()
        {
            MenuRoot.style.backgroundColor = NormalColor;
        }
    }
}
