using CacheTower;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestProject;

using var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Debug).AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

var services = new ServiceCollection();

services.AddCacheStack<UserData>((_, cache) =>
{
    cache.AddMemoryCacheLayer()
         .AddCustomFileCacheLayer("./cache")
         .WithCleanupFrequency(TimeSpan.FromMinutes(5));
});

var serviceProvider = services.BuildServiceProvider();

Console.WriteLine("Exercising cache");

const string CacheEntryName = "MyCacheEntry";

var cache = serviceProvider.GetRequiredService<ICacheStack<UserData>>();

var userData = new UserData
{
    FirstName = "John",
    LastName = "Doe"
};

// Test if we can evict a cache entry
await cache.SetAsync(CacheEntryName, userData, TimeSpan.FromMinutes(5));
var cachedData = await cache.GetAsync<UserData>(CacheEntryName);
if (cachedData.Value.LastName != "Doe")
{
    throw new Exception();
}

await cache.EvictAsync(CacheEntryName);

cachedData = await cache.GetAsync<UserData>(CacheEntryName);
if (cachedData != null)
{
    throw new Exception();
}

// Test if we can remove expired data
await cache.SetAsync(CacheEntryName, userData, TimeSpan.FromMilliseconds(5));
await Task.Delay(100);

await cache.CleanupAsync();

cachedData = await cache.GetAsync<UserData>(CacheEntryName);
if (cachedData != null)
{
    throw new Exception();
}

for (int i = 0; i < 10; i++)
{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    Task.Run(async () =>
    {
        var cache = serviceProvider.GetRequiredService<ICacheStack<UserData>>();

        var userData = new UserData
        {
            FirstName = "John",
            LastName = "Doe"
        };

        for (int i = 0; i < 10; i++)
        {
            await cache.SetAsync(CacheEntryName, userData, TimeSpan.FromMinutes(5));
            Console.WriteLine("Written to cache");

            var cachedData = await cache.GetAsync<UserData>(CacheEntryName);
            if (cachedData?.Value?.LastName == "Doe")
            {
                Console.WriteLine("Readed from cache");
            }
            else
            {
                Console.WriteLine("Invalid data");
            }
        }
    });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
}

await Task.Delay(10000);

await (cache as IFlushableCacheStack).FlushAsync();
