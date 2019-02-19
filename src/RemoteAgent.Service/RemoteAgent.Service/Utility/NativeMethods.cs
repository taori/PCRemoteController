using System;
using System.Runtime.InteropServices;

namespace RemoteAgent.Service.Utility
{
	public class NativeMethods
	{
		[DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
		private static extern IntPtr GetDesktopWindow();

		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
		
		private const int SC_SCREENSAVE = 0xF140;
		private const int WM_SYSCOMMAND = 0x0112;
		
		public static void SetScreenSaverRunning()
		{
			SendMessage(GetDesktopWindow(), WM_SYSCOMMAND, SC_SCREENSAVE, 1);
		}
	}
}