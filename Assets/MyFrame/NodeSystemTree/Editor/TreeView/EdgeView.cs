using NodeSystemTree.Editor.SpeakTreeView;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public enum EdgeState
{
    Activated,
    InActivated,
    
}
namespace SpeakSystemTree.Editor.SpeakTreeView
{
    
    public class EdgeView : Edge
    {
        private VisualElement[] movePoints;

        private int _stepIndex;

        private int _pointNumber = 4;
        
        private EdgeState _edgeState = EdgeState.InActivated;
        
        public EdgeView(Edge edge)
        { 
            movePoints = new VisualElement[_pointNumber];
           input = edge.input;
           output = edge.output;
        }

        public EdgeView(Port inputPort, Port outputPort)
        {
            movePoints = new VisualElement[_pointNumber];
            input = inputPort;
            output = outputPort;
        }

        public void OnStartMovePoint()
        {
            if (_edgeState == EdgeState.Activated) return;
            if(_edgeState == EdgeState.InActivated)
            {
                for (int i = 0; i < _pointNumber; i++)
                {
                    movePoints[i] = new MovePoint();
                    Add(movePoints[i]);
                }
                _stepIndex = 0;
            }
            _edgeState = EdgeState.Activated;
            /*MovePoints();*/
        }

        /*private async void MovePoints()
        {
            while (_edgeState == EdgeState.Activated)
            {
                _stepIndex %= 100;
                for (int i = 0; i < _pointNumber; i++)
                {
                    var t = (float)i / _pointNumber + _stepIndex / 100f;
                    t = t >= 1 ? t - 1 : t;
                    movePoints[i].transform.position =  Vector2.Lerp(edgeControl.controlPoints[1],
                            edgeControl.controlPoints[2], t);
                }
                _stepIndex++;
                await UniTask.Delay(10);
            }
        }*/
        
        public void OnStopMovePoint()
        {
            if (_edgeState == EdgeState.InActivated) return;
            for (int i = movePoints.Length-1; i >= 0; i--)
            {
                if (movePoints[i] != null)
                {
                    Remove(movePoints[i]);
                    movePoints[i] = null;
                }
            }
            _edgeState = EdgeState.InActivated;
        }
    }
}
