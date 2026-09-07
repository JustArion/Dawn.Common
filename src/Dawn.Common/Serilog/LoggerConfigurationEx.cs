#if NET10_0_WINDOWS_OR_GREATER
using System.Security.Principal;
#endif
using Dawn.Common.Models;
using Dawn.Common.Serilog.Enrichers;
using Dawn.Common.Serilog.Themes;
using Serilog;
using Serilog.Events;

namespace Dawn.Common.Serilog;

public static class LoggerConfigurationEx
{
    private const string LOGGING_FORMAT = "{Level:u1} {Timestamp:yyyy-MM-dd HH:mm:ss.ffffff}   [{Source}] {Message:lj}{NewLine}{Exception}";
    
    #if NET10_0_WINDOWS_OR_GREATER
    private static readonly Lazy<bool> isAdmin = new(() => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator));

    private static bool IsAdmin() => isAdmin.Value;
    #endif
    private static string GetLogFileName()
    {
        #if NET10_0_WINDOWS_OR_GREATER
        var logFileType = IsAdmin() ? "_Admin" : "_User";
        return $"{Application.ProductName}{logFileType}.log";
        #else
        return $"{Process.GetCurrentProcess().ProcessName}.log";
        #endif
    }

    extension(LoggerConfiguration config)
    {
        public LoggerConfiguration AddCommon(CommonLoggingOptions options)
        {
            config.MinimumLevel.Is(LogEventLevel.Verbose)
                .Enrich.With<ClassNameEnricher>()
                .Enrich.WithProcessName()
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    theme: SerilogBlizzardTheme.GetTheme,
                    outputTemplate: LOGGING_FORMAT,
                    applyThemeToRedirectedOutput: true
                );
            
            options.Deconstruct(out var includeFileLogging, out var extendedLogging, out var customSeqUrl );

            if (includeFileLogging)
            {
                config.WriteTo.File(Path.Combine(AppContext.BaseDirectory, GetLogFileName()),
                    outputTemplate: LOGGING_FORMAT,
                    restrictedToMinimumLevel: extendedLogging
                        ? LogEventLevel.Verbose
                        : LogEventLevel.Information,
                    retainedFileCountLimit: 1,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: (long)Math.Pow(1024, 2) * 20, // 20mb
                    flushToDiskInterval: TimeSpan.FromSeconds(1));
            }


            config.WriteTo.Seq(string.IsNullOrWhiteSpace(customSeqUrl)
                    ? "http://localhost:9999"
                    : customSeqUrl,
                restrictedToMinimumLevel: extendedLogging
                    ? LogEventLevel.Verbose
                    : LogEventLevel.Information);

            return config;
        }
    }
}