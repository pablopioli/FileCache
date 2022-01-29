namespace FileCache
{
    internal class CacheItem<T>
    {
        public T? Value { get; set; }
        public long ExpirationTime { get; set; }
    }
}
