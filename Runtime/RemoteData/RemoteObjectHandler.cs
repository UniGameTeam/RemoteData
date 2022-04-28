namespace UniModules.UniGame.RemoteData.RemoteData
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Core.Runtime.DataFlow.Interfaces;
    using Core.Runtime.Interfaces;
    using Cysharp.Threading.Tasks;
    using MutableObject;
    using UniCore.Runtime.DataFlow;

    public abstract class RemoteObjectHandler<T> : IDisposable, ILifeTimeContext, IRemoteTokenProvider
    {
        public object DeleteValueObject { get; }
        public T Object { get; protected set; }
        public abstract string RemoteToken { get; protected set; }
        public ILifeTime LifeTime => _lifeTime;

        private LifeTimeDefinition _lifeTime = new LifeTimeDefinition();

        private static Dictionary<string, FieldInfo> _fieldInfoCache = new Dictionary<string, FieldInfo>();
        private static Dictionary<string, PropertyInfo> _propertyInfoCache = new Dictionary<string, PropertyInfo>();

        public RemoteObjectHandler(object deleteValueObject)
        {
            DeleteValueObject = deleteValueObject;
        }

        public void Dispose()
        {
            _lifeTime.Terminate();
            _lifeTime = null;
        }

        public abstract UniTask<IEnumerable<RemoteDataChange>> LoadData(Func<T> initialDataProvider = null);

        public abstract RemoteDataChange CreateChange(string fieldName, object fieldValue);

        public async UniTask ApplyChange(RemoteDataChange change)
        {
            await ApplyChangeRemote(change);
        }

        public virtual UniTask ApplyChangesBatched(List<RemoteDataChange> changes)
        {
            throw new NotImplementedException($"Batch apply not implemented for type :: ${this.GetType().FullName}");
        }

        public void ApplyChangeLocal(RemoteDataChange change)
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
        }

        public abstract string GetDataId();

        public abstract string GetFullPath();

        protected abstract UniTask ApplyChangeRemote(RemoteDataChange change);

        public abstract UniTask ClearData();
        public virtual UniTask<string> LoadRemoteToken()
        {
            throw new NotImplementedException();
        }
        public abstract UniTask SetData(T data);
    }

    // TODO левый не нужный интерфейс нужно привести IRemoteChangesStorage и localStorage к одному виду
    public interface IRemoteTokenProvider
    {
        UniTask<string> LoadRemoteToken();
    }
}
