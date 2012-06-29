using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Butterfly.Core;
using System.Net;
using System.Collections.Specialized;
using System.Net.Cache;
using Butterfly.MainServerConnectionHandeling;
using System.Diagnostics;

namespace Butterfly
{
    internal class Program
    {
        #region For Close Button disable
        [DllImport("user32.dll")]
        static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);
        [DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        static extern IntPtr RemoveMenu(IntPtr hMenu, uint nPosition, uint wFlags);
        internal const uint SC_CLOSE = 0xF060;
        internal const uint MF_GRAYED = 0x00000001;
        internal const uint MF_BYCOMMAND = 0x00000000;
        #endregion

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        public static MainServerConnectionHolder LicHandeler { get; private set; }
        private delegate bool EventHandler(CtrlType sig);

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        [STAThreadAttribute]
        internal static void Main()
        {
            #region For Close Button disable
            IntPtr hMenu = Process.GetCurrentProcess().MainWindowHandle;
            IntPtr hSystemMenu = GetSystemMenu(hMenu, false);
            EnableMenuItem(hSystemMenu, SC_CLOSE, MF_GRAYED);
            RemoveMenu(hSystemMenu, SC_CLOSE, MF_BYCOMMAND);
            #endregion

            Program.InitEnvironment();

            while (true)
            {
                Console.CursorVisible = true;
                if (Logging.DisabledState)
                    Console.Write("bfly> ");
                ConsoleCommandHandeling.InvokeCommand(Console.ReadLine());
            }
        }

        [MTAThread]
        internal static void InitEnvironment()
        {
            if (!ButterflyEnvironment.isLive)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.CursorVisible = false;
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);

                ButterflyEnvironment.Initialize();
            }
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Logging.DisablePrimaryWriting(true);
            Exception e = (Exception)args.ExceptionObject;
            Logging.LogCriticalException("SYSTEM CRITICAL EXCEPTION: " + e.ToString());
            ButterflyEnvironment.SendMassMessage("A fatal error crashed the server, server shutting down.");
            ButterflyEnvironment.PreformShutDown(true);
        }
    }
}
