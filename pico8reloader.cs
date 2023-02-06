using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;

[assembly : AssemblyTitle("pico8reloader")]
[assembly : AssemblyConfiguration("")]
[assembly : AssemblyCompany("rostok - https://github.com/rostok/")]
[assembly : AssemblyTrademark("")]
[assembly : AssemblyCulture("")]
[assembly : AssemblyVersion("1.0.4.0")]
[assembly : AssemblyFileVersion("1.0.4.0")]

public class Pico8Reloader {
    // https://www.codeproject.com/Questions/1228092/Simulate-this-keys-to-inactive-application-with-Cs
    [DllImport("user32.dll")] public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    const uint WM_KEYDOWN = 0x0100;
    const uint WM_KEYUP = 0x0101;

    public struct Rect {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
    }

    [DllImport("User32.dll")] static extern int SetForegroundWindow(IntPtr point);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    // http://csharphelper.com/blog/2016/12/set-another-applications-size-and-position-in-c/
    [DllImport("user32.dll")] [return :MarshalAs(UnmanagedType.Bool)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

    [Flags()]
    private enum SetWindowPosFlags : uint {
        SynchronousWindowPosition = 0x4000,
        DeferErase = 0x2000,
        DrawFrame = 0x0020,
        FrameChanged = 0x0020,
        HideWindow = 0x0080,
        DoNotActivate = 0x0010,
        DoNotCopyBits = 0x0100,
        IgnoreMove = 0x0002,
        DoNotChangeOwnerZOrder = 0x0200,
        DoNotRedraw = 0x0008,
        DoNotReposition = 0x0200,
        DoNotSendChangingEvent = 0x0400,
        IgnoreResize = 0x0001,
        IgnoreZOrder = 0x0004,
        ShowWindow = 0x0040,
    }

    public static Rect windowRect = new Rect();
    public static bool windowRectSet = false;
    public static bool focusSet = false;
    public static bool checkSet = false;
    public static int delay = 0;

    // https://stackoverflow.com/questions/2633628/can-i-get-command-line-arguments-of-other-processes-from-net-c/2633674#2633674
    private static string GetCommandLine(Process process) {
        using(ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
        using(ManagementObjectCollection objects = searcher.Get()) {
            return objects.Cast<ManagementBaseObject>().SingleOrDefault() ? ["CommandLine"]?.ToString();
        }
    }

    private static void Action(string FullPath) {
        IntPtr focusedWindow = GetForegroundWindow();
        Process p = Process.GetProcessesByName("pico8").Where(q=>Path.GetFileName(q.MainModule.FileName).ToLower()=="pico8.exe").FirstOrDefault();

        if (p != null) {
            string args = GetCommandLine(p)+"";
            string fn = Path.GetFileName(FullPath);

            if (!fn.Contains(".p8")) {
                Console.WriteLine("    not a p8 file: " + fn);
                return;
            }

            if (args.ToLower().Contains(fn.ToLower())) {
                // just reload
                Console.WriteLine("    sending Ctrl+R");
                SetForegroundWindow(p.MainWindowHandle);
                p.WaitForInputIdle();
                //SendKeys.SendWait("^(r)"); // doesn't work
                //p.WaitForInputIdle();

                // https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-keydown
                // https://www.win.tue.nl/~aeb/linux/kbd/scancodes-1.html
                PostMessage(p.MainWindowHandle, WM_KEYDOWN, (IntPtr) (Keys.LControlKey), (IntPtr) (1 | 0x1D << 16));
                PostMessage(p.MainWindowHandle, WM_KEYDOWN, (IntPtr) (Keys.R), (IntPtr) (1 | 0x13 << 16));
                Thread.Sleep(50);
                PostMessage(p.MainWindowHandle, WM_KEYUP, (IntPtr) (Keys.R), (IntPtr) (1 | 0x13 << 16));
                PostMessage(p.MainWindowHandle, WM_KEYUP, (IntPtr) (Keys.LControlKey), (IntPtr) (1 | 0x1D << 16));
                Thread.Sleep(50);

                GetWindowRect(focusSet ? p.MainWindowHandle : focusedWindow, ref windowRect);
                // https://stackoverflow.com/a/27449582/2451546
                ShowWindow(focusSet ? p.MainWindowHandle : focusedWindow, 9); // SW_RESTORE = 9
                // https://stackoverflow.com/a/30572826/2451546
                keybd_event(0, 0, 0, 0);
                SetForegroundWindow(focusSet ? p.MainWindowHandle : focusedWindow);
                SetWindowPos(focusSet ? p.MainWindowHandle : focusedWindow, IntPtr.Zero, windowRect.Left, windowRect.Top, windowRect.Right - windowRect.Left, windowRect.Bottom - windowRect.Top, 0);
                return;
            } else {
                GetWindowRect(p.MainWindowHandle, ref windowRect);
                windowRectSet = true;

                Console.WriteLine("    killing");
                p.Kill();
            }
        }

        string fnonly = Path.GetFileName(FullPath);
        if (delay>0) {
            Console.WriteLine($"    waiting {delay} ms");
            System.Threading.Thread.Sleep(delay);
        }
        if (checkSet) 
        	for (int i=0; i<10; i++) {
          	    Console.WriteLine($"{FullPath} exists {File.Exists(fnonly)}");
                try
                {
                    using (FileStream fs = File.Open(fnonly, FileMode.Open, FileAccess.Read))
                    {
                        Console.WriteLine("The file is now readable. File length: " + fs.Length + " bytes");
                        fs.Close();
                        break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{fnonly} is not yet readable: {e}. Check in 250 ms");
                    Thread.Sleep(250);
                }
        	}

        Console.WriteLine("    running: pico8 -run " + FullPath);
        p = Process.Start("pico8", " -run " + FullPath);
        if (p != null) {
            Thread.Sleep(1000);
            SetForegroundWindow(p.MainWindowHandle);

            if (windowRectSet) SetWindowPos(p.MainWindowHandle, IntPtr.Zero, windowRect.Left, windowRect.Top, windowRect.Right - windowRect.Left, windowRect.Bottom - windowRect.Top, 0);

            GetWindowRect(focusSet ? p.MainWindowHandle : focusedWindow, ref windowRect);
            ShowWindow(focusSet ? p.MainWindowHandle : focusedWindow, 9); // SW_RESTORE
            keybd_event(0, 0, 0, 0);
            SetForegroundWindow(focusSet ? p.MainWindowHandle : focusedWindow);
            SetWindowPos(focusSet ? p.MainWindowHandle : focusedWindow, IntPtr.Zero, windowRect.Left, windowRect.Top, windowRect.Right - windowRect.Left, windowRect.Bottom - windowRect.Top, 0);
        }
    }

    private static void OnChanged(object source, FileSystemEventArgs e) {
        Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
        Action(e.FullPath);
    }

    private static void OnRenamed(object source, RenamedEventArgs e) {
        Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
        Action(e.FullPath);
    }

    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    public static void Run() {
        string[] args = System.Environment.GetCommandLineArgs();

        if (args.Contains("-h") || args.Contains("/?") || args.Contains("--help")) {
            Console.WriteLine("pico8reloader will watch folder for any p8 file changes.");
            Console.WriteLine("in case of file write or file rename it will:");
            Console.WriteLine("1) run pico8 with latest p8 file if it is not running (after delay or check)");
            Console.WriteLine("2) restart pico8 if lastest p8 file is not in command line");
            Console.WriteLine("3) sent Ctrl+R (reload) keystroke to pico8 process");
            Console.WriteLine("4) on --focus keep focus on pico8 window, or get to previous one");
            Console.WriteLine("");
            Console.WriteLine("syntax: pico8reloader [path] [--winpos=x,y[,w,h]] [--focus] [--delay=milisecs] [--check]");
            Console.WriteLine("default path is .");
            Console.WriteLine("pico8 should be accessible via PATH variable (.bat or a shim)");
            Console.WriteLine("");
            Console.WriteLine("this comes with MIT license from rostok - https://github.com/rostok/");
            return;
        }

        string dir = ".";
        if (args.Length >= 2 && Directory.Exists(args[1])) dir = args[1];

        args.ToList().Where(a=>a.StartsWith("--delay=")).ToList().ForEach(a=>Int32.TryParse(a.Replace("--delay=",""), out delay));

        args.ToList().Where(a=>a.StartsWith("--winpos=")).ToList().ForEach(a=>{
            string s = a.Replace("--winpos=","");
            string[] sa = s.Split(',');
            int x = 0;
            int y = 0;
            int w = 256;
            int h = 256;
            if (sa.Length>0) x = Int16.Parse(sa[0]);
            if (sa.Length>1) y = Int16.Parse(sa[1]);
            if (sa.Length>2) w = Int16.Parse(sa[2]);
            if (sa.Length>3) h = Int16.Parse(sa[3]);
            windowRect.Left = x;
            windowRect.Top = y;
            windowRect.Right = x+w;
            windowRect.Bottom = y+h;
            windowRectSet = true;
        });
        
        focusSet = args.Contains("--focus");
        checkSet = args.Contains("--check");

        // Create a new FileSystemWatcher and set its properties.
        FileSystemWatcher watcher = new FileSystemWatcher();
        watcher.Path = dir;
        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName; // NotifyFilters.LastAccess
        watcher.Filter = "*.p8";

        watcher.Changed += new FileSystemEventHandler(OnChanged);
        watcher.Created += new FileSystemEventHandler(OnChanged);
        //watcher.Deleted += new FileSystemEventHandler(OnChanged);
        watcher.Renamed += new RenamedEventHandler(OnRenamed);

        watcher.EnableRaisingEvents = true;

        Console.WriteLine("Starting to monitor " + dir + " folder.");
        Console.WriteLine("Press Q or Ctrl+C to quit, F to switch focus mode.");
        while (true) {
            char c = Char.ToLower(Convert.ToChar(Console.Read()));
            if (c=='q') break;
            if (c=='f') { focusSet = !focusSet; Console.WriteLine("--focus is "+(focusSet?"set":"not set")); };
            if (c=='s') {
                Process p = Process.GetProcessesByName("pico8").FirstOrDefault();
                Console.WriteLine(p);
                if (p != null) SetForegroundWindow(p.MainWindowHandle);
            }
        }
    }

    public static void Main() {
        Run();
    }

}