namespace Dawn.Common.Models;

public abstract record StandardLaunchArgs
{
    protected StandardLaunchArgs(string[] args, bool checkEnv)
    {
        Args = args;
        CommandLine = string.Join(" ", args);
        CheckEnvironment = checkEnv;
        
        ExtendedLogging = Contains("Extended Logging");
        NoFileLogging = Contains("No File Logging");

        CustomSeqUrl = ExtractArgumentValue("SEQ URL");
        HasCustomSeqUrl = Uri.TryCreate(CustomSeqUrl, UriKind.Absolute, out _);

        if (!int.TryParse(ExtractArgumentValue("Bind To"), out var pid)) 
            return;
        
        ProcessBinding = pid;
        HasProcessBinding = true;
    }

    protected string[] Args { get; }
    public IReadOnlyList<string> RawArgs => Args;
    public string CommandLine { get; init; }
    protected bool CheckEnvironment { get; }
    
    // Args
    
    /// Whether to log to a .log file in the App folder
    public bool NoFileLogging { get; }
    public bool ExtendedLogging { get; init; } // More verbose logging
    
    /// The target Url for Seq to send logs to
    public bool HasCustomSeqUrl { get; }
    public string CustomSeqUrl { get; }
    
    /// The process to bind to that when the bound pid exits, so does this process
    public bool HasProcessBinding { get; }
    public int ProcessBinding { get; }
    
    // Inversion
    public bool FileLogging => !NoFileLogging;
    
    // Methods

    protected string ExtractArgumentValue(string key) => ExtractArgumentValue(key, Args, CheckEnvironment);
    protected bool Contains(string key) => Contains(key, Args, CheckEnvironment);
    
    public static string ExtractArgumentValue(string argumentKey, string[] args, bool checkEnv = false)
    {
        argumentKey = ToKebabCase(argumentKey);
        var prefix = $"--{argumentKey}=";
        var rawArgument = args.FirstOrDefault(x => x.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase));

        if (checkEnv && string.IsNullOrWhiteSpace(rawArgument))
            return Environment.GetEnvironmentVariable(argumentKey) ?? string.Empty;

        var keyValue = rawArgument.TrimStart(prefix);

        return keyValue.Length > 1 ? keyValue.ToString() : string.Empty;
    }

    public static bool Contains(string key, string[] cliArgs, bool checkEnv = false) => cliArgs.Contains($"--{key = ToKebabCase(key)}", StringComparer.InvariantCultureIgnoreCase) || (checkEnv && Environment.GetEnvironmentVariable(key) is not null);

    public static string ToKebabCase(string str) => str.ToLower().Replace(' ', '-');
}