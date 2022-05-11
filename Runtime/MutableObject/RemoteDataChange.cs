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

        public static RemoteDataChange Create(string fullPath,
                                                    string fieldName,
                                                    object fieldValue,
                                                    Action<RemoteDataChange> applyCallback)
        {
            var change = ClassPool.Spawn<RemoteDataChange>();
            change.FullPath = fullPath.Trim(RemoteObjectsProvider.PathDelimeter);
            change.FieldName = fieldName;
            change.FieldValue = fieldValue;
            change.ApplyCallback = applyCallback;
            return change;
        }

        public void Dispose()
        {
            ClassPool.Despawn(this);
        }

    }
}
