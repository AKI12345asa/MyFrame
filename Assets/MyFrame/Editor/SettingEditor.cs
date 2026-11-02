using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace MyFrame.Editor
{
    [CustomEditor(typeof(MyFrameSetting))]
    public class SettingEditor: OdinEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (FrameWindow.Window != null)
            {
                Debug.LogWarning("当前窗口已经开启");
                return;
            }
        }
    }
}