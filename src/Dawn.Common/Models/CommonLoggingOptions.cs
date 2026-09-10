namespace Dawn.Common.Models;

public record struct CommonLoggingOptions(bool IncludeFileLogging, bool ExtendedLogging, string CustomSEQUrl)
{
    public static CommonLoggingOptions FromLaunchArgs(StandardLaunchArgs args) => 
        new(
            args.FileLogging,
            args.ExtendedLogging,
            args.HasCustomSeqUrl 
                ? args.CustomSeqUrl 
                : string.Empty);

    public static implicit operator CommonLoggingOptions(StandardLaunchArgs args) => FromLaunchArgs(args);
}