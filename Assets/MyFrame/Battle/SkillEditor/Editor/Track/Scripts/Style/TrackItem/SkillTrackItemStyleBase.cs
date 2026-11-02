using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class SkillTrackItemStyleBase
{
    public VisualElement Root { get; protected set; }

    public virtual void SetBGColor(Color color)
    {
        Root.style.backgroundColor = color;
    }

    public virtual void SetWidth(float width)
    { 
        Root.style.width = width;
    }

    public virtual void SetPositionX(float x)
    {
        var pos = Root.transform.position;
        pos.x = x;
        Root.transform.position = pos;
    }
    
  
}
