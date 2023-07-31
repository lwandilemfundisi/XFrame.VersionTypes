namespace XFrame.VersionTypes
{
    public interface IVersionedTypeUpgrader<in TFrom, TTo>
        where TFrom : IVersionedType
        where TTo : IVersionedType
    {
        Task<TTo> UpgradeAsync(TFrom fromVersionedType, CancellationToken cancellationToken);
    }
}
