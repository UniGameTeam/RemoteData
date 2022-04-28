namespace UniModules.UniGame.RemoteData.MutableObject
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using RemoteData;
    using UniRx;

    public class MutableChild<T> : IMutableChildBase
    {
        private static Dictionary<string, FieldInfo> _fieldInfoCache = new Dictionary<string, FieldInfo>();
        private static Dictionary<string, PropertyInfo> _propertyInfoCache = new Dictionary<string, PropertyInfo>();

        private readonly Func<T> _getter;
        protected IRemoteChangesStorage _storage;
        private readonly Dictionary<string, INotifyable> _properties;
        private Dictionary<string, IMutableChildBase> _childObjects;

        public ReactiveProperty<bool> HaveNewChanges => _storage.HaveNewChanges;
        protected T Object => _getter();

        public MutableChild(Func<T> getter, string fullPath, IRemoteChangesStorage storage)
        {
            _getter = getter;
            FullPath = fullPath;
            _storage = storage;
            _properties = new Dictionary<string, INotifyable>();
            _childObjects = new Dictionary<string, IMutableChildBase>();
        }

        public string FullPath { get; private set; }

        public void UpdateChildData(string fieldName, object newValue)
        {
            _storage.AddChange(RemoteDataChange.Create(
                FullPath + fieldName,
                fieldName,
                newValue,
                ApplyChangeLocal));
        }

        private void ApplyChangeLocal(RemoteDataChange change)
        {
            SetValueChangeValue(change);
            PropertyChanged(change.FieldName);
        }

        private void SetValueChangeValue(RemoteDataChange change)
        {
            if (_fieldInfoCache.TryGetValue(change.FieldName, out var fieldInfo))
            {
                fieldInfo.SetValue(Object, change.FieldValue);
                return;
            }

            if (_propertyInfoCache.TryGetValue(change.FieldName, out var propertyInfo))
            {
                propertyInfo.SetValue(Object, change.FieldValue);
                return;
            }

            fieldInfo = typeof(T).GetField(change.FieldName);
            if (fieldInfo != null)
            {
                _fieldInfoCache.Add(change.FieldName, fieldInfo);
                fieldInfo.SetValue(Object, change.FieldValue);
                return;
            }

            propertyInfo = typeof(T).GetProperty(change.FieldName);
            if (propertyInfo != null)
            {
                _propertyInfoCache.Add(change.FieldName, propertyInfo);
                propertyInfo.SetValue(Object, change.FieldValue);
                return;
            }

            throw new InvalidOperationException($"Unable to find field or property :: {change.FieldName}");
        }

        protected MutableObjectReactiveProperty<Tvalue> CreateReactiveProperty<Tvalue>(Func<Tvalue> getter, Action<Tvalue> setter, string fieldName)
        {
            var property = new MutableObjectReactiveProperty<Tvalue>(getter, setter, this);
            _properties.Add(fieldName, property);
            return property;
        }

        protected void PropertyChanged(string name)
        {
            if (_properties.ContainsKey(name))
                _properties[name].Notify();
        }

        protected void AllPropertiesChanged()
        {
            foreach (var property in _properties.Values)
                property.Notify();
        }
        
        public void AddChange(RemoteDataChange change)
        {
            _storage.AddChange(change);
        }

        public bool IsRootLoaded()
        {
            return _storage.IsRootLoaded();
        }

        public Task CommitChanges(bool disposeChanges = true)
        {
            return _storage.CommitChanges();
        }

        public string GetChildPath(string objectName)
        {
            return FullPath + objectName + RemoteObjectsProvider.PathDelimeter;
        }
    }
}
