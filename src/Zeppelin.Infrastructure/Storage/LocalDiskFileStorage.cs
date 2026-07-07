using Microsoft.Extensions.Options;

namespace Zeppelin.Infrastructure.Storage;

public class LocalDiskFileStorageOptions
{
    public const string SectionName = "Storage";

    public string RootPath { get; set; } = "App_Data/attachments";
}

public class LocalDiskFileStorage(IOptions<LocalDiskFileStorageOptions> options) : IFileStorage
{
    private readonly string _rootPath = options.Value.RootPath;

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_rootPath);

        var extension = SanitizeExtension(Path.GetExtension(fileName));
        var storageKey = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(_rootPath, storageKey);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);

        return storageKey;
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_rootPath, storageKey);
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_rootPath, storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    // storageKey is always a fresh Guid we generated, but the extension is
    // derived from a client-supplied filename - keep it to a short
    // alphanumeric suffix so it can't smuggle path separators or traversal
    // sequences into the on-disk path.
    private static string SanitizeExtension(string extension)
    {
        var chars = extension.Where(char.IsLetterOrDigit).Take(10).ToArray();
        return chars.Length == 0 ? string.Empty : "." + new string(chars);
    }
}
