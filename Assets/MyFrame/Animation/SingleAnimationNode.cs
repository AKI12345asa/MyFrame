using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// 单个动画片段播放节点。
 /// </summary>
    public class SingleAnimationNode:AnimationNodeBase
    {
        private AnimationClipPlayable _clipPlayable;
        /// <summary>当前动画片段的归一化播放时间，范围通常为 0 到 1。</summary>
        public float NormalizedTime => (float)(_clipPlayable.GetTime() / _clipPlayable.GetAnimationClip().length);

        /// <summary>初始化单片段节点并将其接入动画播放图。</summary>
        /// <param name="animationClip">要播放的动画片段。</param>
        /// <param name="speed">播放速度倍率。</param>
        /// <param name="inputPort">父混合器输入端口。</param>
        /// <param name="isTransitioning">是否由控制器在过渡阶段稍后接入播放图。</param>
        /// <param name="defaultDeviation">初始播放时间在动画总时长中的比例。</param>
        /// <param name="controller">动画控制器</param>
        public void Init(PlayableGraph graph,AnimationMixerPlayable outputMixer,AnimationClip animationClip,float speed,int inputPort,bool isTransitioning = false,float defaultDeviation = 0)
        {
            _clipPlayable = AnimationClipPlayable.Create(graph,animationClip);
            Speed = speed;
            _clipPlayable.SetSpeed(speed);
            defaultDeviation = Mathf.Clamp(defaultDeviation, 0, 1);
            _clipPlayable.SetTime(animationClip.length * defaultDeviation);
            InputPort = inputPort;
            ConnectToPlayableGraph(_clipPlayable, graph, outputMixer, 0,isTransitioning);   
        }
        
        /// <summary>获取此节点正在播放的动画片段。</summary>
        public AnimationClip GetAnimationClip()
        {
            return _clipPlayable.GetAnimationClip();
        }
        
        /// <summary>将动画片段 Playable 接入父混合器。</summary>
        public override void ConnectToPlayableGraph<V>(V outputMixer, PlayableGraph graph, bool isTransitioning)
        {
            if (isTransitioning) return;
            ConnectToPlayableGraph(_clipPlayable, graph, outputMixer, 0,false);
        }

        /// <summary>设置动画片段的播放速度倍率。</summary>
        public override void SetClipSpeed(float speed)
        {
            _clipPlayable.SetSpeed(speed * Speed);
        }
        
    }