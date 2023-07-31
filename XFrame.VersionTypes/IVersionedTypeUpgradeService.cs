namespace XFrame.VersionTypes
{
    public interface IVersionedTypeUpgradeService<TAttribute, TDefinition, TVersionedType>
        where TAttribute : VersionedTypeAttribute
        where TDefinition : VersionedTypeDefinition
        where TVersionedType : IVersionedType
    {
        Task<TVersionedType> UpgradeAsync(TVersionedType versionedType, CancellationToken cancellationToken);
    }
}
