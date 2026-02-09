using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace BZTerrainEditor.ViewModels;

public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, 
    INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly Dictionary<TKey, TValue> _dict = new();

    public event NotifyCollectionChangedEventHandler CollectionChanged;
    public event PropertyChangedEventHandler PropertyChanged;

    public TValue this[TKey key]
    {
        get => _dict[key];
        set
        {
            if (_dict.ContainsKey(key))
            {
                var oldValue = _dict[key];
                _dict[key] = value;
                OnPropertyChanged(nameof(Count));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace, 
                    new KeyValuePair<TKey, TValue>(key, value), 
                    new KeyValuePair<TKey, TValue>(key, oldValue)));
            }
            else
            {
                Add(key, value);
            }
        }
    }

    public ICollection<TKey> Keys => _dict.Keys;
    public ICollection<TValue> Values => _dict.Values;
    public int Count => _dict.Count;
    public bool IsReadOnly => false;

    public void Add(TKey key, TValue value)
    {
        _dict.Add(key, value);
        OnPropertyChanged(nameof(Count));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Add, 
            new KeyValuePair<TKey, TValue>(key, value)));
    }

    public bool Remove(TKey key)
    {
        if (_dict.TryGetValue(key, out var value) && _dict.Remove(key))
        {
            OnPropertyChanged(nameof(Count));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, 
                new KeyValuePair<TKey, TValue>(key, value)));
            return true;
        }
        return false;
    }

    public bool ContainsKey(TKey key) => _dict.ContainsKey(key);
    public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear()
    {
        _dict.Clear();
        OnPropertyChanged(nameof(Count));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
    public bool Contains(KeyValuePair<TKey, TValue> item) => _dict.ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
        ((IDictionary<TKey, TValue>)_dict).CopyTo(array, arrayIndex);
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();

    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) =>
        CollectionChanged?.Invoke(this, e);

    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}