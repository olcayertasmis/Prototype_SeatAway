using System;
using System.Collections.Generic;
using UnityEngine;

namespace PSA.Core
{
    public static class SystemLocator
    {
        private static readonly Dictionary<Type, ISystem> Systems = new();

        public static void Register<T>(T system) where T : ISystem
        {
            Type type = typeof(T);
            if (!Systems.TryAdd(type, system))
            {
                Debug.LogWarning($"SystemLocator {type.Name} is already registered!");
            }
        }

        public static void Deregister<T>() where T : ISystem
        {
            Type type = typeof(T);
            Systems.Remove(type);
        }

        public static T Get<T>() where T : class, ISystem
        {
            Type type = typeof(T);
            if (Systems.TryGetValue(type, out ISystem system))
            {
                return system as T;
            }

            Debug.LogError($"SystemLocator {type.Name} not found in registered systems!");
            return null;
        }

        public static bool TryGet<T>(out T result) where T : class, ISystem
        {
            Type type = typeof(T);
            if (Systems.TryGetValue(type, out ISystem system))
            {
                result = system as T;
                return true;
            }

            result = null;
            return false;
        }
    }
}