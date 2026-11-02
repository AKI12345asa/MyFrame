using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TwoSpanView : TwoPaneSplitView
{
    public new class UxmlFactory : UxmlFactory<TwoSpanView, UxmlTraits>{}
    
    public TwoSpanView()
    {
        Init();
    }
    
    public void Init(){}
    
}
