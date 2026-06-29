namespace RightKeyboard;

public sealed class LayoutSelectionDialog : Form
{
    private readonly FlowLayoutPanel layoutList;
    private readonly Button acceptButton;
    private readonly TextBox customNameTextBox;
    private readonly KeyboardDevice device;
    private RadioButton? selectedLayoutButton;

    public LayoutSelectionDialog(KeyboardDevice device, string suggestedName)
    {
        this.device = device;
        SetStyle(ControlStyles.ApplyThemingImplicitly, true);

        Text = "Seleccionar distribución del teclado";
        AccessibleDescription = "Asigna un nombre y una distribución al teclado detectado.";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = SystemFonts.MessageBoxFont;
        ClientSize = new Size(620, 590);
        MinimumSize = new Size(540, 500);
        Padding = new Padding(28);
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
            Font = new Font(Font.FontFamily, Font.Size + 3, FontStyle.Bold),
            Text = "Configura este teclado",
            Margin = new Padding(0, 0, 0, 16)
        };

        TableLayoutPanel devicePanel = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Margin = new Padding(0, 0, 0, 18),
            Padding = new Padding(16),
            BackColor = SystemColors.Window,
            ForeColor = SystemColors.WindowText
        };
        devicePanel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Text = "Nombre para este teclado",
            Margin = new Padding(0, 0, 0, 6)
        });
        customNameTextBox = new TextBox
        {
            Dock = DockStyle.Top,
            Text = suggestedName,
            PlaceholderText = $"Teclado {device.TechnicalId.Split(' ').LastOrDefault()}",
            Margin = new Padding(0, 0, 0, 8),
            AccessibleName = "Nombre para este teclado",
            AccessibleDescription = "Alias opcional para reconocer el dispositivo más adelante."
        };
        devicePanel.Controls.Add(customNameTextBox);
        Label detectedDeviceLabel = new()
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Text = $"Detectado: {device.DisplayName}  ·  {device.TechnicalId}",
            MaximumSize = new Size(420, 0),
            AccessibleName = "Información detectada del dispositivo",
            AccessibleDescription = $"Nombre detectado: {device.DisplayName}. Identificador técnico: {device.TechnicalId}."
        };
        devicePanel.Controls.Add(detectedDeviceLabel);

        Panel listContainer = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(1),
            BackColor = SystemColors.ControlLight,
            Margin = new Padding(0)
        };
        layoutList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(12),
            BackColor = SystemColors.Window,
            AccessibleName = "Idiomas y distribuciones de teclado",
            AccessibleDescription = "Usa Tab para entrar en la lista y las flechas para cambiar la selección."
        };
        layoutList.SizeChanged += (_, _) => ResizeLayoutRows();
        listContainer.Controls.Add(layoutList);

        Label help = new()
        {
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Text = "Elige una distribución. Si el dispositivo no es un teclado, puedes ignorarlo y recuperarlo después desde Configuración.",
            MaximumSize = new Size(460, 0),
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
        acceptButton = ActionButton("&Aceptar");
        acceptButton.Enabled = false;
        acceptButton.Click += (_, _) => AcceptSelection();
        Button cancelButton = ActionButton("&Cancelar");
        cancelButton.DialogResult = DialogResult.Cancel;
        Button ignoreButton = ActionButton("&Ignorar este dispositivo");
        ignoreButton.Margin = new Padding(0);
        ignoreButton.AccessibleDescription = "Guarda el dispositivo como ignorado; puede recuperarse desde Configuración.";
        ignoreButton.Click += (_, _) => IgnoreDevice();
        actions.Controls.Add(acceptButton);
        actions.Controls.Add(cancelButton);
        actions.Controls.Add(ignoreButton);

        root.Controls.Add(heading, 0, 0);
        root.Controls.Add(devicePanel, 0, 1);
        root.Controls.Add(listContainer, 0, 2);
        root.Controls.Add(help, 0, 3);
        root.Controls.Add(actions, 0, 4);
        Controls.Add(root);
        AcceptButton = acceptButton;
        CancelButton = cancelButton;

        LoadLayouts();
        Shown += (_, _) =>
        {
            customNameTextBox.SelectAll();
            customNameTextBox.Focus();
        };
    }

    public Layout? SelectedLayout { get; private set; }

    public string CustomName => customNameTextBox.Text.Trim();

    private void LoadLayouts()
    {
        foreach (IGrouping<string, RightKeyboard.Layout> language in RightKeyboard.Layout.EnumerateLayouts()
                     .OrderBy(layout => layout.LanguageName, StringComparer.CurrentCultureIgnoreCase)
                     .ThenBy(layout => layout.LayoutName, StringComparer.CurrentCultureIgnoreCase)
                     .GroupBy(layout => layout.LanguageName))
        {
            layoutList.Controls.Add(new Label
            {
                AutoSize = false,
                Height = 34,
                Text = language.Key,
                Font = new Font(Font, FontStyle.Bold),
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(4, 0, 0, 4),
                Margin = new Padding(0, 4, 0, 2)
            });

            foreach (Layout layout in language)
            {
                RadioButton row = new()
                {
                    Appearance = Appearance.Button,
                    AutoSize = false,
                    Height = 42,
                    FlatStyle = FlatStyle.Flat,
                    Text = layout.LayoutName,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(16, 0, 12, 0),
                    Margin = new Padding(0, 0, 0, 4),
                    Tag = layout,
                    AccessibleName = $"{language.Key}, {layout.LayoutName}",
                    AccessibleDescription = "Distribución disponible. Usa las flechas para recorrer la lista."
                };
                row.FlatAppearance.BorderColor = SystemColors.ControlLight;
                row.FlatAppearance.CheckedBackColor = SystemColors.GradientActiveCaption;
                row.CheckedChanged += (_, _) =>
                {
                    if (row.Checked)
                    {
                        selectedLayoutButton = row;
                        acceptButton.Enabled = true;
                    }
                };
                row.DoubleClick += (_, _) =>
                {
                    row.Checked = true;
                    AcceptSelection();
                };
                layoutList.Controls.Add(row);
            }
        }

        ResizeLayoutRows();
        RadioButton? first = layoutList.Controls.OfType<RadioButton>().FirstOrDefault();
        if (first is not null)
        {
            first.Checked = true;
        }
    }

    private void ResizeLayoutRows()
    {
        int width = Math.Max(280, layoutList.ClientSize.Width - layoutList.Padding.Horizontal - 20);
        foreach (Control control in layoutList.Controls)
        {
            control.Width = width;
        }
    }

    private void AcceptSelection()
    {
        if (selectedLayoutButton?.Tag is not Layout layout)
        {
            return;
        }

        SelectedLayout = layout;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void IgnoreDevice()
    {
        if (MessageBox.Show(
                $"RightKeyboard dejará de reaccionar a \"{CustomName}\".\n\n" +
                "Puedes recuperarlo desde Configuración o usando Limpiar preferencias.",
                "Ignorar dispositivo",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button2) != DialogResult.OK)
        {
            return;
        }

        DialogResult = DialogResult.Ignore;
        Close();
    }

    private static Button ActionButton(string text) => new()
    {
        Text = text,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        MinimumSize = new Size(88, 36),
        Margin = new Padding(8, 0, 0, 0),
        Padding = new Padding(12, 4, 12, 4),
        UseVisualStyleBackColor = true
    };
}
