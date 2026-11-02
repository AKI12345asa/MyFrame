using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeSystemTree.Editor.SpeakTreeView
{
    public class MovePoint : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<MovePoint,UxmlTraits>{}

        public EdgeControl edgeC;

        public MovePoint()
        {
            Image icon = new Image(){name = "MovePoint"};
            this.Add(icon);
            icon.transform.position-=new Vector3(10,10,0);
        }
    
    
    }
}
