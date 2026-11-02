using System;

namespace NodeTreeViewTool
{
    [AttributeUsage(AttributeTargets.Field)]
    public class OpenNodeViewAttribute : Attribute
    {
        public string MuneName;
        
        public OpenNodeViewAttribute(string muneName)
        {
            MuneName = muneName;
        }
    }
}
