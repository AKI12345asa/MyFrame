using System;

namespace MyFrame
{
    public class FrameConfigAttribute:Attribute
    {
        public Type ConfigType;
        public string ConfigGroupName;

        public FrameConfigAttribute(Type configType, string configGroupName)
        {
            ConfigType = configType;
            ConfigGroupName = configGroupName;
        }
    }
    
    public class FrameConfigInspectorAttribute:Attribute
    {
        public Type ConfigType;
        public Type InspectorType;

        public FrameConfigInspectorAttribute(Type configType,Type inspectorType)
        {
            ConfigType = configType;
            InspectorType = inspectorType;
        }
    }
    
    
}