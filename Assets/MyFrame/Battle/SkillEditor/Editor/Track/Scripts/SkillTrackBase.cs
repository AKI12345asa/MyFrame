using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class SkillTrackBase
{

   protected float FrameUnitWidth;
   
   public virtual void Init(VisualElement menuParent, VisualElement trackParent,float frameWidth)
   {
      
      FrameUnitWidth = frameWidth;
     
   }

   public virtual void ResetView()
   {
      ResetView(FrameUnitWidth);
   }

   public virtual void ResetView(float frameWidth)
   {
      FrameUnitWidth = frameWidth;
   }
   
   public virtual void DeleteTrackItem(int frameIndex){}
   
   public virtual void OnConfigChanged(){}

   public virtual void OnPlay(int startFrameIndex) {}

   public virtual void TickView(int frameIndex){}

   public virtual void OnStop(){}
   public virtual void Destroy() {}

   public virtual void DrawGizmos() {}

   public virtual void OnSceneGUI() {}
}
