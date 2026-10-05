using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Service.HttpErrorExceptions;

namespace Service.ExternalServices;

public class CloudflareR2StorageService(IAmazonS3 s3Client, IConfiguration configuration)
{
    private readonly string _publicBucketName = configuration["R2Settings:PublicBucketName"]!;

    private readonly string _privateBucketName = configuration["R2Settings:PrivateBucketName"]!;

    private readonly string _publicDomain = configuration["R2Settings:PublicDomain"]!;

    private string GetBucketName(BucketType bucketType) =>
        bucketType == BucketType.Private ? _privateBucketName : _publicBucketName;

    public async Task UploadFileAsync(string filePath, Stream fileStream, string contentType,
        BucketType bucketType = BucketType.Public, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = GetBucketName(bucketType),
                Key = filePath,
                InputStream = fileStream,
                ContentType = contentType,
                DisablePayloadSigning = true
            };
            await s3Client.PutObjectAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InternalServerErrorException("Không thể tải lên tệp tin.", ex);
        }
    }

    public async Task<FileDownloadResult> DownloadFileAsync(string filePath, BucketType bucketType = BucketType.Public,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = GetBucketName(bucketType),
                Key = filePath
            };
            using var response = await s3Client.GetObjectAsync(request, cancellationToken);
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            return new FileDownloadResult
            {
                FileStream = memoryStream,
                ContentType = response.Headers.ContentType,
                FileName = filePath.Split('/').Last()
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new BadRequestException("Tệp tin không tồn tại hoặc đã bị xóa.", ex);
        }
        catch (Exception ex)
        {
            throw new InternalServerErrorException("Đã xảy ra lỗi khi tải tệp tin.", ex);
        }
    }

    public async Task DeleteFileAsync(string filePath, BucketType bucketType = BucketType.Public,
        CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = GetBucketName(bucketType),
            Key = filePath
        };
        await s3Client.DeleteObjectAsync(request, cancellationToken);
    }

    public async Task<bool> FileExistsAsync(string filePath, BucketType bucketType = BucketType.Public,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = GetBucketName(bucketType),
                Key = filePath
            };
            await s3Client.GetObjectMetadataAsync(request, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            throw new InternalServerErrorException("Đã xảy ra lỗi khi kiểm tra tồn tại của tệp tin.", ex);
        }
    }

    public async Task<PaginatedFileList> ListFilesAsync(string? prefix = null, int maxItems = 100,
        string? continuationToken = null, BucketType bucketType = BucketType.Public,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ListObjectsV2Request
            {
                BucketName = GetBucketName(bucketType),
                Prefix = prefix,
                MaxKeys = maxItems,
                ContinuationToken = continuationToken
            };

            var response = await s3Client.ListObjectsV2Async(request, cancellationToken);

            var files = (response.S3Objects ?? []).Select(o => new FileMetadata
            {
                Key = o.Key,
                Size = o.Size,
                LastModified = o.LastModified
            });

            return new PaginatedFileList
            {
                Files = files,
                NextContinuationToken = response.NextContinuationToken
            };
        }
        catch (Exception ex)
        {
            throw new InternalServerErrorException("Không thể lấy danh sách tệp tin.", ex);
        }
    }

    public string GetPublicUrl(string filePath) => $"{_publicDomain}/{filePath}";

    public Task<string> GeneratePresignedUrlAsync(string filePath, TimeSpan expiresIn) =>
        s3Client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _privateBucketName,
            Key = filePath,
            Expires = DateTime.UtcNow.Add(expiresIn),
            Verb = HttpVerb.GET
        });
}

public class CloudflareR2HealthCheck(IAmazonS3 s3Client, IConfiguration configuration) : IHealthCheck
{
    private readonly string _publicBucketName = configuration["R2Settings:PublicBucketName"]!;
    private readonly string _privateBucketName = configuration["R2Settings:PrivateBucketName"]!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(3000);
            await s3Client.GetBucketLocationAsync(_publicBucketName, cts.Token);
            await s3Client.GetBucketLocationAsync(_privateBucketName, cts.Token);
            return HealthCheckResult.Healthy($"Kết nối thành công đến Cloudflare R2.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Không thể kết nối đến Cloudflare R2': {ex.Message}", ex);
        }
    }
}

public enum BucketType
{
    Public,
    Private
}

public class FileDownloadResult : IDisposable
{
    public required Stream FileStream { get; set; }
    public required string ContentType { get; set; }
    public required string FileName { get; set; }

    public void Dispose()
    {
        FileStream.Dispose();
        GC.SuppressFinalize(this);
    }
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