using CacheTower;
using HashDepot;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileCache;

public class FileCacheLayer : ICacheLayer
{
    private readonly string _folderLocation;
    private readonly ILogger? _logger;
    private readonly byte[] _key;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public FileCacheLayer(string folderLocation, ILogger? logger = null, byte[]? key = null)
    {
        if (string.IsNullOrWhiteSpace(folderLocation))
        {
            throw new ArgumentException(folderLocation, nameof(folderLocation));
        }

        _folderLocation = folderLocation;

        if (key == null)
        {
            var tempKey = Encoding.UTF8.GetBytes(folderLocation).Take(16).ToList();

            while (tempKey.Count < 16)
            {
                tempKey.Add(tempKey[0]);
            }

            _key = tempKey.ToArray();
        }
        else
        {
            _key = key;
        }

        _logger = logger;

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        if (!Directory.Exists(_folderLocation))
        {
            Directory.CreateDirectory(_folderLocation);
        }
    }
    public async ValueTask CleanupAsync()
    {
        foreach (var fileName in Directory.GetFiles(_folderLocation))
        {
            while (File.Exists(fileName))
            {
                try
                {
                    var cacheItem = ReadFile<object>(fileName);
                    var expirationTime = new DateTime(cacheItem.ExpirationTime, DateTimeKind.Utc);
                    if (expirationTime <= DateTime.UtcNow)
                    {
                        try
                        {
                            File.Delete(fileName);
                        }
                        catch (IOException)
                        {
                            // Maybe next time
                        }
                    }

                    break;
                }
                catch (IOException)
                {
                    await Task.Delay(300);
                }
            }
        }
    }

    public ValueTask EvictAsync(string cacheKey)
    {
        DeleteFile(GetFileName(cacheKey));
        return ValueTask.CompletedTask;
    }

    private static void DeleteFile(string file)
    {
        while (File.Exists(file))
        {
            try
            {
                File.Delete(file);
                break;
            }
            catch (IOException)
            {
                // Let's retry
            }
        }
    }

    public ValueTask FlushAsync()
    {
        foreach (var file in Directory.GetFiles(_folderLocation))
        {
            DeleteFile(file);
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask<CacheEntry<T>?> GetAsync<T>(string cacheKey)
    {
        var fileName = GetFileName(cacheKey);
        while (true)
        {
            if (File.Exists(fileName))
            {
                try
                {
                    var cacheItem = ReadFile<T>(fileName);
                    return new CacheEntry<T>(cacheItem.Value, new DateTime(cacheItem.ExpirationTime));
                }
                catch (IOException)
                {
                    _logger?.LogDebug($"Collision reading file {fileName}");
                    await Task.Delay(300);
                }
            }
            else
            {
                throw new Exception();
            }
        }
    }

    private CacheItem<T> ReadFile<T>(string fileName)
    {
        using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        var cacheItem = JsonSerializer.Deserialize<CacheItem<T>>(fileStream, _jsonSerializerOptions);
        fileStream.Close();

        if (cacheItem == null)
        {
            // Blame the serializer
            throw new NullReferenceException();
        }

        return cacheItem;
    }

    public ValueTask<bool> IsAvailableAsync(string cacheKey)
    {
        return ValueTask.FromResult(File.Exists(GetFileName(cacheKey)));
    }

    public async ValueTask SetAsync<T>(string cacheKey, CacheEntry<T> cacheEntry)
    {
        var payload = new CacheItem<T>
        {
            Value = cacheEntry.Value,
            ExpirationTime = cacheEntry.Expiry.Ticks
        };

        var fileName = GetFileName(cacheKey);
        var fileWritten = false;

        do
        {
            try
            {
                using var fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                fileStream.SetLength(0);
                JsonSerializer.Serialize(fileStream, payload, _jsonSerializerOptions);
                fileStream.Close();
                fileWritten = true;
            }
            catch (IOException)
            {
                _logger?.LogDebug($"Collision writing file {fileName}");
                await Task.Delay(300);
            }
        }
        while (!fileWritten);
    }

    private string GetFileName(string cacheKey)
    {
        var hash = SipHash24.Hash64(Encoding.UTF8.GetBytes(cacheKey), _key);
        return Path.Combine(_folderLocation, $"h{hash}");
    }
}
