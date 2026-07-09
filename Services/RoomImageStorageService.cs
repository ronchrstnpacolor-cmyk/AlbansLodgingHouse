using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace AlbansLodgingHouse.Services;

public class RoomImageStorageService
{
    private static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    public const long MaxFileSizeBytes = 5 * 1024 * 1024;
    public const int MaxPhotosPerRoomType = 5;

    private readonly string _webRootPath;
    private readonly string _bucketName;
    private readonly string _publicBaseUrl;
    private readonly AmazonS3Client? _s3Client;

    public RoomImageStorageService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _webRootPath = environment.WebRootPath;

        var accountId = configuration["R2:AccountId"] ?? "";
        var accessKey = configuration["R2:AccessKeyId"] ?? "";
        var secretKey = configuration["R2:SecretAccessKey"] ?? "";
        _bucketName = configuration["R2:BucketName"] ?? "";
        _publicBaseUrl = (configuration["R2:PublicBaseUrl"] ?? "").TrimEnd('/');

        IsCloudConfigured = accountId.Length > 0 && accessKey.Length > 0 && secretKey.Length > 0
            && _bucketName.Length > 0 && _publicBaseUrl.Length > 0;

        if (IsCloudConfigured)
        {
            _s3Client = new AmazonS3Client(
                new BasicAWSCredentials(accessKey, secretKey),
                new AmazonS3Config
                {
                    ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                    ForcePathStyle = true,
                    // R2 doesn't implement the AWS SDK's newer trailing-checksum streaming
                    // upload mode; without this, PutObject fails with "STREAMING-AWS4-HMAC-
                    // SHA256-PAYLOAD-TRAILER not implemented".
                    RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                    ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
                });
        }
    }

    public bool IsCloudConfigured { get; }

    public static bool IsAllowed(IFormFile file, out string? error)
    {
        if (file.Length <= 0)
        {
            error = "The file is empty.";
            return false;
        }

        if (file.Length > MaxFileSizeBytes)
        {
            error = $"\"{file.FileName}\" is larger than 5 MB.";
            return false;
        }

        if (!AllowedContentTypes.ContainsKey(file.ContentType))
        {
            error = $"\"{file.FileName}\" must be a JPG, PNG or WEBP image.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Saves the photo and returns a value ready to drop straight into an &lt;img src&gt; —
    /// a root-relative path ("/uploads/...") when stored locally, or an absolute R2 URL when
    /// cloud storage is configured.
    /// </summary>
    public async Task<string> SaveAsync(Guid roomTypeNewId, IFormFile file, CancellationToken cancellationToken = default)
    {
        var extension = AllowedContentTypes[file.ContentType];
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var key = $"rooms/{roomTypeNewId}/{fileName}";

        if (IsCloudConfigured)
        {
            await using var stream = file.OpenReadStream();
            await _s3Client!.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = stream,
                ContentType = file.ContentType,
                // R2 doesn't support the AWS SDK's chunked/streaming SigV4 payload signing —
                // fall back to a single unsigned-payload PUT over HTTPS, which R2 does support.
                DisablePayloadSigning = true,
                UseChunkEncoding = false,
                DisableDefaultChecksumValidation = true,
            }, cancellationToken);

            return $"{_publicBaseUrl}/{key}";
        }

        var relativeDir = Path.Combine("uploads", "rooms", roomTypeNewId.ToString());
        var absoluteDir = Path.Combine(_webRootPath, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var absolutePath = Path.Combine(absoluteDir, fileName);
        await using (var fileStream = File.Create(absolutePath))
        {
            await file.CopyToAsync(fileStream, cancellationToken);
        }

        return "/" + Path.Combine(relativeDir, fileName).Replace('\\', '/');
    }

    public async Task DeleteAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        if (IsCloudConfigured && imagePath.StartsWith(_publicBaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            var key = imagePath[(_publicBaseUrl.Length + 1)..];
            await _s3Client!.DeleteObjectAsync(_bucketName, key, cancellationToken);
            return;
        }

        var relativePath = imagePath.TrimStart('/');
        var absolutePath = Path.Combine(_webRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
    }
}
