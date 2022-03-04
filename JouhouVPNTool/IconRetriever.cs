using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Media.Imaging;

namespace JouhouVPNTool
{
	public static class IconRetriever
	{
		[DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
		private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr phiconLarge, IntPtr phiconSmall, uint nIcons);
		
		[DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
		private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr phiconLarge, IntPtr[] phiconSmall, uint nIcons);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		extern static bool DestroyIcon(IntPtr handle);

		private static Dictionary<string, ImageSource> _cache;
		private static bool canUseIcon = true;
		public static void Init()
		{
			_cache = new Dictionary<string, ImageSource>();

			try
			{
				GetIcon("Unknown");
				canUseIcon = true;
				GetIcon("Folder");
			}
			catch (Exception e)
			{
				canUseIcon = false;
				if (_cache.ContainsKey("Unknown"))
					_cache["Unknown"] = null;
				else
					_cache.Add("Unknown", null);
				throw e;
			}
		}

		public static void Dispose()
		{
			_cache.Clear();
		}

		private static ImageSource ToImageSource(Icon icon)
		{
			ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
				icon.Handle,
				Int32Rect.Empty,
				BitmapSizeOptions.FromEmptyOptions());

			return imageSource;
		}

		public static ImageSource GetIcon(string extension)
		{
			if(!canUseIcon)
				return null;

			if (_cache.ContainsKey(extension))
				return _cache[extension];

			var path = GetFromExtensionPath(extension);
			if (string.IsNullOrEmpty(path))
			{
				return _cache["Unknown"];
			}
			var s = path.Split(',');
			if (s.Length > 2)
			{
				return _cache["Unknown"];
			}


			var iconPath = s[0];


			var iconIndex = 0;
			if (s.Length >= 2)
			{
				iconIndex = int.Parse(s[1]);
			}

			var ptrs = new IntPtr[1];
			var extractedIcon = ExtractIconEx(iconPath, iconIndex, IntPtr.Zero, ptrs, 1);

			var icon = Icon.FromHandle(ptrs[0]);
			var result = ToImageSource(icon);

			icon.Dispose();

			_cache.Add(extension, result);
			return result;

		}

		public static string GetFromExtensionPath(string extension)
		{
			var root = Registry.ClassesRoot;
			var extKey = root.OpenSubKey(extension);
			if (extKey == null)
			{
				root.Close();
				return null;
			}
			var defIcon = extKey.OpenSubKey("DefaultIcon");
			if (defIcon == null)
			{
				var kitei = extKey.GetValue(string.Empty);
				extKey.Close();
				root.Close();

				if (kitei != null)
				{
					return GetFromExtensionPath((string)kitei);
				}


				return null;
			}

			var result= (string)defIcon.GetValue(string.Empty);
			defIcon.Close();
			extKey.Close();
			root.Close();
			return result;
		}
	}
}
