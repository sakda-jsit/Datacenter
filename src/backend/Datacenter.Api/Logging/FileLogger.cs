using System.Collections.Concurrent;
using System.Text;

namespace Datacenter.Api.Logging;

/// <summary>
/// เขียน log ลงไฟล์รายวัน (ไม่พึ่ง package ภายนอก) — บน production ต้องมีร่องรอยย้อนหลัง
/// เพราะ console log หายไปเมื่อ service restart. ตั้งค่าได้ที่ section "Logging:File".
/// </summary>
public class FileLoggerOptions
{
    public const string SectionName = "Logging:File";

    public bool Enabled { get; set; } = true;
    /// <summary>โฟลเดอร์เก็บ log (relative = อิงจากโฟลเดอร์ของแอป)</summary>
    public string Directory { get; set; } = "logs";
    /// <summary>ชื่อไฟล์นำหน้า — ไฟล์จริงคือ {Prefix}-yyyyMMdd.log</summary>
    public string FilePrefix { get; set; } = "datacenter";
    /// <summary>ระดับต่ำสุดที่บันทึก (Trace/Debug/Information/Warning/Error/Critical)</summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
    /// <summary>เก็บ log ย้อนหลังกี่วัน (ลบไฟล์ที่เก่ากว่านี้; 0 = ไม่ลบ)</summary>
    public int RetainedDays { get; set; } = 90;
}

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly FileLoggerOptions _options;
    private readonly string _directory;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private readonly object _writeLock = new();
    private DateOnly _currentDate = DateOnly.MinValue;
    private string _currentPath = "";

    public FileLoggerProvider(FileLoggerOptions options, string contentRootPath)
    {
        _options = options;
        _directory = Path.IsPathRooted(options.Directory)
            ? options.Directory
            : Path.Combine(contentRootPath, options.Directory);
    }

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, name => new FileLogger(name, this, _options.MinimumLevel));

    internal void Write(string line)
    {
        try
        {
            lock (_writeLock)
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                if (today != _currentDate)
                {
                    System.IO.Directory.CreateDirectory(_directory);
                    _currentPath = Path.Combine(_directory, $"{_options.FilePrefix}-{today:yyyyMMdd}.log");
                    _currentDate = today;
                    PurgeOldFiles(today);
                }

                File.AppendAllText(_currentPath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
            // log ต้องไม่ทำให้ระบบล้ม (ดิสก์เต็ม/ไฟล์ถูกล็อก) — ปล่อยผ่านเงียบ ๆ
        }
    }

    private void PurgeOldFiles(DateOnly today)
    {
        if (_options.RetainedDays <= 0) return;
        var cutoff = today.AddDays(-_options.RetainedDays);
        foreach (var file in System.IO.Directory.EnumerateFiles(_directory, $"{_options.FilePrefix}-*.log"))
        {
            var stamp = Path.GetFileNameWithoutExtension(file).Split('-').LastOrDefault();
            if (stamp is null || stamp.Length != 8) continue;
            if (!DateOnly.TryParseExact(stamp, "yyyyMMdd", out var fileDate)) continue;
            if (fileDate < cutoff)
            {
                try { File.Delete(file); } catch { /* ไฟล์ถูกใช้งาน — ข้าม */ }
            }
        }
    }

    public void Dispose() => _loggers.Clear();
}

internal sealed class FileLogger(string category, FileLoggerProvider provider, LogLevel minimumLevel) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= minimumLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var sb = new StringBuilder()
            .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Append(" [").Append(Short(logLevel)).Append("] ")
            .Append(category).Append(" — ")
            .Append(formatter(state, exception));

        if (exception is not null)
            sb.AppendLine().Append(exception);

        provider.Write(sb.ToString());
    }

    private static string Short(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRT",
        _ => "???",
    };
}

public static class FileLoggerExtensions
{
    /// <summary>เปิด log ลงไฟล์ตาม config section "Logging:File" (ปิดได้ด้วย Enabled=false)</summary>
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, IConfiguration configuration)
    {
        var options = new FileLoggerOptions();
        configuration.GetSection(FileLoggerOptions.SectionName).Bind(options);
        if (!options.Enabled) return builder;

        var contentRoot = configuration[Microsoft.Extensions.Hosting.HostDefaults.ContentRootKey]
                          ?? AppContext.BaseDirectory;
        builder.AddProvider(new FileLoggerProvider(options, contentRoot));
        return builder;
    }
}
