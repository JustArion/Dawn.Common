namespace Dawn.Common.Models;

public readonly record struct CommonLoggingOptions(bool IncludeConsoleLogging, bool IncludeFileLogging, bool ExtendedLogging, string CustomSEQUrl)
{
    public static CommonLoggingOptions FromLaunchArgs(StandardLaunchArgs args) => 
        new(
            true,
            args.FileLogging,
            args.ExtendedLogging,
            args.HasCustomSeqUrl 
                ? args.CustomSeqUrl 
                : string.Empty);

    public static implicit operator CommonLoggingOptions(StandardLaunchArgs args) => FromLaunchArgs(args);
}