using System;
using UnityEngine;
using UnityEngine.UI;
namespace MyFrame
{
    public class UILayout:MonoBehaviour
    {
        public Transform root;
        public bool enableMask = true;
        public Image maskImage;
        private int count = 0;

        public void Init(UILayoutData data)
        {
            enableMask = data.enableMask;
            maskImage.gameObject.SetActive(enableMask);
            maskImage.sprite = data.maskSprite;
            maskImage.color = data.maskColor;
            maskImage.raycastTarget = data.ifBlockRayCast;
        }
        
        public void OnWindowShow()
        {
            count += 1;
            Update();
        }
        public void OnWindowClose()
        {
            count -= 1;
            Update();
        }
        /// <summary>
        /// 更新Mask的层级和遮挡状态，保证Mask可以起到遮挡交互的作用，同时又不遮挡UI元素的显示
        /// </summary>
        private void Update()
        {
            if (enableMask == false) return;
            int posIndex = root.childCount - 2;
            maskImage.transform.SetSiblingIndex(posIndex < 0 ? 0 : posIndex);
        }
        public void Reset()
        {
            count = 0;
            Update();
        }
    }
}
