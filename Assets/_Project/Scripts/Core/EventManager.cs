using System;
using System.Collections.Generic;
using UnityEngine;

namespace PSA.Core
{
    public class EventManager : MonoBehaviour, ISystem
    {
        private readonly Dictionary<Type, Delegate> _events = new();

        #region Unity Lifecycle

        private void OnDestroy()
        {
            SystemLocator.Deregister<EventManager>();
        }

        #endregion

        #region ISystem Implementation

        public void Initialize()
        {
            SystemLocator.Register(this);
        }

        #endregion

        #region Core Logic

        public void AddListener<T>(Action<T> listener) where T : struct
        {
            Type eventType = typeof(T);
            if (!_events.TryAdd(eventType, listener))
            {
                _events[eventType] = Delegate.Combine(_events[eventType], listener);
            }
        }

        public void RemoveListener<T>(Action<T> listener) where T : struct
        {
            Type eventType = typeof(T);
            if (_events.ContainsKey(eventType))
            {
                Delegate currentDelegate = _events[eventType];
                currentDelegate = Delegate.Remove(currentDelegate, listener);

                if (currentDelegate == null)
                {
                    _events.Remove(eventType);
                }
                else
                {
                    _events[eventType] = currentDelegate;
                }
            }
        }

        public void TriggerEvent<T>(T eventData) where T : struct
        {
            Type eventType = typeof(T);
            if (_events.TryGetValue(eventType, out Delegate currentDelegate))
            {
                if (currentDelegate is Action<T> action)
                {
                    action.Invoke(eventData);
                }
            }
        }

        #endregion
    }
}