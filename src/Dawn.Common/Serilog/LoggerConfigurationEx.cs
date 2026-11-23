#if NET10_0_WINDOWS_OR_GREATER
using System.Security.Principal;
#endif
using Dawn.Common.Models;
using Dawn.Common.Serilog.Enrichers;
using Dawn.Common.Serilog.Themes;
using Serilog.Events;

namespace Dawn.Common.Serilog;

public static class LoggerConfigurationEx
{
    public const string LOGGING_FORMAT = "{Level:u1} {Timestamp:yyyy-MM-dd HH:mm:ss.ffffff}   [{Source}] {Message:lj}{NewLine}{Exception}";
    
        
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
        return Process.GetCurrentProcess().ProcessName + ".log";
        #endif
    }
    
    extension(LoggerConfiguration config)
    {
        public LoggerConfiguration AddCommon(CommonLoggingOptions options) =>
            config.AddCommon(options, new DirectoryInfo(Environment.CurrentDirectory));
        public LoggerConfiguration AddCommon(CommonLoggingOptions options, DirectoryInfo logDirectory)
        {
            options.Deconstruct(out var isConsoleLogging, out var isFileLogging, out var isExtendedLogging, out var customSeqUrl);

            config.MinimumLevel.Is(LogEventLevel.Verbose);

            if (isConsoleLogging)
            {
                config.Enrich.With<ClassNameEnricher>()
                    .Enrich.WithProcessName()
                    .Enrich.FromLogContext()
                    .WriteTo.Console(
                        theme: SerilogBlizzardTheme.GetTheme,
                        outputTemplate: LOGGING_FORMAT,
                        restrictedToMinimumLevel: LogEventLevel.Verbose,
                        applyThemeToRedirectedOutput: true
                    );
            }
            
            if (isFileLogging)
            {
                config.WriteTo.File(Path.Combine(logDirectory.FullName, GetLogFileName()),
                    restrictedToMinimumLevel: isExtendedLogging 
                        ? LogEventLevel.Verbose 
                        : LogEventLevel.Information,
                    outputTemplate: LOGGING_FORMAT,
                    retainedFileCountLimit: 1,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: (long)Math.Pow(1024, 2) * 20, // 20mb
                    flushToDiskInterval: TimeSpan.FromSeconds(1));
            }

                #if RELEASE
                config.WriteTo.Seq(string.IsNullOrWhiteSpace(customSeqUrl)
                        ? "http://localhost:9999"
                        : customSeqUrl,
                        restrictedToMinimumLevel: isExtendedLogging 
                            ? LogEventLevel.Verbose 
                            : LogEventLevel.Information);
                #endif

            return config;
        }
    }
}