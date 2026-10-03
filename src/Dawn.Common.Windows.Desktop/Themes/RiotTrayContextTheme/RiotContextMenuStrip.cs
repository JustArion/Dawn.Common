using System.ComponentModel;

namespace Dawn.Common.Windows.Desktop.Themes.RiotTrayContextTheme;

public sealed class RiotContextMenuStrip : ContextMenuStrip
{
    public RiotContextMenuStrip()
    {
        BackColor = RiotContextMenuDefaults.ContextMenuStripBackgroundColor;
        AllowTransparency = true;
        AutoSize = false;
        ShowItemToolTips = false;
    }


    protected override void OnOpening(CancelEventArgs e)
    {
        SetSize();
        HandleSubmenus(Items);
    }

    private readonly List<ToolStripDropDown> _hoverFunctionalityAdded = [];

    private void HandleSubmenus(ToolStripItemCollection ic)
    {
        foreach (ToolStripItem it in ic)
        {
            if (it is not ToolStripDropDownItem item)
                continue;
            
            var dd = item.DropDown;
            dd.DefaultDropDownDirection = ToolStripDropDownDirection.Left;
            
            var items = item.DropDownItems;
            
            if (!_hoverFunctionalityAdded.Contains(dd))
            {
                item.MouseEnter += (_, _) => item.ShowDropDown();
                _hoverFunctionalityAdded.Add(dd);
            }
            
            dd.AutoSize = false;
            dd.Size = SyncSize(items);
            
            item.BackColor = RiotContextMenuDefaults.ContextMenuStripBackgroundColor;
            HandleSubmenus(item.DropDownItems);
        }
    }

    private static Size SyncSize(ToolStripItemCollection ic)
    {
        var height = RiotContextMenuDefaults.DefaultHeight;
        foreach (ToolStripItem ctxItem in ic)
        {
            if (ctxItem is ToolStripSeparator)
                height += RiotContextMenuDefaults.SeparatorHeight;
            else height += RiotContextMenuDefaults.ItemHeight;
        }

        return new Size(RiotContextMenuDefaults.DefaultWidth, height);
    }

    private void SetSize()
    {
        var height = RiotContextMenuDefaults.DefaultHeight;
        foreach (ToolStripItem ctxItem in Items)
        {
            if (ctxItem is ToolStripSeparator)
                height += RiotContextMenuDefaults.SeparatorHeight;
            else height += RiotContextMenuDefaults.ItemHeight;
        }
        
        Size  = new Size(RiotContextMenuDefaults.DefaultWidth, height);
    }

    private Bitmap? menuItemHeaderSize;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool IsMainMenu { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int MenuItemHeight { get; set; } = 25;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color MenuItemTextColor { get; set; } = Color.Empty;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color PrimaryColor { get; set; } = Color.Empty;

    private void LoadMenuItemHeight()
    {
        menuItemHeaderSize = IsMainMenu ? new Bitmap(25, 45) : new Bitmap(20, MenuItemHeight);

        foreach (ToolStripItem menuItem in Items)
        {
            RecursivelySetMenuImage(menuItem);
        }
    }

    private void RecursivelySetMenuImage(ToolStripItem item, int depth = 0, int maxDepth = 10)
    {
        if (item is ToolStripSeparator)
            return;
        item.ImageScaling = ToolStripItemImageScaling.None;
        item.Image ??= menuItemHeaderSize;

        if (item is not ToolStripDropDownItem tsmi) 
            return;
        
        if (++depth > maxDepth)
            return;
        
        foreach (ToolStripItem subItem in tsmi.DropDownItems)
            RecursivelySetMenuImage(subItem, depth);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (DesignMode) 
            return;
        
        Renderer = new RiotMenuRenderer();
        LoadMenuItemHeight();
    }
}