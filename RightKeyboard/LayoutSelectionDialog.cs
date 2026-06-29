namespace RightKeyboard;

public partial class LayoutSelectionDialog : Form
{
    public LayoutSelectionDialog()
    {
        InitializeComponent();
        LoadLanguageList();
    }

    public Layout? SelectedLayout { get; private set; }

    private void LoadLanguageList()
    {
        lbLayouts.Items.Clear();
        foreach (RightKeyboard.Layout layout in RightKeyboard.Layout.EnumerateLayouts())
        {
            lbLayouts.Items.Add(layout);
        }

        if (lbLayouts.Items.Count > 0)
        {
            lbLayouts.SelectedIndex = 0;
        }

        btOk.Enabled = lbLayouts.SelectedItem is not null;
    }

    private void btOk_Click(object sender, EventArgs e)
    {
        if (lbLayouts.SelectedItem is not RightKeyboard.Layout layout)
        {
            return;
        }

        SelectedLayout = layout;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void lbLayouts_SelectedIndexChanged(object sender, EventArgs e) =>
        btOk.Enabled = lbLayouts.SelectedItem is not null;

    private void lbLayouts_DoubleClick(object sender, EventArgs e)
    {
        if (btOk.Enabled)
        {
            btOk.PerformClick();
        }
    }
}
