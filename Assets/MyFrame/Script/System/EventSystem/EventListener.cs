using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MyFrame
{
    /// <summary>
    /// 事件类型,枚举本质上是int，所以您可以自定义事件，只需要枚举的值和下方不重复即可
    /// </summary>
    public enum EventType
    {
        OnMouseEnter = -10001,
        OnMouseExit = -10002,
        OnClick = -10003,
        OnClickDown = -10004,
        OnClickUp = -10005,
        OnDrag = -10006,
        OnBeginDrag = -10007,
        OnEndDrag = -10008,
        OnCollisionEnter = -10009,
        OnCollisionStay = -10010,
        OnCollisionExit = -10011,
        OnCollisionEnter2D = -10012,
        OnCollisionStay2D = -10013,
        OnCollisionExit2D = -10014,
        OnTriggerEnter = -10015,
        OnTriggerStay = -10016,
        OnTriggerExit = -10017,
        OnTriggerEnter2D = -10018,
        OnTriggerStay2D = -10019,
        OnTriggerExit2D = -10020,
        OnReleaseAddressableAsset = -10021,
        OnDestroy = -10022,
    }
    
    public class EventListener: MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        private static ObjectPoolModule poolModul = new ObjectPoolModule();
        #region 内部类、接口等
        /// <summary>
        /// 持有关键字典数据，主要用于将这个引用放入对象池中
        /// </summary>
        private class EventListenerData
        {
            public Dictionary<int, IEventListenerEventInfos> eventInfoDic = new ();
        }


        private interface IEventListenerEventInfo<T>
        {
            void TriggerEvent(T eventData);
            void Destory();
        }

        /// <summary>
        /// 某个事件中一个事件的数据包装类
        /// </summary>
        private class EventListenerEventInfo<T, TEventArg> : IEventListenerEventInfo<T>
        {
            public Action<T, TEventArg> action;
            public TEventArg arg;
            public void Init(Action<T, TEventArg> action, TEventArg args = default(TEventArg))
            {
                this.action = action;
                this.arg = args;
            }
            public void Destory()
            {
                this.action = null;
                this.arg = default(TEventArg);
                poolModul.PushObject(this);
            }
            public void TriggerEvent(T eventData)
            {
                action?.Invoke(eventData, arg);
            }
        }


        /// <summary>
        /// 某个事件中一个事件的数据包装类（无参）
        /// </summary>
        private class EventListenerEventInfo<T> : IEventListenerEventInfo<T>
        {
            public Action<T> action;
            public void Init(Action<T> action)
            {
                this.action = action;
            }
            public void Destory()
            {
                this.action = null;
                poolModul.PushObject(this);
            }
            public void TriggerEvent(T eventData)
            {
                action?.Invoke(eventData);
            }
        }


        interface IEventListenerEventInfos
        {
            void RemoveAll();

        }

        /// <summary>
        /// 一类事件的数据包装类型：包含多个EventListenerEventInfo
        /// </summary>
        private class EventListenerEventInfos<T> : IEventListenerEventInfos
        {
            // 所有的事件
            private List<IEventListenerEventInfo<T>> eventList = new List<IEventListenerEventInfo<T>>();


            /// <summary>
            /// 添加事件 无参
            /// </summary>
            public void AddListener(Action<T> action)
            {
                EventListenerEventInfo<T> info = poolModul.GetObject<EventListenerEventInfo<T>>();
                if (info == null) info = new EventListenerEventInfo<T>();
                info.Init(action);
                eventList.Add(info);
            }

            /// <summary>
            /// 添加事件 有参
            /// </summary>
            public void AddListener<TEventArg>(Action<T, TEventArg> action, TEventArg args = default(TEventArg))
            {
                EventListenerEventInfo<T, TEventArg> info = poolModul.GetObject<EventListenerEventInfo<T, TEventArg>>();
                if (info == null) info = new EventListenerEventInfo<T, TEventArg>();
                info.Init(action, args);
                eventList.Add(info);
            }

            public void TriggerEvent(T evetData)
            {
                for (int i = 0; i < eventList.Count; i++)
                {
                    eventList[i].TriggerEvent(evetData);
                }
            }


            /// <summary>
            /// 移除事件（无参）
            /// 同一个函数+参数注册过多次，无论如何该方法只会移除一个事件
            /// </summary>
            public void RemoveListener(Action<T> action)
            {
                for (int i = 0; i < eventList.Count; i++)
                {
                    EventListenerEventInfo<T> eventInfo = eventList[i] as EventListenerEventInfo<T>;
                    if (eventInfo == null) continue; // 类型不符

                    // 找到这个事件，查看是否相等
                    if (eventInfo.action.Equals(action))
                    {
                        // 移除
                        eventInfo.Destory();
                        eventList.RemoveAt(i);
                        return;
                    }
                }
            }

            /// <summary>
            /// 移除事件（有参）
            /// 同一个函数+参数注册过多次，无论如何该方法只会移除一个事件
            /// </summary>
            public void RemoveListener<TEventArg>(Action<T, TEventArg> action, TEventArg args = default(TEventArg))
            {
                for (int i = 0; i < eventList.Count; i++)
                {
                    EventListenerEventInfo<T, TEventArg> eventInfo = eventList[i] as EventListenerEventInfo<T, TEventArg>;
                    if (eventInfo == null) continue; // 类型不符

                    // 找到这个事件，查看是否相等
                    if (eventInfo.action.Equals(action))
                    {
                        // 移除
                        eventInfo.Destory();
                        eventList.RemoveAt(i);
                        return;
                    }
                }
            }

            /// <summary>
            /// 移除全部，全部放进对象池
            /// </summary>
            public void RemoveAll()
            {
                for (int i = 0; i < eventList.Count; i++)
                {
                    eventList[i].Destory();
                }
                eventList.Clear();
                poolModul.PushObject(this);
            }
        }

        #endregion

        private EventListenerData data;
        private EventListenerData Data
        {
            get
            {
                if (data == null)
                {
                    data = poolModul.GetObject<EventListenerData>();
                    if (data == null) data = new EventListenerData();
                }
                return data;
            }
        }

        #region 外部的访问
        /// <summary>
        /// 添加无参事件 
        /// </summary>
        public void AddListener<T>(int eventTypeInt, Action<T> action)
        {
            if (Data.eventInfoDic.TryGetValue(eventTypeInt, out IEventListenerEventInfos info))
            {
                ((EventListenerEventInfos<T>)info).AddListener(action);
            }
            else
            {
                EventListenerEventInfos<T> infos = poolModul.GetObject<EventListenerEventInfos<T>>();
                if (infos == null) infos = new EventListenerEventInfos<T>();
                infos.AddListener(action);
                Data.eventInfoDic.Add(eventTypeInt, infos);
            }
        }

        /// <summary>
        /// 添加事件（有参）
        /// </summary>
        public void AddListener<T, TEventArg>(int eventTypeInt, Action<T, TEventArg> action, TEventArg args)
        {
            if (Data.eventInfoDic.TryGetValue(eventTypeInt, out IEventListenerEventInfos info))
            {
                ((EventListenerEventInfos<T>)info).AddListener(action, args);
            }
            else
            {
                EventListenerEventInfos<T> infos = poolModul.GetObject<EventListenerEventInfos<T>>();
                if (infos == null) infos = new EventListenerEventInfos<T>();
                infos.AddListener(action, args);
                Data.eventInfoDic.Add(eventTypeInt, infos);
            }
        }


        /// <summary>
        /// 添加事件（无参）
        /// </summary>
        public void AddListener<T>(EventType eventType, Action<T> action)
        {
            AddListener((int)eventType, action);
        }
        /// <summary>
        /// 添加事件（有参）
        /// </summary>
        public void AddListener<T, TEventArg>(EventType eventType, Action<T, TEventArg> action, TEventArg args)
        {
            AddListener((int)eventType, action, args);
        }


        /// <summary>
        /// 移除事件（无参）
        /// </summary>
        public void RemoveListener<T>(int eventTypeInt, Action<T> action)
        {
            if (Data.eventInfoDic.TryGetValue(eventTypeInt, out IEventListenerEventInfos info))
            {
                ((EventListenerEventInfos<T>)info).RemoveListener(action);
            }
        }
        /// <summary>
        /// 移除事件（无参）
        /// </summary>
        public void RemoveListener<T>(EventType eventType, Action<T> action)
        {
            RemoveListener((int)eventType, action);
        }


        /// <summary>
        /// 移除事件（有参）
        /// </summary>
        public void RemoveListener<T, TEventArg>(int eventTypeInt, Action<T, TEventArg> action)
        {
            if (Data.eventInfoDic.TryGetValue(eventTypeInt, out IEventListenerEventInfos info))
            {
                ((EventListenerEventInfos<T>)info).RemoveListener(action);
            }
        }
        /// <summary>
        /// 移除事件（有参）
        /// </summary>
        public void RemoveListener<T, TEventArg>(EventType eventType, Action<T, TEventArg> action)
        {
            RemoveListener((int)eventType, action);
        }

        /// <summary>
        /// 移除某一个事件类型下的全部事件
        /// </summary>
        public void RemoveAllListener(EventType eventType)
        {
            RemoveAllListener((int)eventType);
        }

        /// <summary>
        /// 移除某一个事件类型下的全部事件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventType"></param>
        public void RemoveAllListener(int eventType)
        {
            if (Data.eventInfoDic.TryGetValue(eventType, out IEventListenerEventInfos infos))
            {
                infos.RemoveAll();
                Data.eventInfoDic.Remove(eventType);
            }
        }

        /// <summary>
        /// 移除全部事件
        /// </summary>
        public void RemoveAllListener()
        {
            foreach (IEventListenerEventInfos infos in Data.eventInfoDic.Values)
            {
                infos.RemoveAll();
            }

            data.eventInfoDic.Clear();
            // 将整个数据容器放入对象池
            poolModul.PushObject(data);
            data = null;
        }

        #endregion
        /// <summary>
        /// 触发事件
        /// </summary>
        public void TriggerAction<T>(int eventTypeInt, T eventData)
        {
            if (Data.eventInfoDic.TryGetValue(eventTypeInt, out IEventListenerEventInfos infos))
            {
                (infos as EventListenerEventInfos<T>).TriggerEvent(eventData);
            }
        }
        /// <summary>
        /// 触发事件
        /// </summary>
        public void TriggerAction<T>(EventType eventType, T eventData)
        {
            TriggerAction<T>((int)eventType, eventData);
        }

        #region 鼠标事件
        public void OnPointerEnter(PointerEventData eventData)
        {
            TriggerAction(EventType.OnMouseEnter, eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TriggerAction(EventType.OnMouseExit, eventData);
        }


        public void OnBeginDrag(PointerEventData eventData)
        {
            TriggerAction(EventType.OnBeginDrag, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            TriggerAction(EventType.OnDrag, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            TriggerAction(EventType.OnEndDrag, eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            TriggerAction(EventType.OnClick, eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            TriggerAction(EventType.OnClickDown, eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            TriggerAction(EventType.OnClickUp, eventData);
        }
        #endregion

        #region 碰撞事件
        private void OnCollisionEnter(Collision collision)
        {
            TriggerAction(EventType.OnCollisionEnter, collision);
        }
        private void OnCollisionStay(Collision collision)
        {
            TriggerAction(EventType.OnCollisionStay, collision);
        }
        private void OnCollisionExit(Collision collision)
        {
            TriggerAction(EventType.OnCollisionExit, collision);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TriggerAction(EventType.OnCollisionEnter2D, collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TriggerAction(EventType.OnCollisionStay2D, collision);
        }
        private void OnCollisionExit2D(Collision2D collision)
        {
            TriggerAction(EventType.OnCollisionExit2D, collision);
        }
        #endregion

        #region 触发事件
        private void OnTriggerEnter(Collider other)
        {
            TriggerAction(EventType.OnTriggerEnter, other);
        }
        private void OnTriggerStay(Collider other)
        {
            TriggerAction(EventType.OnTriggerStay, other);
        }
        private void OnTriggerExit(Collider other)
        {
            TriggerAction(EventType.OnTriggerExit, other);
        }
        private void OnTriggerEnter2D(Collider2D collision)
        {
            TriggerAction(EventType.OnTriggerEnter2D, collision);
        }
        private void OnTriggerStay2D(Collider2D collision)
        {
            TriggerAction(EventType.OnTriggerStay2D, collision);
        }
        private void OnTriggerExit2D(Collider2D collision)
        {
            TriggerAction(EventType.OnTriggerExit2D, collision);
        }
        #endregion

        #region 销毁事件
        private void OnDestroy()
        {
            TriggerAction(EventType.OnReleaseAddressableAsset, gameObject);
            TriggerAction(EventType.OnDestroy, gameObject);

            // 销毁所有数据，并将一些数据放回对象池中
            RemoveAllListener();
        }
        #endregion
    }
}
