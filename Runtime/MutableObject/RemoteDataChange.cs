namespace UniModules.UniGame.RemoteData.MutableObject
{
    using System;
    using Newtonsoft.Json;
    using UniModules.UniCore.Runtime.ObjectPool.Runtime;
    using UniModules.UniGame.RemoteData.RemoteData;

    public class RemoteDataChange : IDisposable
    {
        public string FieldName;
        public string FullPath;
        [JsonConverter(typeof(ObjectJsonConverter))]
        public object FieldValue;
        [JsonIgnore]
        public Action<RemoteDataChange> ApplyCallback;

        protected RemoteDataChange() { }

        public static RemoteDataChange Create(string FullPath,
                                                    string FieldName,
                                                    object FieldValue,
                                                    Action<RemoteDataChange> ApplyCallback)
        {
            var change = ClassPool.SpawnOrCreate(() => new RemoteDataChange());
            change.FullPath = FullPath.Trim(RemoteObjectsProvider.PathDelimeter);
            change.FieldName = FieldName;
            change.FieldValue = FieldValue;
            change.ApplyCallback = ApplyCallback;
            return change;
        }

        public void Dispose()
        {
            ClassPool.Despawn(this, null);
        }

    }
}
