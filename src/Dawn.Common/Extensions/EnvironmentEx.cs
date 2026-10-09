using DotNetEnv;

namespace Dawn.Common.Extensions;

public static class EnvironmentEx
{
    private static Dictionary<string, string>? _additionalVariables;
    private static readonly Lazy<Dictionary<string, string>> _additionalVariablesLoaded = new(() =>
    {
        // This might throw an unauthorized exception if we don't have permissions to read it, we just don't read further when that happens
        try
        {
            return _additionalVariables = Env.TraversePath()
                .Load()
                .ToDictionary(x => x.Key, x => x.Value);
        }
        catch
        {
            return [];
        }
    });
    
    extension(Environment)
    {
        public static Dictionary<string, string> LoadAdditionalVariables() => _additionalVariablesLoaded.Value;
        public static Dictionary<string, string> AdditionalVariables => _additionalVariables ?? [];
    }
}