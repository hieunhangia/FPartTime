namespace Contracts.External;

public interface IObjectStorageService
{
    Task UploadFileAsync(string filePath, Stream fileStream, string contentType,
        BucketType bucketType = BucketType.Public, CancellationToken cancellationToken = default);

    Task DeleteFileAsync(string filePath, BucketType bucketType = BucketType.Public,
        CancellationToken cancellationToken = default);

    Task<bool> FileExistsAsync(string filePath, BucketType bucketType = BucketType.Public,
        CancellationToken cancellationToken = default);

    Task<PaginatedFileList> ListFilesAsync(string prefix, int pageSize = 1000, string? continuationToken = null,
        BucketType bucketType = BucketType.Public, CancellationToken cancellationToken = default);

    Task<string> GetPublicUrlAsync(string filePath);

    Task<string> GeneratePresignedUrlAsync(string filePath, TimeSpan expiration, PresignedUrlOptions? options = null);
}

public enum BucketType
{
    Public,
    Private
}

public class FileMetadata
{
    public required string Key { get; set; }
    public long? Size { get; set; }
    public DateTime? LastModified { get; set; }
}

public class PaginatedFileList
{
    public required IEnumerable<FileMetadata> Files { get; set; }
    public string? NextContinuationToken { get; set; }
    public bool HasNextPage => !string.IsNullOrEmpty(NextContinuationToken);
}

public record PresignedUrlOptions(
    string? ContentType = null,
    string? ContentDisposition = null,
    string? CacheControl = null
);