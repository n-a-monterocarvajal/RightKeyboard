namespace RightKeyboard;

internal sealed class SettingsDialog : Form
{
    private readonly Configuration configuration;
    private readonly KeyboardDevicesCollection devices;
    private readonly Action saveConfiguration;
    private readonly Layout[] layouts;
    private readonly FlowLayoutPanel deviceList;
    private readonly Label emptyLabel;
    private readonly Label detectedNameLabel;
    private readonly Label technicalIdLabel;
    private readonly Label statusLabel;
    private readonly TextBox customNameTextBox;
    private readonly ComboBox layoutComboBox;
    private readonly CheckBox ignoredCheckBox;
    private readonly CheckBox startupCheckBox;
    private readonly Button saveButton;
    private readonly Button forgetButton;
    private string? selectedIdentity;

    public SettingsDialog(
        Configuration configuration,
        KeyboardDevicesCollection devices,
        Action saveConfiguration)
    {
        this.configuration = configuration;
        this.devices = devices;
        this.saveConfiguration = saveConfiguration;
        layouts = RightKeyboard.Layout.EnumerateLayouts()
            .OrderBy(layout => layout.LanguageName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(layout => layout.LayoutName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        SetStyle(ControlStyles.ApplyThemingImplicitly, true);
        Text = "Configuración de RightKeyboard";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(800, 540);
        ClientSize = new Size(920, 620);
        AutoScaleMode = AutoScaleMode.Dpi;
        Padding = new Padding(24);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        Label title = new()
        {
            AutoSize = true,
            Text = "Teclados y preferencias",
            Font = new Font(Font.FontFamily, Font.Size + 4, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 20)
        };

        TableLayoutPanel content = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));

        Panel leftPanel = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 18, 0)
        };
        deviceList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 0, 8, 0),
            AccessibleName = "Dispositivos conocidos"
        };
        deviceList.SizeChanged += (_, _) => ResizeDeviceButtons();
        emptyLabel = new Label
        {
            AutoSize = true,
            Text = "Aún no hay dispositivos recordados.",
            ForeColor = SystemColors.GrayText,
            Padding = new Padding(8)
        };
        leftPanel.Controls.Add(deviceList);

        TableLayoutPanel editor = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 11,
            Padding = new Padding(20),
            BackColor = SystemColors.Window,
            ForeColor = SystemColors.WindowText
        };
        editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        editor.Controls.Add(SectionLabel("Nombre para este teclado"));
        customNameTextBox = new TextBox
        {
            Dock = DockStyle.Top,
            PlaceholderText = "Nombre reconocible",
            Margin = new Padding(0, 6, 0, 16)
        };
        editor.Controls.Add(customNameTextBox);

        detectedNameLabel = DetailLabel();
        technicalIdLabel = DetailLabel();
        statusLabel = DetailLabel();
        editor.Controls.Add(detectedNameLabel);
        editor.Controls.Add(technicalIdLabel);
        editor.Controls.Add(statusLabel);

        editor.Controls.Add(SectionLabel("Distribución"));
        layoutComboBox = new ComboBox
        {
            Dock = DockStyle.Top,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = nameof(RightKeyboard.Layout.Name),
            Margin = new Padding(0, 6, 0, 14)
        };
        layoutComboBox.Items.Add("Sin distribución");
        layoutComboBox.Items.AddRange(layouts.Cast<object>().ToArray());
        editor.Controls.Add(layoutComboBox);

        ignoredCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "Ignorar eventos de este dispositivo",
            Margin = new Padding(0, 0, 0, 16)
        };
        ignoredCheckBox.CheckedChanged += (_, _) => layoutComboBox.Enabled = !ignoredCheckBox.Checked;
        editor.Controls.Add(ignoredCheckBox);

        FlowLayoutPanel preferenceActions = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0)
        };
        saveButton = ActionButton("Guardar cambios");
        saveButton.Click += (_, _) => SaveSelectedPreference();
        forgetButton = ActionButton("Olvidar dispositivo");
        forgetButton.Click += (_, _) => ForgetSelectedDevice();
        preferenceActions.Controls.Add(saveButton);
        preferenceActions.Controls.Add(forgetButton);
        editor.Controls.Add(preferenceActions);

        Label pathLabel = DetailLabel();
        pathLabel.Text = $"Archivo: {Configuration.GetConfigFilePath()}";
        pathLabel.AutoEllipsis = true;
        pathLabel.Dock = DockStyle.Bottom;
        editor.Controls.Add(pathLabel);

        content.Controls.Add(leftPanel, 0, 0);
        content.Controls.Add(editor, 1, 0);

        FlowLayoutPanel footer = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 20, 0, 0)
        };
        Button exportButton = ActionButton("Exportar");
        exportButton.Click += (_, _) => ExportConfiguration();
        Button importButton = ActionButton("Importar");
        importButton.Click += (_, _) => ImportConfiguration();
        startupCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "Iniciar RightKeyboard con Windows",
            Checked = StartupManager.IsEnabled,
            Margin = new Padding(24, 8, 0, 0)
        };
        Button closeButton = ActionButton("Cerrar");
        closeButton.DialogResult = DialogResult.OK;
        closeButton.Margin = new Padding(24, 0, 0, 0);
        footer.Controls.Add(exportButton);
        footer.Controls.Add(importButton);
        footer.Controls.Add(startupCheckBox);
        footer.Controls.Add(closeButton);

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(content, 0, 1);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);
        AcceptButton = saveButton;
        CancelButton = closeButton;

        startupCheckBox.CheckedChanged += (_, _) => StartupManager.SetEnabled(startupCheckBox.Checked);
        RefreshDeviceList();
    }

    private void RefreshDeviceList(string? identityToSelect = null)
    {
        devices.Refresh();
        HashSet<string> connected = devices.Select(device => device.Identity).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (KeyboardDevice device in devices)
        {
            configuration.TouchDevice(device);
        }

        saveConfiguration();
        selectedIdentity = null;
        deviceList.Controls.Clear();

        foreach (DevicePreference preference in configuration.Devices.Values
                     .OrderBy(preference => preference.DisplayName, StringComparer.CurrentCultureIgnoreCase))
        {
            string state = connected.Contains(preference.Identity) ? "Conectado" : "Desconectado";
            string mode = configuration.IgnoredDevices.Contains(preference.Identity)
                ? "Ignorado"
                : configuration.LayoutMappings.TryGetValue(preference.Identity, out Layout? layout)
                    ? layout.Name
                    : "Sin distribución";
            RadioButton button = new()
            {
                Appearance = Appearance.Button,
                AutoSize = false,
                Height = 64,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 6, 12, 6),
                Text = $"{preference.DisplayName}\r\n{state} · {mode}",
                Tag = preference.Identity,
                Margin = new Padding(0, 0, 0, 8),
                AccessibleName = $"{preference.DisplayName}, {state}, {mode}"
            };
            button.FlatAppearance.BorderColor = SystemColors.ControlLight;
            button.CheckedChanged += (_, _) =>
            {
                if (button.Checked)
                {
                    SelectDevice((string)button.Tag);
                }
            };
            deviceList.Controls.Add(button);
        }

        if (deviceList.Controls.Count == 0)
        {
            deviceList.Controls.Add(emptyLabel);
            SetEditorEnabled(false);
        }
        else
        {
            ResizeDeviceButtons();
            RadioButton? selected = deviceList.Controls.OfType<RadioButton>()
                .FirstOrDefault(button => string.Equals(button.Tag as string, identityToSelect, StringComparison.OrdinalIgnoreCase))
                ?? deviceList.Controls.OfType<RadioButton>().First();
            selected.Checked = true;
        }
    }

    private void ResizeDeviceButtons()
    {
        int width = Math.Max(180, deviceList.ClientSize.Width - deviceList.Padding.Horizontal - 12);
        foreach (RadioButton button in deviceList.Controls.OfType<RadioButton>())
        {
            button.Width = width;
        }
    }

    private void SelectDevice(string identity)
    {
        selectedIdentity = identity;
        DevicePreference preference = configuration.Devices[identity];
        customNameTextBox.Text = preference.CustomName ?? preference.DisplayName;
        detectedNameLabel.Text = $"Detectado: {preference.DetectedName}";
        technicalIdLabel.Text = $"Identificador: {preference.TechnicalId}";
        bool connected = devices.Any(device => device.Identity == identity);
        statusLabel.Text = $"Estado: {(connected ? "Conectado" : "Desconectado")} · Última detección: {preference.LastSeenUtc.ToLocalTime():g}";
        ignoredCheckBox.Checked = configuration.IgnoredDevices.Contains(identity);
        layoutComboBox.SelectedItem = configuration.LayoutMappings.TryGetValue(identity, out Layout? layout)
            ? layouts.FirstOrDefault(candidate => candidate.Identifier == layout.Identifier) ?? layout
            : layoutComboBox.Items[0];
        layoutComboBox.Enabled = !ignoredCheckBox.Checked;
        SetEditorEnabled(true);
    }

    private void SetEditorEnabled(bool enabled)
    {
        customNameTextBox.Enabled = enabled;
        layoutComboBox.Enabled = enabled && !ignoredCheckBox.Checked;
        ignoredCheckBox.Enabled = enabled;
        saveButton.Enabled = enabled;
        forgetButton.Enabled = enabled;
    }

    private void SaveSelectedPreference()
    {
        if (selectedIdentity is null)
        {
            return;
        }

        Layout? selectedLayout = layoutComboBox.SelectedItem as Layout;
        configuration.UpdatePreference(
            selectedIdentity,
            customNameTextBox.Text,
            selectedLayout,
            ignoredCheckBox.Checked);
        saveConfiguration();
        RefreshDeviceList(selectedIdentity);
    }

    private void ForgetSelectedDevice()
    {
        if (selectedIdentity is null)
        {
            return;
        }

        DevicePreference preference = configuration.Devices[selectedIdentity];
        if (MessageBox.Show(
                $"Se olvidarán el nombre y la preferencia de \"{preference.DisplayName}\".",
                "Olvidar dispositivo",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.OK)
        {
            return;
        }

        configuration.Forget(selectedIdentity);
        saveConfiguration();
        RefreshDeviceList();
    }

    private void ExportConfiguration()
    {
        using SaveFileDialog dialog = new()
        {
            Title = "Exportar preferencias de RightKeyboard",
            Filter = "Configuración JSON (*.json)|*.json",
            FileName = $"RightKeyboard-preferencias-{DateTime.Now:yyyy-MM-dd}.json",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        configuration.Export(dialog.FileName);
    }

    private void ImportConfiguration()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Importar preferencias de RightKeyboard",
            Filter = "Configuración JSON (*.json)|*.json",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        Configuration imported;
        try
        {
            imported = Configuration.LoadConfiguration(devices, dialog.FileName, layouts);
        }
        catch (Exception error)
        {
            MessageBox.Show(this, $"El archivo no es válido.\n\n{error.Message}", "Importar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        DialogResult mode = MessageBox.Show(
            this,
            $"Se encontraron {imported.Devices.Count} dispositivos.\n\n" +
            "Sí: reemplazar la configuración actual.\nNo: combinar con la configuración actual.",
            "Importar preferencias",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (mode == DialogResult.Cancel)
        {
            return;
        }

        configuration.CreateBackup();
        configuration.MergeFrom(imported, replace: mode == DialogResult.Yes);
        saveConfiguration();
        RefreshDeviceList();
    }

    private static Label SectionLabel(string text) => new()
    {
        AutoSize = true,
        Text = text,
        Font = new Font(SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont, FontStyle.Bold),
        Margin = new Padding(0, 0, 0, 2)
    };

    private static Label DetailLabel() => new()
    {
        AutoSize = true,
        ForeColor = SystemColors.GrayText,
        Margin = new Padding(0, 0, 0, 6),
        MaximumSize = new Size(520, 0)
    };

    private static Button ActionButton(string text) => new()
    {
        AutoSize = true,
        Text = text,
        Padding = new Padding(12, 4, 12, 4),
        Margin = new Padding(0, 0, 8, 0)
    };
}
