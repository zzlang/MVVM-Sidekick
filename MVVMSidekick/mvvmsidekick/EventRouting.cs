﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Input;
using MVVMSidekick.ViewModels;
using MVVMSidekick.Commands;
using System.Runtime.CompilerServices;
using MVVMSidekick.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Collections;

#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Controls;
using System.Collections.Concurrent;
using Windows.UI.Xaml.Navigation;

using Windows.UI.Xaml.Controls.Primitives;

#elif WPF
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.Concurrent;
using System.Windows.Navigation;

using MVVMSidekick.Views;
using System.Windows.Controls.Primitives;
using MVVMSidekick.Utilities;

#elif SILVERLIGHT_5||SILVERLIGHT_4
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Navigation;
using System.Windows.Controls.Primitives;
#elif WINDOWS_PHONE_8||WINDOWS_PHONE_7
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using System.Windows.Data;
using System.Windows.Navigation;
using System.Windows.Controls.Primitives;
#endif





namespace MVVMSidekick
{
    namespace EventRouting
    {

        /// <summary>
        /// 全局事件根
        /// </summary>
        public class EventRouter
        {
            protected EventRouter()
            {

            }
            static EventRouter()
            {
                Instance = new EventRouter();
            }

            public static EventRouter Instance { get; protected set; }




            /// <summary>
            /// 触发事件    
            /// </summary>
            /// <typeparam name="TEventArgs">事件数据类型</typeparam>
            /// <param name="sender">事件发送者</param>
            /// <param name="eventArgs">事件数据</param>
            /// <param name="callerMemberName">发送事件名</param>
            public virtual void RaiseEvent<TEventArgs>(object sender, TEventArgs eventArgs, string callerMemberName = "")
            {
                var eventObject = GetIEventObjectInstance(typeof(TEventArgs));
                eventObject.RaiseEvent(sender, callerMemberName, eventArgs);

                while (eventObject.BaseArgsTypeInstance != null)
                {
                    eventObject = eventObject.BaseArgsTypeInstance;
                }
            }


            /// <summary>
            /// 取得独立事件类
            /// </summary>
            /// <typeparam name="TEventArgs">事件数据类型</typeparam>
            /// <returns>事件独立类</returns>
            public virtual EventObject<TEventArgs> GetEventObject<TEventArgs>()
#if !NETFX_CORE
 where TEventArgs : EventArgs
#endif
            {
                var eventObject = (EventObject<TEventArgs>)GetIEventObjectInstance(typeof(TEventArgs));

                return eventObject;

            }

            /// <summary>
            /// 事件来源的代理对象实例
            /// </summary>

            static protected readonly ConcurrentDictionary<Type, IEventObject> EventObjects
     = new ConcurrentDictionary<Type, IEventObject>();
            /// <summary>
            /// 创建事件代理对象
            /// </summary>
            /// <param name="argsType">事件数据类型</param>
            /// <returns>代理对象实例</returns>
            static protected IEventObject GetIEventObjectInstance(Type argsType)
            {

                var rval = EventObjects.GetOrAdd(
                    argsType,
                    t =>
                        Activator.CreateInstance(typeof(EventObject<>).MakeGenericType(t)) as IEventObject
                    );

                if (rval.BaseArgsTypeInstance == null)
                {
#if NETFX_CORE
                    var baseT = argsType.GetTypeInfo().BaseType;
                    if (baseT != typeof(object) && baseT.Name != "RuntimeClass")
#else
                    var baseT = argsType.BaseType;
                    if (baseT != typeof(object))
#endif
                    {
                        rval.BaseArgsTypeInstance = GetIEventObjectInstance(baseT);
                    }

                }

                return rval;
            }


            /// <summary>
            /// 事件对象接口
            /// </summary>
            protected interface IEventObject
            {
                IEventObject BaseArgsTypeInstance { get; set; }
                void RaiseEvent(object sender, string eventName, object args);
            }




            /// <summary>
            ///事件对象
            /// </summary>
            /// <typeparam name="TEventArgs"></typeparam>
            public class EventObject<TEventArgs> : IEventObject
#if !NETFX_CORE
 where TEventArgs : EventArgs
#endif

            {
                public EventObject()
                {
                }


                IEventObject IEventObject.BaseArgsTypeInstance
                {
                    get;
                    set;
                }

                void IEventObject.RaiseEvent(object sender, string eventName, object args)
                {
                    RaiseEvent(sender, eventName, args);
                }

                public void RaiseEvent(object sender, string eventName, object args)
                {


                    var a = args;
                    if (a != null && Event != null)
                    {
                        Event(sender, new RouterEventData<TEventArgs>(sender, eventName, (TEventArgs)args));
                    }
                }

                public event EventHandler<RouterEventData<TEventArgs>> Event;

            }


        }
        /// <summary>
        /// 导航事件数据
        /// </summary>
        public class NavigateCommandEventArgs : EventArgs
        {
            public NavigateCommandEventArgs()
            {
                ParameterDictionary = new Dictionary<string, object>();
            }
            public NavigateCommandEventArgs(IDictionary<string, object> dic)
                : this()
            {
                foreach (var item in dic)
                {

                    (ParameterDictionary as IDictionary<string, object>)[item.Key] = item.Value;
                }

            }
            public Dictionary<string, object> ParameterDictionary { get; set; }

            public Type SourceViewType { get; set; }

            public Type TargetViewType { get; set; }

            public IViewModel ViewModel { get; set; }

            public Object TargetFrame { get; set; }
        }

        /// <summary>
        /// 保存状态事件数据
        /// </summary>
        public class SaveStateEventArgs : EventArgs
        {
            public string ViewKeyId { get; set; }
            public Dictionary<string, object> State { get; set; }
        }

        /// <summary>
        /// 事件路由的扩展方法集合
        /// </summary>
        public static class EventRouterHelper
        {
            /// <summary>
            /// 触发事件
            /// </summary>
            /// <typeparam name="TEventArgs">事件类型</typeparam>
            /// <param name="source">事件来源</param>
            /// <param name="eventArgs">事件数据</param>
            /// <param name="callerMemberName">事件名</param>
            public static void RaiseEvent<TEventArgs>(this BindableBase source, TEventArgs eventArgs, string callerMemberName = "")
            {
                EventRouter.Instance.RaiseEvent(source, eventArgs, callerMemberName);
            }

        }

        /// <summary>
        /// 事件信息
        /// </summary>
        /// <typeparam name="TEventArgs">事件数据类型</typeparam>
        public class RouterEventData<TEventArgs>
#if ! NETFX_CORE
 : EventArgs
                where TEventArgs : EventArgs
#endif

        {
            public RouterEventData(object sender, string eventName, TEventArgs eventArgs)
            {

                Sender = sender;
                EventName = eventName;
                EventArgs = eventArgs;
            }
            /// <summary>
            /// 事件发送者
            /// </summary>
            public Object Sender { get; private set; }
            /// <summary>
            /// 事件名
            /// </summary>
            public string EventName { get; private set; }
            /// <summary>
            /// 事件数据
            /// </summary>
            public TEventArgs EventArgs { get; private set; }
        }

    }
}
