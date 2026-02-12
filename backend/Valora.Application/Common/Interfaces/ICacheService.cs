namespace Valora.Application.Common.Interfaces;

public interface ICacheService
{
    bool TryGetValue<T>(string key, out T? value);
    void Set<T>(string key, T value, TimeSpan expiration);
    void Remove(string key);
}
