using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace NodeSystemTree.Editor.SpeakTreeView
{
    [CreateAssetMenu(fileName = "InspectorData",menuName = "UI/SpeakSystem/InspectorData")]
    public class InspectorDataView : SerializedScriptableObject
    {
        [OdinSerialize,HideReferenceObjectPicker,LabelText("当前选择的点")][ListDrawerSettings(IsReadOnly = true)]
        public HashSet<INodeViewer> SelectedDatas = new();
        
        public InspectorDataView()
        {
            SelectedDatas = new  HashSet<INodeViewer>();
        }
    }
}
