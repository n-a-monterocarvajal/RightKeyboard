namespace RightKeyboard;

internal sealed class SettingsDialog : FluentForm
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
    private readonly PreferenceResetService preferenceReset;
    private string? selectedIdentity;
    private string? lastTriggeredIdentity;

    public SettingsDialog(
        Configuration configuration,
        KeyboardDevicesCollection devices,
        Action saveConfiguration)
        : base(FluentBackdropKind.Main)
    {
        this.configuration = configuration;
        this.devices = devices;
        this.saveConfiguration = saveConfiguration;
        preferenceReset = new PreferenceResetService(configuration);
        layouts = RightKeyboard.Layout.EnumerateLayouts()
            .OrderBy(layout => layout.LanguageName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(layout => layout.LayoutName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        Text = "Configuración de RightKeyboard";
        AccessibleDescription = "Edita alias, distribuciones y dispositivos ignorados, incluso si están desconectados.";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(800, 540);
        ClientSize = new Size(920, 620);
        AutoScaleMode = AutoScaleMode.Dpi;
        KeyPreview = true;
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
            Font = FluentTypography.CreateTitleFont(16f),
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

        TableLayoutPanel leftPanel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(0, 0, 18, 0),
            Margin = new Padding(0)
        };
        leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        leftPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Dispositivos conocidos",
            Font = FluentTypography.CreateSemiboldFont(Font.Size),
            Margin = new Padding(0, 0, 0, 10)
        }, 0, 0);
        deviceList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 0, 8, 0),
            AccessibleName = "Dispositivos conocidos",
            AccessibleDescription = "Incluye dispositivos conectados, desconectados e ignorados."
        };
        deviceList.SizeChanged += (_, _) => ResizeDeviceButtons();
        deviceList.FontChanged += (_, _) => ResizeDeviceButtons();
        emptyLabel = new Label
        {
            AutoSize = true,
            Text = "Aún no hay dispositivos recordados.",
            ForeColor = SystemColors.GrayText,
            Padding = new Padding(8)
        };
        FluentTheme.Mark(emptyLabel, FluentThemeRole.SecondaryText);
        leftPanel.Controls.Add(deviceList, 0, 1);

        FluentTableLayoutPanel editor = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 11,
            Padding = new Padding(20),
            BackColor = SystemColors.Window,
            ForeColor = SystemColors.WindowText,
            AccessibleName = "Editor del dispositivo seleccionado"
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
            Margin = new Padding(0, 6, 0, 16),
            AccessibleName = "Nombre para este teclado",
            AccessibleDescription = "Alias opcional que se muestra en lugar del nombre detectado."
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
            Margin = new Padding(0, 6, 0, 14),
            MaxDropDownItems = 12,
            AccessibleName = "Distribución del teclado",
            AccessibleDescription = "Idioma y distribución que se aplicarán al usar este dispositivo."
        };
        layoutComboBox.Items.Add("Sin distribución");
        layoutComboBox.Items.AddRange(layouts.Cast<object>().ToArray());
        editor.Controls.Add(layoutComboBox);

        ignoredCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "Ignorar eventos de este dispositivo",
            Margin = new Padding(0, 0, 0, 16),
            AccessibleDescription = "Impide que RightKeyboard cambie la distribución al recibir eventos del dispositivo."
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
        saveButton = ActionButton("&Guardar cambios", primary: true);
        saveButton.AccessibleDescription = "Guarda el alias, la distribución y el estado ignorado. Atajo: Control más S.";
        saveButton.Click += (_, _) => SaveSelectedPreference();
        forgetButton = ActionButton("&Olvidar dispositivo");
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

        TableLayoutPanel footer = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 20, 0, 0)
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        FlowLayoutPanel secondaryActions = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0)
        };
        Button exportButton = ActionButton("&Exportar");
        exportButton.Click += (_, _) => ExportConfiguration();
        Button importButton = ActionButton("&Importar");
        importButton.Click += (_, _) => ImportConfiguration();
        Button clearButton = ActionButton("&Limpiar preferencias");
        clearButton.AccessibleDescription = "Elimina todos los alias, distribuciones y dispositivos ignorados después de pedir confirmación.";
        clearButton.Click += (_, _) => ClearPreferences();
        startupCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "Iniciar RightKeyboard con Windows",
            Checked = StartupManager.IsEnabled,
            Margin = new Padding(24, 8, 0, 0)
        };
        Button closeButton = ActionButton("&Cerrar");
        closeButton.DialogResult = DialogResult.OK;
        closeButton.Margin = new Padding(16, 0, 0, 0);
        secondaryActions.Controls.Add(exportButton);
        secondaryActions.Controls.Add(importButton);
        secondaryActions.Controls.Add(clearButton);
        secondaryActions.Controls.Add(startupCheckBox);
        footer.Controls.Add(secondaryActions, 0, 0);
        footer.Controls.Add(closeButton, 1, 0);

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(content, 0, 1);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);
        AcceptButton = saveButton;
        CancelButton = closeButton;

        startupCheckBox.CheckedChanged += (_, _) => StartupManager.SetEnabled(startupCheckBox.Checked);
        KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.Control && eventArgs.KeyCode == Keys.S && saveButton.Enabled)
            {
                SaveSelectedPreference();
                eventArgs.SuppressKeyPress = true;
            }
        };
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
            bool isConnected = connected.Contains(preference.Identity);
            bool isIgnored = configuration.IgnoredDevices.Contains(preference.Identity);
            configuration.LayoutMappings.TryGetValue(preference.Identity, out Layout? layout);
            DevicePresentation presentation = DevicePresentation.Create(isConnected, isIgnored, layout);
            RadioButton button = new()
            {
                Appearance = Appearance.Button,
                AutoSize = false,
                Height = 64,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 6, 12, 6),
                Text = presentation.GetListText(preference.DisplayName),
                Tag = preference.Identity,
                Margin = new Padding(0, 0, 0, 8),
                AutoEllipsis = true,
                AccessibleName = presentation.GetAccessibleName(preference.DisplayName),
                AccessibleDescription = "Selecciona el dispositivo para editar sus preferencias."
            };
            button.FlatAppearance.BorderColor = SystemColors.ControlLight;
            button.FlatAppearance.CheckedBackColor = SystemColors.GradientActiveCaption;
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

    internal void HighlightDevice(string identity)
    {
        lastTriggeredIdentity = identity;
        RadioButton? selected = deviceList.Controls
            .OfType<RadioButton>()
            .FirstOrDefault(button =>
                string.Equals(button.Tag as string, identity, StringComparison.OrdinalIgnoreCase));
        if (selected is null)
        {
            RefreshDeviceList(identity);
            selected = deviceList.Controls
                .OfType<RadioButton>()
                .FirstOrDefault(button =>
                    string.Equals(button.Tag as string, identity, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            selected.Checked = true;
            SelectDevice(identity);
        }

        selected?.Focus();
    }

    private void ResizeDeviceButtons()
    {
        int width = Math.Max(180, deviceList.ClientSize.Width - deviceList.Padding.Horizontal - 12);
        foreach (RadioButton button in deviceList.Controls.OfType<RadioButton>())
        {
            button.Width = width;
            button.Height = Math.Max(76, (Font.Height * 3) + 16);
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
        string triggerState = string.Equals(identity, lastTriggeredIdentity, StringComparison.OrdinalIgnoreCase)
            ? " · Pulsado ahora"
            : string.Empty;
        statusLabel.Text = $"Estado: {(connected ? "Conectado" : "Desconectado")} · Última detección: {preference.LastSeenUtc.ToLocalTime():g}{triggerState}";
        statusLabel.AccessibleDescription = connected
            ? "El dispositivo está conectado actualmente."
            : "El dispositivo no está conectado, pero sus preferencias siguen siendo editables.";
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

    private void ClearPreferences()
    {
        if (!preferenceReset.TryClear(this))
        {
            return;
        }

        selectedIdentity = null;
        deviceList.Controls.Clear();
        deviceList.Controls.Add(emptyLabel);
        customNameTextBox.Clear();
        detectedNameLabel.Text = string.Empty;
        technicalIdLabel.Text = string.Empty;
        statusLabel.Text = "Las preferencias se limpiaron correctamente.";
        ignoredCheckBox.Checked = false;
        layoutComboBox.SelectedIndex = 0;
        SetEditorEnabled(false);
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

        ConfigurationImportResult import;
        try
        {
            import = Configuration.LoadImport(dialog.FileName, layouts);
        }
        catch (Exception error)
        {
            MessageBox.Show(this, $"El archivo no es válido.\n\n{error.Message}", "Importar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        DialogResult mode = MessageBox.Show(
            this,
            $"Se encontraron {import.Configuration.Devices.Count} dispositivos.\n" +
            (import.Warnings.Count == 0
                ? "\n"
                : $"\nAdvertencias:\n- {string.Join("\n- ", import.Warnings)}\n\n") +
            "Sí: reemplazar la configuración actual.\nNo: combinar con la configuración actual.",
            "Importar preferencias",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (mode == DialogResult.Cancel)
        {
            return;
        }

        try
        {
            configuration.ApplyImport(import.Configuration, replace: mode == DialogResult.Yes);
        }
        catch (Exception error)
        {
            MessageBox.Show(
                this,
                $"No se pudo aplicar la importación. Las preferencias actuales no se modificaron.\n\n{error.Message}",
                "Importar",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        RefreshDeviceList();
    }

    private static Label SectionLabel(string text) => new()
    {
        AutoSize = true,
        Text = text,
        Font = FluentTypography.CreateSemiboldFont(SystemFonts.MessageBoxFont?.Size ?? SystemFonts.DefaultFont.Size),
        Margin = new Padding(0, 0, 0, 2)
    };

    private static Label DetailLabel()
    {
        Label label = new()
        {
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6),
            MaximumSize = new Size(520, 0)
        };
        FluentTheme.Mark(label, FluentThemeRole.SecondaryText);
        return label;
    }

    private static Button ActionButton(string text, bool primary = false)
    {
        Button button = new()
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(88, 36),
            Text = text,
            Padding = new Padding(12, 4, 12, 4),
            Margin = new Padding(0, 0, 8, 0),
            FlatStyle = FlatStyle.Flat
        };
        if (primary)
        {
            FluentTheme.Mark(button, FluentThemeRole.PrimaryButton);
        }

        return button;
    }
}
