using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

public static class ExtensionMethods
{
    public static ICacheStackBuilder AddCustomFileCacheLayer(this ICacheStackBuilder builder, string folder)
    {
        builder.CacheLayers.Add(new FileCache.FileCacheLayer(folder));
        return builder;
    }

    public static ICacheStackBuilder AddCustomFileCacheLayer(this ICacheStackBuilder builder, string folder, ILogger logger)
    {
        builder.CacheLayers.Add(new FileCache.FileCacheLayer(folder, logger));
        return builder;
    }
}
