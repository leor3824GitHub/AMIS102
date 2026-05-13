using Amazon;
using Amazon.S3;
using AMIS.Framework.Storage.Local;
using AMIS.Framework.Storage.S3;
using AMIS.Framework.Storage.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AMIS.Framework.Storage;

public static class Extensions
{
    public static IServiceCollection AddHeroLocalFileStorage(this IServiceCollection services)
    {
        services.AddScoped<IStorageService, LocalStorageService>();
        return services;
    }

    public static IServiceCollection AddHeroStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Storage:Provider"]?.ToLowerInvariant();

        if (string.Equals(provider, "s3", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<S3StorageOptions>(configuration.GetSection("Storage:S3"));

            services.AddSingleton<IAmazonS3>(sp =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<S3StorageOptions>>().Value;

                if (string.IsNullOrWhiteSpace(options.Bucket))
                {
                    throw new InvalidOperationException("Storage:S3:Bucket is required when using S3 storage.");
                }

                if (string.IsNullOrWhiteSpace(options.Region))
                {
                    return new AmazonS3Client();
                }

                return new AmazonS3Client(RegionEndpoint.GetBySystemName(options.Region));
            });

            services.AddTransient<IStorageService, S3StorageService>();
        }
        else
        {
            services.AddScoped<IStorageService, LocalStorageService>();
        }

        return services;
    }
}

