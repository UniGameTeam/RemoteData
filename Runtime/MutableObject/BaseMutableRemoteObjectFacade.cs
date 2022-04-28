namespace UniModules.UniGame.RemoteData.MutableObject
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using RemoteData;
    using UniModules.UniCore.Runtime.ObjectPool.Runtime;
    using UniRx;
    using UniTask = Cysharp.Threading.Tasks.UniTask;

    public class BaseMutableRemoteObjectFacade<T> : IRemoteChangesStorage
    {
        public ReactiveProperty<bool> HaveNewChanges { get; } = new ReactiveProperty<bool>(false);

        protected RemoteObjectHandler<T> _objectHandler;

        private ConcurrentStack<RemoteDataChange> _pendingChanges;

        private Dictionary<string, INotifyable> _properties;

        private Dictionary<string, IMutableChildBase> _childObjects;

        public BaseMutableRemoteObjectFacade(RemoteObjectHandler<T> objectHandler)
        {
            _objectHandler = objectHandler;
            _pendingChanges = new ConcurrentStack<RemoteDataChange>();
            _properties = new Dictionary<string, INotifyable>();
            _childObjects = new Dictionary<string, IMutableChildBase>();
        }
        
        public string GetId()
        {
            return _objectHandler.GetDataId();
        }

        public void UpdateChildData(string childName, object newData)
        {
            var change = _objectHandler.CreateChange(childName, newData);
            change.ApplyCallback = ApplyChangeOnLocalHandler;
            AddChange(change);
        }

        public void AddChange(RemoteDataChange change)
        {
            _pendingChanges.Push(change);
            ChangeApplied(change);
            HaveNewChanges.Value = true;
        }

        /// <summary>
        /// Отправляет все локально записанные изменения на сервер.
        /// ВАЖНО: все операции в рамках одной комманды не должны прерыватья вызовом
        /// метода
        /// </summary>
        /// <returns></returns>
        public async Task CommitChanges(bool disposeChanges = true)
        {
            List<RemoteDataChange> changes;
            lock (_pendingChanges)
            {
                changes = _pendingChanges.ToList();
                changes.Reverse();
                _pendingChanges.Clear();
                HaveNewChanges.Value = false;
            }

            await _objectHandler.ApplyChangesBatched(changes);
            if (disposeChanges)
                changes.ForEach((ch) => ch.Dispose());
            changes.Clear();
        }
        
        /// <summary>
        /// Создает Reactive Property для работы с оборачиваемыми данными
        /// </summary>
        /// <typeparam name="Tvalue">Тип обрабатываемого поля</typeparam>
        /// <param name="getter"></param>
        /// <param name="setter"></param>
        /// <param name="fieldName">Имя поля</param>
        /// <returns></returns>
        public MutableObjectReactiveProperty<Tvalue> CreateReactiveProperty<Tvalue>(Func<Tvalue> getter, Action<Tvalue> setter, string fieldName)
        {
            var property = new MutableObjectReactiveProperty<Tvalue>(getter, setter, this);
            _properties.Add(fieldName, property);
            return property;
        }

        public void RegisterMutableChild(string childName, IMutableChildBase child)
        {
            _childObjects.Add(childName, child);
        }

        public bool IsRootLoaded()
        {
            return _objectHandler.Object != null;
        }

        public string GetChildPath(string objectName)
        {
            return _objectHandler.GetFullPath() + objectName + RemoteObjectsProvider.PathDelimeter;
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

        private void ChangeApplied(RemoteDataChange change)
        {
            change.ApplyCallback?.Invoke(change);
            // HACK хочется использовать в воркере сохранения, после проверки работоспособности нужно удалить 
            change.ApplyCallback = null;
        }

        private void ApplyChangeOnLocalHandler(RemoteDataChange change)
        {
            _objectHandler.ApplyChangeLocal(change);
            PropertyChanged(change.FieldName);
        }
    }
}
