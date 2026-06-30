namespace RightKeyboard;

internal sealed class PreferenceResetService
{
    internal const string ConfirmationText =
        "Se eliminarán todos los alias, distribuciones y dispositivos ignorados.\n\nEsta acción no se puede deshacer.";
    internal const string ConfirmationTitle = "Limpiar preferencias";

    private readonly Action clearPreferences;
    private readonly Func<IWin32Window?, DialogResult> confirm;
    private readonly Action<IWin32Window?, Exception> showError;

    public PreferenceResetService(Configuration configuration)
        : this(
            () => configuration.Clear(),
            owner => MessageBox.Show(
                owner,
                ConfirmationText,
                ConfirmationTitle,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2),
            (owner, error) => MessageBox.Show(
                owner,
                $"No se pudieron limpiar las preferencias.\n\n{error.Message}",
                "RightKeyboard",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning))
    {
    }

    internal PreferenceResetService(
        Action clearPreferences,
        Func<IWin32Window?, DialogResult> confirm,
        Action<IWin32Window?, Exception> showError)
    {
        this.clearPreferences = clearPreferences;
        this.confirm = confirm;
        this.showError = showError;
    }

    public bool TryClear(IWin32Window? owner = null)
    {
        if (confirm(owner) != DialogResult.OK)
        {
            return false;
        }

        try
        {
            clearPreferences();
            return true;
        }
        catch (Exception error)
        {
            showError(owner, error);
            return false;
        }
    }
}
