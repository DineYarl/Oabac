using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace Oabac.Services
{
    public class TrayIconService : IDisposable
    {
        private const uint WM_USER = 0x0400;
        private const uint WM_TRAYICON = WM_USER + 1;
        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_DELETE = 0x00000002;
        private const uint NIF_MESSAGE = 0x00000001;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;
        private const uint WM_LBUTTONDBLCLK = 0x0203;
        private const uint WM_RBUTTONUP = 0x0205;
        private const uint WM_COMMAND = 0x0111;
        
        private const uint IDM_OPEN = 1001;
        private const uint IDM_SYNC = 1002;
        private const uint IDM_EXIT = 1003;

        [StructLayout(LayoutKind.Sequential)]
        public struct NOTIFYICONDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public uint dwState;
            public uint dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public uint uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public uint dwInfoFlags;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
        
        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, uint uIdSubclass, IntPtr dwRefData);

        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern bool RemoveWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, uint uIdSubclass);

        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();
        
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);
        
        [DllImport("user32.dll")]
        private static extern bool TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);
        
        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr hMenu);
        
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
        
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        private delegate IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, uint uIdSubclass, IntPtr dwRefData);

        private IntPtr _hwnd;
        private IntPtr _hIcon;
        private SubclassProc _subclassProc;
        private bool _isInitialized;

        public void Initialize()
        {
            if (_isInitialized) return;
            
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "sync.ico");
            if (File.Exists(iconPath))
            {
                _hIcon = LoadImage(IntPtr.Zero, iconPath, 1, 16, 16, 0x00000010);
            }
            
            // Fallback if icon load failed
            if (_hIcon == IntPtr.Zero)
            {
                // IDI_APPLICATION = 32512
                _hIcon = LoadIcon(IntPtr.Zero, (IntPtr)32512);
            }

            var nid = new NOTIFYICONDATA();
            nid.cbSize = (uint)Marshal.SizeOf(nid);
            nid.hWnd = _hwnd;
            nid.uID = 1;
            nid.uFlags = NIF_ICON | NIF_MESSAGE | NIF_TIP;
            nid.uCallbackMessage = WM_TRAYICON;
            nid.hIcon = _hIcon;
            nid.szTip = "Oabac";

            Shell_NotifyIcon(NIM_ADD, ref nid);

            _subclassProc = new SubclassProc(WndProc);
            SetWindowSubclass(_hwnd, _subclassProc, 1, IntPtr.Zero);
            
            _isInitialized = true;
        }

        private IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, uint uIdSubclass, IntPtr dwRefData)
        {
            if (uMsg == WM_TRAYICON)
            {
                if ((uint)lParam == WM_LBUTTONDBLCLK)
                {
                    ShowMainWindow();
                }
                else if ((uint)lParam == WM_RBUTTONUP)
                {
                    ShowContextMenu();
                }
                return IntPtr.Zero;
            }
            else if (uMsg == WM_COMMAND)
            {
                uint id = (uint)wParam & 0xFFFF;
                if (id == IDM_OPEN) ShowMainWindow();
                else if (id == IDM_SYNC) App.SyncService.SyncNow();
                else if (id == IDM_EXIT) ExitApp();
            }

            return DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        private void ShowMainWindow()
        {
            if (App.MainWindow is MainWindow mw)
            {
                mw.RestoreWindow();
            }
        }

        private void ShowContextMenu()
        {
            IntPtr hMenu = CreatePopupMenu();
            AppendMenu(hMenu, 0, IDM_OPEN, "Open");
            AppendMenu(hMenu, 0, IDM_SYNC, "Sync Now");
            AppendMenu(hMenu, 0, IDM_EXIT, "Exit");

            GetCursorPos(out POINT pt);
            SetForegroundWindow(_hwnd);
            TrackPopupMenu(hMenu, 0, pt.X, pt.Y, 0, _hwnd, IntPtr.Zero);
            DestroyMenu(hMenu);
        }

        private void ExitApp()
        {
            Dispose();
            Application.Current.Exit();
        }

        public void Dispose()
        {
            if (_isInitialized)
            {
                var nid = new NOTIFYICONDATA();
                nid.cbSize = (uint)Marshal.SizeOf(nid);
                nid.hWnd = _hwnd;
                nid.uID = 1;
                Shell_NotifyIcon(NIM_DELETE, ref nid);

                RemoveWindowSubclass(_hwnd, _subclassProc, 1);
                if (_hIcon != IntPtr.Zero) DestroyIcon(_hIcon);
                
                _isInitialized = false;
            }
        }
    }
}
