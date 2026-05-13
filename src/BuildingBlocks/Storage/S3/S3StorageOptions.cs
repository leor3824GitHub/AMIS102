namespace AMIS.Framework.Storage.S3;

public sealed class S3StorageOptions
{
    public string? Bucket { get; set; }
    public string? Region { get; set; }
    public string? Prefix { get; set; }
    public bool PublicRead { get; set; } = true;
    public string? PublicBaseUrl { get; set; }
}

