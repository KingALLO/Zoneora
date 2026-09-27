using System.Runtime.InteropServices;

namespace Zoneora.WindowsIntegration;

public sealed class TrayIconController : IDisposable
{
    private const int IdiApplication = 32512;
    private System.Windows.Forms.NotifyIcon? notifyIcon;
    private bool disposed;

    public event EventHandler? RestoreRequested;
    public event EventHandler? ExitRequested;

    public void Show(string tooltip)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (notifyIcon is not null)
        {
            notifyIcon.Visible = true;
            return;
        }

        System.Drawing.Icon icon = GetApplicationIcon();

        System.Windows.Forms.ContextMenuStrip menu = new();
        menu.Items.Add("Open Zoneora", null, (_, _) => RestoreRequested?.Invoke(this, EventArgs.Empty));
        menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = icon,
            Visible = true,
            Text = tooltip,
            ContextMenuStrip = menu
        };
        notifyIcon.DoubleClick += (_, _) => RestoreRequested?.Invoke(this, EventArgs.Empty);
    }

    private static System.Drawing.Icon GetApplicationIcon()
    {
        string? exePath = Environment.ProcessPath;
        if (exePath is not null)
        {
            System.Drawing.Icon? extracted = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
            if (extracted is not null)
            {
                return extracted;
            }
        }

        nint iconHandle = LoadIcon(0, IdiApplication);
        return iconHandle != 0
            ? System.Drawing.Icon.FromHandle(iconHandle)
            : System.Drawing.SystemIcons.Application;
    }

    public void Hide()
    {
        if (notifyIcon is not null)
        {
            notifyIcon.Visible = false;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        notifyIcon?.Dispose();
    }

    [DllImport("user32.dll")]
    private static extern nint LoadIcon(nint instance, nint iconName);
}
