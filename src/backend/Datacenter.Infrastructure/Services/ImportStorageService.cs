using Datacenter.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Datacenter.Infrastructure.Services;

public class ImportStorageService(IConfiguration configuration) : IImportStorageService
{
    private readonly string _basePath = configuration["Import:ExpressBasePath"]
        ?? throw new InvalidOperationException("Import:ExpressBasePath is not configured in appsettings.");

    public string GetExpressFolderPath(string clientCode)
        => Path.Combine(_basePath, clientCode);

    public bool ExpressFolderExists(string clientCode)
        => Directory.Exists(GetExpressFolderPath(clientCode));
}
