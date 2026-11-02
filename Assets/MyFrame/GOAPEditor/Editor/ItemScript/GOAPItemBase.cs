using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class GOAPItemBase
{
   protected Action<GOAPItemBase> OnDelete;
   protected Action<GOAPItemBase> OnSelect;
   public const int ItemDistance = 50;
   protected Label Root;
   protected Button DeleteButton;
   public virtual void Init(Action<GOAPItemBase> onDelete,Action<GOAPItemBase> onMouseDown,string path,VisualElement parent)
   {
      OnDelete = onDelete;
      Root = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path).Instantiate().Query<Label>();
      DeleteButton = Root.Q<Button>("DeleteButton");
      DeleteButton.clicked += () => { OnDelete?.Invoke(this); };
      parent.Add(Root);
      OnSelect = onMouseDown;
   }

   public abstract void Selected();
   
   public abstract void UnSelected();

   public virtual void SetIndex(int index)
   {
      var pos = Root.transform.position;
      pos.y = index * ItemDistance;
      Root.transform.position = pos;
   }

   public virtual void RefreshItem()
   {
      
   }
   
   public virtual void SetItemName(string name)
   {
      Root.text = name;
   }
}
