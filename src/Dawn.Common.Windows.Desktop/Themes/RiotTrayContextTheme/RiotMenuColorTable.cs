namespace Dawn.Common.Windows.Desktop.Themes.RiotTrayContextTheme;

public class RiotMenuColorTable(Color primaryColor) : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground { get; } = Color.White;
    public override Color MenuBorder => RiotContextMenuDefaults.ContextMenuStripBorderColor;
    public override Color MenuItemBorder => primaryColor;
    public override Color MenuItemSelected => primaryColor;
    public override Color ImageMarginGradientBegin => Color.Transparent;
    public override Color ImageMarginGradientMiddle => Color.Transparent;
    public override Color ImageMarginGradientEnd => Color.Transparent;
}