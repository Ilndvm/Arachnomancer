using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectPool<T> where T : Component
{
    public T this[int index] => _pooledObjects[index];

    public int Count => _pooledObjects.Count;

    public IEnumerable<T> ActiveObjects
    {
        get
        {
            for (int i = 0; i < _pooledObjects.Count; i++)
            {
                if (IsActive(_pooledObjects[i]))
                {
                    yield return _pooledObjects[i];
                }
            }
        }
    }

    private T _prefab;
    private List<T> _pooledObjects;
    private bool _isPoolable;
    private Transform _container;


    public ObjectPool(T prefab, Transform container = null)
    {
        _prefab = prefab;
        _isPoolable = typeof(IPoolable).IsAssignableFrom(typeof(T));
        _pooledObjects = new List<T>(2);
        _container = container;
    }

    public T GetPooledObject()
    {
        for (int i = 0; i < Count; i++)
        {
            if (_isPoolable)
            {
                IPoolable poolable = (IPoolable)_pooledObjects[i];
                if (poolable.IsActive == false)
                {
                    ResetPoolableComponent(_pooledObjects[i]);
                    _pooledObjects[i].transform.SetAsLastSibling();

                    return _pooledObjects[i];
                }
            }
            else if (_pooledObjects[i].gameObject.activeSelf == false)
            {
                _pooledObjects[i].gameObject.SetActive(true);
                _pooledObjects[i].transform.SetAsLastSibling();

                return _pooledObjects[i];
            }
        }

        T instance = GetNewInstance(_prefab);
        _pooledObjects.Add(instance);

        return instance;
    }

    private T GetNewInstance(T prefab)
    {
        T instance = Object.Instantiate(prefab, _container);
        instance.transform.SetAsLastSibling();
        ResetPoolableComponent(instance);
        return instance;
    }

    public void Populate(int count)
    {
        for (int i = 0; i < count; i++)
        {
            T instance = Object.Instantiate(_prefab, _container);
            _pooledObjects.Add(instance);
            DisablePoolableComponent(instance);
        }
    }

    public int GetIndex(T obj)
    {
        return _pooledObjects.IndexOf(obj);
    }

    public void DisableAll()
    {
        for (int i = 0; i < _pooledObjects.Count; i++)
        {
            DisablePoolableComponent(_pooledObjects[i]);
        }
    }

    public void DestroyAll()
    {
        for (int i = 0; i < Count; i++)
        {
            Object.DestroyImmediate(_pooledObjects[i].gameObject);
        }

        _pooledObjects.Clear();
    }

    public void DisablePoolableComponent(T target)
    {
        if (_isPoolable)
        {
            ((IPoolable)target).Disable();
        }
        else
        {
            target.gameObject.SetActive(false);
        }
    }

    private void ResetPoolableComponent(T target)
    {
        if (_isPoolable)
        {
            ((IPoolable)target).Reset();
        }
        else
        {
            target.gameObject.SetActive(true);
        }
    }

    private bool IsActive(T poolable)
    {
        if (_isPoolable)
        {
            return ((IPoolable)poolable).IsActive;
        }

        return poolable.gameObject.activeSelf;
    }
}