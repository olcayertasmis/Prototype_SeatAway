using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace PSA.Core
{
    public enum PoolType
    {
        Passenger,
    }

    [Serializable]
    public class PoolSetup
    {
        public PoolType type;
        public GameObject prefab;
        public int defaultCapacity;
        public int maxSize;
    }

    public class PoolManager : MonoBehaviour, ISystem
    {
        [Header("Pool Configurations")]
        [SerializeField] private List<PoolSetup> poolSetups = new();

        private Dictionary<PoolType, ObjectPool<GameObject>> _poolDictionary = new();

        #region Unity Lifecycle

        private void OnDestroy()
        {
            SystemLocator.Deregister<PoolManager>();
        }

        #endregion

        #region Initialization

        public void Initialize()
        {
            SystemLocator.Register(this);

            foreach (var setup in poolSetups)
            {
                var pool = new ObjectPool<GameObject>(createFunc: () =>
                    {
                        GameObject obj = Instantiate(setup.prefab, transform);
                        obj.SetActive(false);
                        return obj;
                    },
                    actionOnGet: (obj) => obj.SetActive(true),
                    actionOnRelease: (obj) =>
                    {
                        obj.SetActive(false);
                        obj.transform.SetParent(transform);
                    },
                    actionOnDestroy: Destroy,
                    collectionCheck: false,
                    defaultCapacity: setup.defaultCapacity,
                    maxSize: setup.maxSize
                );

                _poolDictionary.Add(setup.type, pool);
            }
        }

        #endregion

        #region Core Logic

        public GameObject Spawn(PoolType type, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (_poolDictionary.TryGetValue(type, out var pool))
            {
                GameObject obj = pool.Get();
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                if (parent) obj.transform.SetParent(parent);
                return obj;
            }

            Debug.LogError($"[PoolManager] Pool for {type} not found! Check configurations.");
            return null;
        }

        public T Spawn<T>(PoolType type, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            GameObject obj = Spawn(type, position, rotation, parent);
            return obj ? obj.GetComponent<T>() : null;
        }

        public void Despawn(PoolType type, GameObject obj)
        {
            if (_poolDictionary.TryGetValue(type, out var pool))
            {
                pool.Release(obj);
            }
        }

        #endregion
    }
}