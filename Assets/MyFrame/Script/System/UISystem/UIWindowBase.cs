using System;
using UnityEngine;

namespace MyFrame
{
    public class UIWindowBase:MonoBehaviour
    {
        protected bool uiEnabled;
        public bool UIEnabled =>  uiEnabled;
        protected int currentLayer;
        public int CurrentLayer =>  currentLayer;

        public virtual void Init(){}
        
        public virtual void OnShow(){}
        
        public virtual void OnClose(){}

        public void ShowGeneralLogic(int layerNum)
        {
            currentLayer = layerNum;
            OnShow();
            gameObject.SetActive(true);
            uiEnabled = true;
        }

        public void CloseGeneralLogic()
        {
            OnClose();
            gameObject.SetActive(false);
            uiEnabled = false;
        }
    }
}
