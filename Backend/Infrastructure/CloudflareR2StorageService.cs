using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Contracts.External;
using Domain.Shared.Exceptions.HttpErrorExceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using BucketType = Contracts.External.BucketType;

namespace Infrastructure;

public class CloudflareR2StorageService(IAmazonS3 s3Client, IConfiguration configuration) : IObjectStorageService
{
    private readonly string _publicBucketName = configuration["R2Settings:PublicBucketName"]!;

    private readonly string _privateBucketName = configuration["R2Settings:PrivateBucketName"]!;

    private readonly string _publicDomain = configuration["R2Settings:PublicDomain"]!;

    public async Task UploadFileAsync(string filePath, Stream fileStream, string contentType,
        BucketType bucketType = BucketType.Public, CancellationToken cancellationToken = default)
    {
        try
        {
            await s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = GetBucketName(bucketType),
                Key = filePath,
                InputStream = fileStream,
                ContentType = contentType,
                DisablePayloadSigning = true
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InternalServerErrorException("Không thể tải lên tệp tin.", ex);
        }
    }

    public async Task DeleteFileAsync(string filePath, BucketType bucketType = BucketType.Public,
        CancellationToken cancellationToken = default) =>
        await s3Client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = GetBucketName(bucketType),
            Key = filePath
        }, cancellationToken);

    public async Task<bool> FileExistsAsync(string filePath, BucketType bucketType = BucketType.Public,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await s3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = GetBucketName(bucketType),
                Key = filePath
            }, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
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
            var response = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = GetBucketName(bucketType),
                Prefix = prefix,
                MaxKeys = maxItems,
                ContinuationToken = continuationToken
            }, cancellationToken);
            return new PaginatedFileList
            {
                Files = (response.S3Objects ?? []).Select(o => new FileMetadata
                {
                    Key = o.Key,
                    Size = o.Size,
                    LastModified = o.LastModified
                }),
                NextContinuationToken = response.NextContinuationToken
            };
        }
        catch (Exception ex)
        {
            throw new InternalServerErrorException("Không thể lấy danh sách tệp tin.", ex);
        }
    }

    public Task<string> GetPublicUrlAsync(string filePath) => Task.FromResult($"{_publicDomain}/{filePath}");

    public async Task<string> GeneratePresignedUrlAsync(string filePath, TimeSpan expiresIn,
        PresignedUrlOptions? options = null)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _privateBucketName,
            Key = filePath,
            Expires = DateTime.UtcNow.Add(expiresIn),
            Verb = HttpVerb.GET
        };

        if (options != null)
        {
            request.ResponseHeaderOverrides = new ResponseHeaderOverrides
            {
                ContentType = options.ContentType,
                ContentDisposition = options.ContentDisposition,
                CacheControl = options.CacheControl
            };
        }

        return await s3Client.GetPreSignedURLAsync(request);
    }

    private string GetBucketName(BucketType bucketType) =>
        bucketType == BucketType.Private ? _privateBucketName : _publicBucketName;
}

public class CloudflareR2HealthCheck(IAmazonS3 s3Client, IConfiguration configuration) : IHealthCheck
{
    private readonly string _publicBucketName = configuration["R2Settings:PublicBucketName"]!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(3000);
            await s3Client.HeadBucketAsync(new HeadBucketRequest { BucketName = _publicBucketName }, cts.Token);
            return HealthCheckResult.Healthy($"Kết nối thành công đến Cloudflare R2.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Không thể kết nối đến Cloudflare R2': {ex.Message}", ex);
        }
    }
}