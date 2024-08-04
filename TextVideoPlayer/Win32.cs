using System.Runtime.InteropServices;

namespace TextVideoPlayer;

static class Win32
{
    public const int WM_SETTEXT = 0x000c;

    [DllImport( "kernel32.dll", SetLastError = true )]
    public static extern bool SetConsoleMode( IntPtr hConsoleHandle, int mode );

    [DllImport( "kernel32.dll", SetLastError = true )]
    public static extern bool GetConsoleMode( IntPtr handle, out int mode );

    [DllImport( "kernel32.dll", SetLastError = true )]
    public static extern IntPtr GetStdHandle( int handle );

    [DllImport("User32.dll", CharSet = CharSet.Auto)]
    public extern static IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, [In] string lpClassName, [In] string lpWindowName);

    [DllImport("User32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, string lParam);
}