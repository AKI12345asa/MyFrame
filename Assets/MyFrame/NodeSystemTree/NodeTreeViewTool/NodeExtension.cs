using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;
using SerializationUtility = Sirenix.Serialization.SerializationUtility;

namespace NodeTreeViewTool
{
    public static class NodeExtension
    {
        public static List<INodeViewer> Clone(this List<INodeViewer> nodes)
        {
            byte[] nodeBytes= SerializationUtility.SerializeValue(nodes, DataFormat.Binary);
            var toNode = SerializationUtility.DeserializeValue<List<INodeViewer>>(nodeBytes ,DataFormat.Binary);
            foreach (var node in toNode)
            {
                node.GUID = GUID.Generate().ToString();
           
                if(node.GetOutPutList(out var outputNodes))
                {
                    outputNodes = outputNodes.Intersect(toNode).ToList();
                    node.ChangeCurrentList(outputNodes);
                }

                node.Position += Vector2.one * 30;
            }

            return toNode;
        }
        #region Attribute

        public class NodeLabelAttribute : Attribute
        {
            /// <summary>
            /// 菜单名称
            /// </summary>
            public string MenuName;
            /// <summary>
            /// 标题名称
            /// </summary>
            public string Label;
            
            /// <summary>
            /// 是否为子类
            /// </summary>
            public bool IsSubClass = false;
            
            public NodeLabelAttribute(string menuName) =>MenuName = menuName;
        }

        #endregion
    }
  
}
