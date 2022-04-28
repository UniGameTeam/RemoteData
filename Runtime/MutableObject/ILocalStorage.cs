namespace UniModules.UniGame.RemoteData.MutableObject
{
    using System.Collections.Generic;
    using UniRx;

    public interface ILocalStorage
    {
        IReactiveProperty<bool> HaveChanges { get; }

        List<RemoteDataChange> GetPendingChanges();
        void                   FlushChanges();
        void SetRemoteToken(string value);

        string GetRemoteToken();
    }
}