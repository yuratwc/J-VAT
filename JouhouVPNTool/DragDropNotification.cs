using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using System.Windows;

namespace JouhouVPNTool
{
	public class DragDropNotification
	{
		#region Win32API
		[DllImport("User32.dll")]
		static extern bool GetCursorPos(out POINT lppoint);
		[StructLayout(LayoutKind.Sequential)]
		struct POINT
		{
			public int X { get; set; }
			public int Y { get; set; }
			public static implicit operator Point(POINT point)
			{
				return new Point(point.X, point.Y);
			}
		}
		#endregion

		public static Point GetCursorPos()
		{
			var pt = new POINT();
			GetCursorPos(out pt);
			return pt;
		}
	}
}
