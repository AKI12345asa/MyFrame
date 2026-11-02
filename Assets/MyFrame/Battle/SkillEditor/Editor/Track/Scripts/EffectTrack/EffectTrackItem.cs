using System.Linq;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class EffectTrackItem : TrackItemBase<EffectTrack>
{
    private SkillMutilineTrackStyle.ChildTrack _childTrack;
    
    private SkillEffectTrackItemStyle EffectTrackItemStyle => ItemStyle as SkillEffectTrackItemStyle;

    private SkillEffectEvent _effectEvent;
    
    public SkillEffectEvent EffectEvent => _effectEvent;
    public void Init(EffectTrack track,float frameUnitWidth,SkillEffectEvent skillEffectEvent,SkillMutilineTrackStyle.ChildTrack childTrack)
    {
        Track = track;
        _frameIndex = skillEffectEvent.FrameIndex;
        _childTrack = childTrack;
        FrameUnitWidth = frameUnitWidth; NormalColor = new Color(0.388f, 0.850f, 0.905f, 0.5f);
        SelectColor = new Color(0.388f, 0.850f, 0.905f, 0.8f);
        ItemStyle = new SkillEffectTrackItemStyle();
        _effectEvent = skillEffectEvent;
        _childTrack.ContentRoot.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
        _childTrack.ContentRoot.RegisterCallback<DragExitedEvent>(OnDragExited);
        ResetView(frameUnitWidth);
    }

    public override void ResetView(float frameWidth)
    {
        base.ResetView(frameWidth);
        if (_effectEvent.Prefab != null) 
        {
            if (!EffectTrackItemStyle.IsInit)
            {
                //绑定事件
            EffectTrackItemStyle.Init(FrameUnitWidth, _effectEvent, _childTrack);
            EffectTrackItemStyle.MainDragArea.RegisterCallback<MouseDownEvent>(MouseDown);
            EffectTrackItemStyle.MainDragArea.RegisterCallback<MouseUpEvent>(MouseUp);
            EffectTrackItemStyle.MainDragArea.RegisterCallback<MouseOutEvent>(MouseOut);
            EffectTrackItemStyle.MainDragArea.RegisterCallback<MouseMoveEvent>(MouseMove);
            }
        }
        EffectTrackItemStyle.ResetView(frameWidth,_effectEvent);
        
        //强行生成重新预览
        CleanEffectPreviewObj();
        TickView(SkillEditorWindow.Instance.CurrentSelectFrameIndex);
    }

    public void Destroy()
    {
        _childTrack.Destroy();
    }

    public void CleanEffectPreviewObj()
    {
        if (_effectPreviewObject != null)
        {
            Object.DestroyImmediate(_effectPreviewObject);
            _effectPreviewObject = null;
        }
    }
    
    public void SetTrackName(string name)
    {
        _childTrack.SetTrackName(name);
    }
    
    #region 鼠标交互
 
    private bool _mouseDrag = false;
    private float _startDragPosX;
    private int _startDragFrameIndex;
    private readonly object _mouseLock = new object();
    private void MouseDown(MouseDownEvent evt)
    {
        _startDragPosX = evt.mousePosition.x;
        _startDragFrameIndex = _frameIndex;
        _mouseDrag = true;
        Select();
       
    }

    private void MouseUp(MouseUpEvent evt)
    {
        if(_mouseDrag)ApplyDrag();
        _mouseDrag = false;
       
    }

    private void MouseOut(MouseOutEvent evt)
    {
        if(_mouseDrag)ApplyDrag();
        _mouseDrag = false;
    }

    private void MouseMove(MouseMoveEvent evt)
    {
        if (!_mouseDrag) return;
        var offsetPos = evt.mousePosition.x - _startDragPosX;
        var offsetFrame = Mathf.RoundToInt(offsetPos / FrameUnitWidth);
        var targetFrameIndex = _startDragFrameIndex + offsetFrame;
        if (targetFrameIndex < 0 || offsetFrame == 0) return;//不考虑拖动到负数帧的情况
            _frameIndex = targetFrameIndex;
            _effectEvent.FrameIndex = _frameIndex;
           //刷新视图
           ResetView(FrameUnitWidth);
    }

    private void ApplyDrag()
    {
            if (_startDragFrameIndex != _frameIndex)
            {
               _effectEvent.FrameIndex = _frameIndex;
            }
            SkillEditorWindow.Instance.SaveConfig();
    }

    /*public void CheckFrameCount()
    {
        var frameCount = (int)(_audioEvent.AudioClip.length * SkillEditorWindow.Instance.SkillClip.FrameRate);
        //当前动画超过的技能总帧数，则拓展技能总帧数
        if (_frameIndex + frameCount > SkillEditorWindow.Instance.SkillClip.FrameCount)
        {
            SkillEditorWindow.Instance.CurrentFrameCount = _frameIndex + frameCount;
        }
    }*/
    
    #endregion
    
    #region 资源的拖拽

    private void OnDragUpdate(DragUpdatedEvent evt)
    {
        //监听当前用户拖拽的是否是动画片段
        var objs = DragAndDrop.objectReferences; 
            
        if (objs[0] is GameObject)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }
  
    }
    private void OnDragExited(DragExitedEvent evt)
    {
        var objs = DragAndDrop.objectReferences;
        if (objs[0] is GameObject prefab)
        {
            var selectFrameIndex = SkillEditorWindow.Instance.GetFrameIndexByPos(evt.localMousePosition.x);//轨道的本地坐标有考虑偏移值的影响所以直接根据本地坐标计算即可;

            if (selectFrameIndex < 0) return;
            _effectEvent.Prefab = prefab;
            _effectEvent.Position = Vector3.zero;
            _effectEvent.AutoDestruct = true;
            _effectEvent.Duration = 0;
            _effectEvent.Rotation = Vector3.zero;
            _effectEvent.Scale = Vector3.one;
            _effectEvent.FrameIndex = selectFrameIndex;
            
            var duration = prefab.GetComponentsInChildren<ParticleSystem>().Max(system => system.main.duration);
            _effectEvent.Duration = (int)(duration * SkillEditorWindow.Instance.SkillClip.FrameRate);
            _frameIndex = _effectEvent.FrameIndex;
            ResetView(FrameUnitWidth);
            SkillEditorWindow.Instance.SaveConfig();
        }
    }

    #endregion

    #region 预览

    private GameObject _effectPreviewObject;
    public void TickView(int frameIndex)
    {
        if(_effectEvent.Prefab == null || !SkillEditorWindow.Instance.CurrentPreviewCharacterObj) return;
        //是不是在播放范围内
        var durationFrame = _effectEvent.Duration;

        if (_effectEvent.FrameIndex <= frameIndex && _effectEvent.FrameIndex + durationFrame > frameIndex)
        {
            var editorWindow = SkillEditorWindow.Instance;
            // 优先使用自定义预览模型，未指定则回退到当前预览角色，避免未赋值导致的异常
            var previewModel = editorWindow.PreviewModel;
            Transform characterRoot = previewModel ? previewModel.transform : editorWindow.CurrentPreviewCharacterObj?.transform;
            if (characterRoot == null)
            {
                CleanEffectPreviewObj();
                return;
            }
            //对比预制体
            if (_effectPreviewObject == null)
            {
                var oldPos = characterRoot.position;
                var rootPosition = editorWindow.GetPositionForRootMotion(_effectEvent.FrameIndex, true);
                characterRoot.position = rootPosition;
                var pos = characterRoot.TransformPoint(_effectEvent.Position);
                var rot = characterRoot.eulerAngles + _effectEvent.Rotation;
                
                characterRoot.transform.position = oldPos;
                //实例化特效
                _effectPreviewObject = Object.Instantiate(EffectEvent.Prefab, pos, Quaternion.Euler(rot), EffectTrack.EffectParent);
                _effectPreviewObject.name = EffectEvent.Prefab.name;
                _effectPreviewObject.transform.localScale = EffectEvent.Scale;
            }
            else
            {
                _effectPreviewObject.transform.position = characterRoot.TransformPoint(_effectEvent.Position);
            }
            var particleSystems = _effectPreviewObject.GetComponentsInChildren<ParticleSystem>();
            particleSystems.ForEach(particleSystem =>
            {
                var simulateStartFrame = frameIndex - EffectEvent.FrameIndex;
                particleSystem.Simulate(
                    (float)simulateStartFrame / SkillEditorWindow.Instance.SkillClip.FrameRate);
            });
        }
        else
        {
            CleanEffectPreviewObj();
        }
    }

    #endregion


    public void ApplyModelTransformData()
    {
        if (_effectPreviewObject == null) return;
        var editorWindow = SkillEditorWindow.Instance;
        // 优先使用自定义预览模型，未指定则回退到当前预览角色，避免未赋值导致的异常
        var previewModel = editorWindow.PreviewModel;
        Transform characterRoot = previewModel ? previewModel.transform : editorWindow.CurrentPreviewCharacterObj?.transform;
        if (characterRoot == null)
        {
            CleanEffectPreviewObj();
            return;
        }
        /*var oldPos = characterRoot.position;

        var rootPosition = SkillEditorWindow.Instance.GetPositionForRootMotion(_effectEvent.FrameIndex, true);
        
        characterRoot.position = rootPosition;*/
        EffectEvent.Position = characterRoot.InverseTransformPoint(_effectPreviewObject.transform.position);
        EffectEvent.Rotation = _effectPreviewObject.transform.eulerAngles - characterRoot.eulerAngles;
        EffectEvent.Scale = _effectPreviewObject.transform.localScale;
        
        /*characterRoot.position = oldPos;*/
    }
   
}
