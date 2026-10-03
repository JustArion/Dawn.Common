namespace Dawn.Common.Windows.Desktop.Extensions;

public static class ToolStripItemCollectionEx
{
    extension(ToolStripItemCollection items)
    {
        public void Add(string text, EventHandler onClick) => items.Add(text, null, onClick);
    }
}