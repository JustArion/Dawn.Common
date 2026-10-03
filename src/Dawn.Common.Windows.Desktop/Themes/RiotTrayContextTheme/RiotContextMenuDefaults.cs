namespace Dawn.Common.Windows.Desktop.Themes.RiotTrayContextTheme;

public static class RiotContextMenuDefaults
{
    public static Color ContextMenuStripBackgroundColor { get; set; } = Color.FromArgb(0x2B, 0x2B, 0x2B);
    public static Color ContextMenuStripBorderColor { get; set; } = Color.FromArgb(0xA0, 0xA0, 0xA0);
    public static Color ContextMenuStripTextColor { get; set; } = Color.FromArgb(0xf9, 0xf9, 0xf9);

    public static int SeparatorHeight { get; set; } = 0;
    public static int ItemHeight { get; set; } = 34;
    public static int DefaultHeight { get; set; } = 3;
    public static int DefaultWidth { get; set; } = 266;
}