namespace Dawn.Common.Extensions;

public class FileInfoComparer(StringComparison comparison = StringComparison.CurrentCultureIgnoreCase) : IEqualityComparer<FileInfo>, IAlternateEqualityComparer<string, FileInfo>
{
    public static readonly FileInfoComparer Default = new();
    public bool Equals(FileInfo? x, FileInfo? y)
    {
        if (ReferenceEquals(x, y)) 
            return true;
        
        if (x is null) 
            return false;
        
        return y is not null && string.Equals(x.FullName, y.FullName, comparison);
    }

    public int GetHashCode(FileInfo obj) => StringComparer.CurrentCultureIgnoreCase.GetHashCode(obj.FullName);

    public bool Equals(string? alternate, FileInfo other) => other.FullName.Equals(alternate, comparison);

    public int GetHashCode(string alternate) => alternate.GetHashCode();

    public FileInfo Create(string alternate) => new(alternate);
}