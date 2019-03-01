using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace RemoteAgent.Service.Utility
{
	public class NativeMethods
	{
		[DllImport("user32.dll")]
		public static extern void LockWorkStation();

		[DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
		private static extern IntPtr GetDesktopWindow();

		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
		
		private const int SC_SCREENSAVE = 0xF140;
		private const int WM_SYSCOMMAND = 0x0112;
		
		public static void SetScreenSaverRunning()
		{
			SendMessage(GetProcessPointer(), WM_SYSCOMMAND, SC_SCREENSAVE, 1);
		}

		private const int APPCOMMAND_VOLUME_MUTE = 0x80000;
		private const int WM_APPCOMMAND = 0x319;

		[DllImport("user32.dll")]
		public static extern IntPtr SendMessagePointer(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

		public static void ToggleMute()
		{
			var intPtr = GetProcessPointer();
			SendMessagePointer(intPtr, WM_APPCOMMAND, intPtr, (IntPtr)APPCOMMAND_VOLUME_MUTE);
			//			SendMessagePointer()
		}

		private static unsafe IntPtr GetProcessPointer()
		{
			var p = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
			var intPtr = new IntPtr(p.ToPointer());
			return intPtr;
		}

		public static class Monitor
		{
			[DllImport("user32.dll")]
			static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
			[DllImport("user32.dll")]
			static extern void mouse_event(Int32 dwFlags, Int32 dx, Int32 dy, Int32 dwData, UIntPtr dwExtraInfo);

			private const int WmSyscommand = 0x0112;
			private const int ScMonitorpower = 0xF170;
			private const int MonitorShutoff = 2;
			private const int MouseeventfMove = 0x0001;

			public static void Off()
			{
				SendMessage(GetProcessPointer(), WmSyscommand, (IntPtr)ScMonitorpower, (IntPtr)MonitorShutoff);
			}

			public static void On()
			{
				mouse_event(MouseeventfMove, 0, 1, 0, UIntPtr.Zero);
				Thread.Sleep(40);
				mouse_event(MouseeventfMove, 0, -1, 0, UIntPtr.Zero);
			}
		}
	}
}