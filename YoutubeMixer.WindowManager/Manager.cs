using System.Runtime.InteropServices;

namespace YoutubeMixer.WindowManager
{
    public class Manager
    {
        // Importeren van User32 functies
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static bool SetWindowPosition(string title, int newX, int newY, int newWidth, int newHeight)
        {
            IntPtr hWnd = FindWindow(null, $"{title} - Google Chrome");

            if (hWnd == IntPtr.Zero)
                return false;

            bool success = MoveWindow(hWnd, newX, newY, newWidth, newHeight, true);
            return success;
        }
    }
}
