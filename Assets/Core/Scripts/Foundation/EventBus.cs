using Core.Scripts.Foundation.Singleton;
using System;
using System.Collections.Generic;

namespace Core.Scripts.Foundation
{
    public abstract class MessageUnit : EventArgs {}
    
    public class EventBus : SingletonObject<EventBus>
    {
        #region Singleton
        private EventBus() { }
        #endregion

        private readonly Dictionary<Type, Action<MessageUnit>> _messageMap = new();
        private readonly Dictionary<object, Dictionary<Type, Action<MessageUnit>>> _subscriberMap = new();
        
        public void SubscribeEvent<T>(object recver, Action<T> callback) where T : MessageUnit
        {
            if(recver == null || callback == null) 
                return;
            
            Action<MessageUnit> action = message => callback((T)message);
            
            if(!_messageMap.ContainsKey(typeof(T)))
                _messageMap.Add(typeof(T), action);
            else
                _messageMap[typeof(T)] += action;
            
            if(!_subscriberMap.ContainsKey(recver))
                _subscriberMap.Add(recver, new Dictionary<Type, Action<MessageUnit>>());
            
            if(!_subscriberMap[recver].ContainsKey(typeof(T)))
                _subscriberMap[recver].Add(typeof(T), action);
            else
                _subscriberMap[recver][typeof(T)] += action;
        }

        public void SendMessage<T>(T message) where T : MessageUnit
        {
            if (_messageMap.ContainsKey(typeof(T)))
            {
                _messageMap[typeof(T)]?.Invoke(message);
            }
        }
        
        public void UnsubscribeEvent(object recver)
        {
            if (recver == null) return;
            List<Type> targets = new();
            foreach(var type in _subscriberMap[recver].Keys)
            {
                if (type == null) continue;
                targets.Add(type);
            }
            foreach(var type in targets)
            {
                _messageMap[type] -= _subscriberMap[recver][type];
                if (_messageMap[type] == null)  //된다!
                    _messageMap.Remove(type);
            }
            _subscriberMap.Remove(recver);
        }
        
        
    }
}