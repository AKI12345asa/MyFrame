using System;
using UnityEngine.UIElements;

namespace MyFrame.Editor
{
    public abstract class FrameConfigInspectorBase
    {
        public abstract void Init(FrameConfigBase frameConfig);
        
        public abstract VisualElement GetInspectorContent();
    }

    public abstract class FrameConfigInspectorBase<T> : FrameConfigInspectorBase where T : FrameConfigBase
    {
        protected T Config;
        protected VisualElement Root;

        public override void Init(FrameConfigBase frameConfig)
        {
            if(frameConfig is not T config) throw new Exception($"类型匹配失败请检查{typeof(T).Name}与{frameConfig.GetType().Name}");
            Config = config;
            Root = new VisualElement();
        }

        public override VisualElement GetInspectorContent()
        {
            return Root;
        }
    }
}