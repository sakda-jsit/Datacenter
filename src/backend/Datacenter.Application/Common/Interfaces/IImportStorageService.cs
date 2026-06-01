namespace Datacenter.Application.Common.Interfaces;

/// <summary>
/// Resolves the file system path for a client's Express DBF folder.
/// Base path is configured in appsettings "Import:ExpressBasePath".
/// </summary>
public interface IImportStorageService
{
    /// <summary>
    /// Returns full path to the client's Express folder.
    /// e.g. D:\ExpressI\ABC001
    /// </summary>
    string GetExpressFolderPath(string clientCode);

    /// <summary>True when the folder exists on the file system.</summary>
    bool ExpressFolderExists(string clientCode);
}
