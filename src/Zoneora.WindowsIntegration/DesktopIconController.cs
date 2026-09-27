using System.Runtime.InteropServices;

namespace Zoneora.WindowsIntegration;

public sealed class DesktopIconController
{
    public void SetVisible(bool visible)
    {
        nint desktop = FindWindowEx(FindWindow("Progman", "Program Manager"), 0, "SHELLDLL_DefView", null);
        if (desktop == 0)
        {
            desktop = FindDesktopView();
        }

        if (desktop != 0)
        {
            ShowWindow(FindWindowEx(desktop, 0, "SysListView32", null), visible ? 5 : 0);
        }
    }

    private static nint FindDesktopView()
    {
        nint worker = 0;
        do
        {
            worker = FindWindowEx(0, worker, "WorkerW", null);
            nint view = FindWindowEx(worker, 0, "SHELLDLL_DefView", null);
            if (view != 0)
            {
                return view;
            }
        } while (worker != 0);

        return 0;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(string? className, string? windowName);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindowEx(nint parent, nint childAfter, string? className, string? windowName);
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint window, int command);
}
