using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class AnimationTrack : SkillTrackBase
{
    private SkillSingleLineTrackStyle _trackStyle;
    
    private Dictionary<int, AnimationTrackItem> _trackItemDic = new();

    public SkillAnimationData AnimationData => SkillEditorWindow.Instance.SkillClip.SkillAnimationData;
    
    
    public override void Init(VisualElement menuParent, VisualElement trackParent, float frameWidth)
    {
        base.Init(menuParent, trackParent,frameWidth);
        _trackStyle = new SkillSingleLineTrackStyle();
        _trackStyle.Init(menuParent, trackParent, "动画配置");
        _trackStyle.RegisterContentCallback<DragUpdatedEvent>(OnDragUpdate);
        _trackStyle.RegisterContentCallback<DragExitedEvent>(OnDragExited);
        ResetView();
    }

    public override void ResetView(float frameWidth)
    {
        base.ResetView(frameWidth);
        //清空原有的视图
        _trackItemDic.ForEach(item =>
        {
            _trackStyle.DeleteItem(item.Value.ItemStyle.Root);
        });
        _trackItemDic.Clear();
        if (SkillEditorWindow.Instance.SkillClip == null) return;
        //根据新数据绘制轨道
        SkillEditorWindow.Instance.SkillClip.SkillAnimationData.FrameData.ForEach(item =>
        {
           CreateItem(item.Key,item.Value);
        });
    }

    private void CreateItem(int frameIndex, SkillAnimationEvent skillAnimationEvent)
    {
        var trackItem = new AnimationTrackItem();
        trackItem.Init(this, _trackStyle, frameIndex, FrameUnitWidth, skillAnimationEvent);
        _trackItemDic.Add(frameIndex, trackItem);
    }

    #region 资源的拖拽

        private void OnDragUpdate(DragUpdatedEvent evt)
    {
        //监听当前用户拖拽的是否是动画片段
        var objs = DragAndDrop.objectReferences;
        if (objs[0] is AnimationClip clip)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }
  
    }
    private void OnDragExited(DragExitedEvent evt)
    {
        var objs = DragAndDrop.objectReferences;
        if (objs[0] is AnimationClip clip)
        {
            //放置动画资源
           var selectFrameIndex = SkillEditorWindow.Instance.GetFrameIndexByPos(evt.localMousePosition.x);//轨道的本地坐标有考虑偏移值的影响所以直接根据本地坐标计算即可
           var canPlace = true;
           var durationFrame = -1;//-1代表可用原本的AnimationClip的持续时间
           var clipFrameCount = (int)(clip.length * clip.frameRate);
           var nextTrack = -1;
           var currentOffset = int.MaxValue;

           foreach (var item in SkillEditorWindow.Instance.SkillClip.SkillAnimationData.FrameData)
           {
               //不允许选中帧在TrackItem之间(动画时间的起点到它的终点之间)
               if (selectFrameIndex > item.Key && selectFrameIndex < item.Value.DurationFrame + item.Key)
               {
                   //不可放置
                   canPlace = false;
                   break;
               }
               //找到最右侧的TrackItem
               if (item.Key > selectFrameIndex)
               {
                   var tempOffset = item.Key - selectFrameIndex;
                   if (tempOffset < currentOffset)
                   {
                       currentOffset = tempOffset;
                       nextTrack = item.Key;
                   }
               }
           }
            //实际的放置逻辑
           if (canPlace)
           {
               //右边右其他的TrackItem,要考虑Track不能重叠的问题 
               if (nextTrack != -1)
               {
                   var offset = clipFrameCount - currentOffset;
                   durationFrame = offset < 0? clipFrameCount : currentOffset;
               }
               //右边啥都没有
               else durationFrame = clipFrameCount;
               
               //构建该动画片段的对应动画数据
               var animationEvent = new SkillAnimationEvent()
               {
                   AnimationClip = clip,
                   DurationFrame = durationFrame,
                   TransitionTime = 0.25f
               };
           
               //保存新增的动画数据
               SkillEditorWindow.Instance.SkillClip.SkillAnimationData.FrameData.TryAdd(selectFrameIndex, animationEvent);
               SkillEditorWindow.Instance.SaveConfig();
               
               //绘制一个新的Item
               CreateItem(selectFrameIndex,animationEvent);
           }

           
           
           
        }
    }

    #endregion

    #region 起始帧校验

    public bool CheckFrameIndexOnDrag( int targetIndex,int startIndex = -1)
    {
        foreach (var item in AnimationData.FrameData)
        {
            if(item.Key == startIndex)continue;
            if (targetIndex > item.Key && targetIndex < item.Value.DurationFrame + item.Key)
            {
                return false;
            }
        }

        return true;
    }

    public bool CheckFrameIndexOnDrag(int startIndex, int endIndex,int originalIndex)
    {
        foreach (var item in AnimationData.FrameData)
        {
            if(item.Key == originalIndex) continue;
            if (item.Key >= startIndex && item.Key + item.Value.DurationFrame <= endIndex)
            {
                return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// 更新当前动画的索引
    /// </summary>
    /// <param name="oldIndex"></param>
    /// <param name="newIndex"></param>
    public void SetFrameIndex(int oldIndex, int newIndex)
    {
        if (AnimationData.FrameData.Remove(oldIndex,
                out SkillAnimationEvent skillAnimationEvent))
        {
            if(!AnimationData.FrameData.TryGetValue(newIndex,out _))
            {
                AnimationData.FrameData.Add(newIndex, skillAnimationEvent);
                _trackItemDic.Remove(oldIndex,out var trackItem);
                _trackItemDic.Add(newIndex, trackItem);
                SkillEditorWindow.Instance.SaveConfig();
            }
        }
    }
    #endregion
    public override void DeleteTrackItem(int frameIndex)
    {
        AnimationData.FrameData.Remove(frameIndex);//从保存数据中删除
        if (_trackItemDic.Remove(frameIndex, out var trackItem))
        {
            _trackStyle.DeleteItem(trackItem.ItemStyle.Root);
        }//从视图中删除
        SkillEditorWindow.Instance.SaveConfig();
    }

    public override void OnConfigChanged()
    {
        _trackItemDic.ForEach(item =>
        {
            item.Value.OnConfigChanged();
        });
    }

    public Vector3 GetPositionForRootMotion(int frameIndex, bool recover = false)
    { 
        var previewGameObject = SkillEditorWindow.Instance.CurrentPreviewCharacterObj;
        GameObject playerModel = SkillEditorWindow.Instance.PreviewModel;
        if (playerModel == null) playerModel = previewGameObject;
        var animator = previewGameObject.GetComponentInChildren<Animator>();
        //根据帧找到目前是哪个动画
        var frameData = AnimationData.FrameData;

        #region 关于根运动的计算

        var frameDataSortedDic = new SortedDictionary<int, SkillAnimationEvent>(frameData);
        var keys = frameDataSortedDic.Keys.ToArray();
        var rootMotionTotalPos = Vector3.zero;

       for(var i = 0;i < keys.Length;i++)
       {
           var key = keys[i];//当前动画的起始帧数
           var animationEvent = frameDataSortedDic[key];
           //只考虑跟运动配置的动画
           if (!animationEvent.ApplyRootMotion) continue;

         

           var nextKeyFrame = i + 1 < keys.Length ? keys[i + 1] : SkillEditorWindow.Instance.SkillClip.FrameCount;

           var isBreak = false;

           if (nextKeyFrame > frameIndex)//通过判断当前帧是否到达下一个动画的起始帧来决定是否继续采样
           {
               nextKeyFrame = frameIndex;
               isBreak = true;
           }
           //持续的帧数 = 下一个动画的帧数-这个动画的开始时间
           var durationFrameCount = nextKeyFrame - key;
           if (durationFrameCount > 0)
           {
               #region 进度计算
               //动画总帧数
               var clipFrameCount = animationEvent.AnimationClip.length * SkillEditorWindow.Instance.SkillClip.FrameRate;
               //计算当前的播放进度
               var totalProgress = durationFrameCount * animationEvent.GetAnimationSpeed() / clipFrameCount;
               
               //播放次数
               var playTimes = 0;
               //最终不完整的一次播放进度
               float lastProgress = 0;
               //只有循环动画才需要采样多次
               if (animationEvent.AnimationClip.isLooping)
               {
                   playTimes = (int)totalProgress;
                   lastProgress = totalProgress - (int)totalProgress;
               }
               //不循环的动画，播放进度超过1，则约束为1
               else
               {
                   playTimes = totalProgress >= 1? 1 : 0;
                   lastProgress = totalProgress >= 1? 0 : totalProgress;
               }
               #endregion
               
               #region 根运动采样计算

               animator.applyRootMotion = true;
               if (playTimes >= 1)
               {
                   //采样一次完整的进度
                   animationEvent.AnimationClip.SampleAnimation(playerModel,animationEvent.AnimationClip.length);
                   var samplePos = playerModel.transform.position;
                   rootMotionTotalPos += samplePos * playTimes;
               }

               if (lastProgress > 0)
               {
                   //采样一次不完整的进度
                   animationEvent.AnimationClip.SampleAnimation(playerModel,lastProgress * animationEvent.AnimationClip.length);
                   rootMotionTotalPos += playerModel.transform.position;
               }
                
               #endregion
               
             
           }
           
           if(isBreak)break;
       }
       
        #endregion

        if (recover)
        {
            UpdatePosture(SkillEditorWindow.Instance.CurrentSelectFrameIndex);
        }
        
        return rootMotionTotalPos;
    }
    
    private void UpdatePosture(int frameIndex)
    {
        var previewGameObject = SkillEditorWindow.Instance.CurrentPreviewCharacterObj;
        var animator = previewGameObject.GetComponentInChildren<Animator>();
        //根据帧找到目前是哪个动画
        var frameData = AnimationData.FrameData;
        
        #region 关于当前帧的姿态

        //找到距离这一帧左边最近的一个动画，也就是当前要播放的动画 
        var currentOffset = int.MaxValue;//最近的索引距离当前的选中帧的差距
        var animationEventIndex = -1;
        foreach (var item in frameData)
        { 
            var temp = frameIndex - item.Key;
            if (temp >= 0 && temp < currentOffset)
            {
                currentOffset = temp;
                animationEventIndex = item.Key;
            }
        }
        if (animationEventIndex != -1)
        {
            var animationEvent = frameData[animationEventIndex];
            //动画总帧数
            var clipFrameCount = animationEvent.AnimationClip.length * SkillEditorWindow.Instance.SkillClip.FrameRate;
            //计算当前的播放进度
            var progress = currentOffset * animationEvent.GetAnimationSpeed() / clipFrameCount;
            //循环动画的处理
            if (animationEvent.AnimationClip.isLooping)
            {
                progress %= 1f;
            }
            else
            {
                progress = Mathf.Clamp01(progress);
            }
            animator.applyRootMotion = animationEvent.AnimationClip.hasRootCurves;
            var skillPlayer = previewGameObject.GetComponentInChildren<Skill_Player>();
            GameObject playerModel = SkillEditorWindow.Instance.PreviewModel;
            if (playerModel == null) playerModel = previewGameObject;
            skillPlayer.SetMainWeaponHand(animationEvent.MainWeaponOnLeftHand);
            animationEvent.AnimationClip.SampleAnimation(playerModel, progress * animationEvent.AnimationClip.length);
        }
        #endregion
    }
    public override void TickView(int frameIndex)
    {
        var previewGameObject = SkillEditorWindow.Instance.CurrentPreviewCharacterObj;
        GameObject playerModel = SkillEditorWindow.Instance.PreviewModel;
        if (playerModel == null) playerModel = previewGameObject;
        if (previewGameObject == null) return;
        //更新姿态
        UpdatePosture(frameIndex);//该姿态处理仅会作用于取消了根运动的动画，因为在下层计算其跟运动时的采样操作被跳过了因为其取消勾选了应用根运动
        playerModel.transform.position = GetPositionForRootMotion(frameIndex);
    }

    public override void Destroy()
    {
        _trackStyle.Destroy();
    }
}
