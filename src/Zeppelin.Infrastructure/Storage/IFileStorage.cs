namespace Zeppelin.Infrastructure.Storage;

// Storage key is opaque to callers - entities persist it, never a raw path,
// so the backend (local disk now, blob storage later) can change without
// touching any table.
public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, CancellationToken ct = default);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default);

    Task DeleteAsync(string storageKey, CancellationToken ct = default);
}
