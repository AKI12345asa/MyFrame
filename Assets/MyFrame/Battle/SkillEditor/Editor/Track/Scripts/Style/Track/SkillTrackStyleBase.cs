using System;
using UnityEngine;
using UnityEngine.UIElements;
public abstract class SkillTrackStyleBase
{
   public Label TitleLabel;

   #region 自身的父节点
   protected VisualElement MenuParent;
   protected VisualElement ContentParent;
   #endregion

   #region 自身的根节点
   protected VisualElement MenuRoot;
   protected VisualElement ContentRoot;
   #endregion


   public virtual void AddItem(VisualElement ve)
   {
      ContentRoot.Add(ve);
   }

   public virtual void DeleteItem(VisualElement ve)
   {
      ContentRoot.Remove(ve);
   }

   public virtual void RegisterContentCallback<T>(EventCallback<T> callback) where T : EventBase<T>, new()
   {
      ContentRoot.RegisterCallback(callback);
   }
   
   public virtual void RegisterMenuCallback<T>(EventCallback<T> callback) where T : EventBase<T>, new()
   {
      MenuRoot.RegisterCallback(callback);
   }

   public virtual void Destroy()
   {
      if(MenuRoot != null) MenuParent.Remove(MenuRoot);
      if(ContentRoot != null) ContentParent.Remove(ContentRoot);
   }
}
