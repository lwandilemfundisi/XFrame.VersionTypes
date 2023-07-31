using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XFrame.Common.Extensions;

namespace XFrame.VersionTypes
{
    public abstract class VersionedTypeUpgradeService<TAttribute, TDefinition, TVersionedType, TDefinitionService> :
        IVersionedTypeUpgradeService<TAttribute, TDefinition, TVersionedType>
        where TAttribute : VersionedTypeAttribute
        where TDefinition : VersionedTypeDefinition
        where TVersionedType : IVersionedType
        where TDefinitionService : IVersionedTypeDefinitionService<TAttribute, TDefinition>
    {
        private readonly ILogger<VersionedTypeUpgradeService<TAttribute, TDefinition, TVersionedType, TDefinitionService>> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TDefinitionService _definitionService;

        protected VersionedTypeUpgradeService(
            ILogger<VersionedTypeUpgradeService<TAttribute, TDefinition, TVersionedType, TDefinitionService>> logger,
            IServiceProvider serviceProvider,
            TDefinitionService definitionService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _definitionService = definitionService;
        }

        public async Task<TVersionedType> UpgradeAsync(TVersionedType versionedType, CancellationToken cancellationToken)
        {
            var currentDefinition = _definitionService.GetDefinition(versionedType.GetType());
            var definitionsWithHigherVersion = _definitionService.GetDefinitions(currentDefinition.Name)
                .Where(d => d.Version > currentDefinition.Version)
                .OrderBy(d => d.Version)
                .ToList();

            if (!definitionsWithHigherVersion.Any())
            {
                if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
                {
                    _logger.LogTrace($"No need to update '{versionedType.GetType().PrettyPrint()}' as its already the correct version");
                }

                return versionedType;
            }

            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
            {
                _logger.LogTrace($"Snapshot '{currentDefinition.Name}' v{currentDefinition.Version} needs to be upgraded to v{definitionsWithHigherVersion.Last().Version}");
            }

            foreach (var nextDefinition in definitionsWithHigherVersion)
            {
                versionedType = await UpgradeToVersionAsync(
                    versionedType,
                    currentDefinition,
                    nextDefinition,
                    cancellationToken)
                    .ConfigureAwait(false);
                currentDefinition = nextDefinition;
            }

            return versionedType;
        }

        protected abstract Type CreateUpgraderType(Type fromType, Type toType);

        private async Task<TVersionedType> UpgradeToVersionAsync(
            TVersionedType versionedType,
            TDefinition fromDefinition,
            TDefinition toDefinition,
            CancellationToken cancellationToken)
        {
            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace))
                _logger.LogTrace($"Upgrading '{fromDefinition}' to '{toDefinition}'");

            var upgraderType = CreateUpgraderType(fromDefinition.Type, toDefinition.Type);
            var versionedTypeUpgraderType = typeof(IVersionedTypeUpgrader<,>).MakeGenericType(fromDefinition.Type, toDefinition.Type);
            var versionedTypeUpgrader = _serviceProvider.GetService(upgraderType);

            var methodInfo = versionedTypeUpgraderType.GetTypeInfo().GetMethod("UpgradeAsync");

            var task = (Task)methodInfo.Invoke(versionedTypeUpgrader, new object[] { versionedType, cancellationToken });

            await task.ConfigureAwait(false);

            return ((dynamic)task).Result;
        }
    }
}
