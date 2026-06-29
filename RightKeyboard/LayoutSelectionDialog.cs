namespace RightKeyboard;

public sealed class LayoutSelectionDialog : Form
{
    private readonly TreeView layoutTree;
    private readonly Button acceptButton;
    private readonly Button cancelButton;
    private readonly Button ignoreButton;
    private readonly KeyboardDevice device;

    public LayoutSelectionDialog(KeyboardDevice device)
    {
        this.device = device;
        SetStyle(ControlStyles.ApplyThemingImplicitly, true);

        Text = "Seleccionar distribución del teclado";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(590, 500);
        MinimumSize = new Size(520, 430);
        Padding = new Padding(24);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Label heading = new()
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Text = "Selecciona la distribución para este teclado",
            Margin = new Padding(0, 0, 0, 10)
        };

        TableLayoutPanel devicePanel = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Margin = new Padding(0, 0, 0, 18),
            Padding = new Padding(14),
            BackColor = SystemColors.Window,
            ForeColor = SystemColors.WindowText
        };
        devicePanel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font(Font.FontFamily, Font.Size + 1, FontStyle.Bold),
            Text = device.DisplayName,
            AccessibleName = "Nombre del dispositivo"
        });
        devicePanel.Controls.Add(new Label
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Text = device.TechnicalId,
            Margin = new Padding(0, 5, 0, 0),
            AccessibleName = "Identificador técnico del dispositivo"
        });

        layoutTree = new TreeView
        {
            Dock = DockStyle.Fill,
            HideSelection = false,
            FullRowSelect = true,
            ShowLines = true,
            ShowPlusMinus = true,
            ShowRootLines = false,
            ItemHeight = 26,
            BorderStyle = BorderStyle.FixedSingle,
            AccessibleName = "Idiomas y distribuciones de teclado",
            Margin = new Padding(0)
        };
        layoutTree.AfterSelect += (_, _) => UpdateAcceptButton();
        layoutTree.NodeMouseDoubleClick += (_, args) =>
        {
            if (args.Node?.Tag is RightKeyboard.Layout)
            {
                AcceptSelection();
            }
        };

        Label help = new()
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Text = "Selecciona una distribución dentro de un idioma. Si no es un teclado, puedes ignorar el dispositivo.",
            MaximumSize = new Size(540, 0),
            Margin = new Padding(0, 14, 0, 14)
        };

        FlowLayoutPanel actions = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0)
        };

        acceptButton = new Button
        {
            Text = "Aceptar",
            AutoSize = true,
            Enabled = false,
            Margin = new Padding(8, 0, 0, 0),
            Padding = new Padding(12, 4, 12, 4)
        };
        acceptButton.Click += (_, _) => AcceptSelection();

        cancelButton = new Button
        {
            Text = "Cancelar",
            AutoSize = true,
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(8, 0, 0, 0),
            Padding = new Padding(12, 4, 12, 4)
        };

        ignoreButton = new Button
        {
            Text = "Ignorar este dispositivo",
            AutoSize = true,
            Margin = new Padding(0),
            Padding = new Padding(12, 4, 12, 4)
        };
        ignoreButton.Click += (_, _) => IgnoreDevice();

        actions.Controls.Add(acceptButton);
        actions.Controls.Add(cancelButton);
        actions.Controls.Add(ignoreButton);

        root.Controls.Add(heading, 0, 0);
        root.Controls.Add(devicePanel, 0, 1);
        root.Controls.Add(layoutTree, 0, 2);
        root.Controls.Add(help, 0, 3);
        root.Controls.Add(actions, 0, 4);
        Controls.Add(root);

        AcceptButton = acceptButton;
        CancelButton = cancelButton;
        LoadLayouts();
    }

    public RightKeyboard.Layout? SelectedLayout { get; private set; }

    private void LoadLayouts()
    {
        layoutTree.BeginUpdate();
        try
        {
            foreach (IGrouping<string, RightKeyboard.Layout> language in RightKeyboard.Layout.EnumerateLayouts()
                         .OrderBy(layout => layout.LanguageName, StringComparer.CurrentCultureIgnoreCase)
                         .ThenBy(layout => layout.LayoutName, StringComparer.CurrentCultureIgnoreCase)
                         .GroupBy(layout => layout.LanguageName))
            {
                TreeNode languageNode = new(language.Key)
                {
                    NodeFont = new Font(layoutTree.Font, FontStyle.Bold)
                };

                foreach (RightKeyboard.Layout layout in language)
                {
                    languageNode.Nodes.Add(new TreeNode(layout.LayoutName) { Tag = layout });
                }

                layoutTree.Nodes.Add(languageNode);
                languageNode.Expand();
            }

            TreeNode? firstLayout = layoutTree.Nodes.Cast<TreeNode>()
                .SelectMany(node => node.Nodes.Cast<TreeNode>())
                .FirstOrDefault();
            layoutTree.SelectedNode = firstLayout;
            firstLayout?.EnsureVisible();
        }
        finally
        {
            layoutTree.EndUpdate();
        }
    }

    private void UpdateAcceptButton() => acceptButton.Enabled = layoutTree.SelectedNode?.Tag is RightKeyboard.Layout;

    private void AcceptSelection()
    {
        if (layoutTree.SelectedNode?.Tag is not RightKeyboard.Layout layout)
        {
            return;
        }

        SelectedLayout = layout;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void IgnoreDevice()
    {
        DialogResult confirmation = MessageBox.Show(
            $"RightKeyboard dejará de reaccionar a \"{device.DisplayName}\".\n\n" +
            "Puedes recuperar el dispositivo usando Limpiar preferencias.",
            "Ignorar dispositivo",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Information,
            MessageBoxDefaultButton.Button2);

        if (confirmation == DialogResult.OK)
        {
            DialogResult = DialogResult.Ignore;
            Close();
        }
    }
}
