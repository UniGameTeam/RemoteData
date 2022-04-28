using Cysharp.Threading.Tasks;

namespace UniModules.UniGame.RemoteData.MutableObject
{
    using System.Threading.Tasks;
    using UniRx;

    public interface IRemoteChangesStorage
    {
        void AddChange(RemoteDataChange change);
        bool IsRootLoaded();
        Task CommitChanges(bool disposeChanges = true);
        ReactiveProperty<bool> HaveNewChanges { get; }
    }
}
