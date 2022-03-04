using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace JouhouVPNTool
{
	public static class TempFile
	{
		private static Random _random = new Random();

		private static HashSet<string> _tempDirectories = new HashSet<string>();
		public static string GetTempDirectory()
		{
			var path = Path.Combine(Path.GetTempPath(), "jvat" + _random.Next(1000000));
			if (_tempDirectories.Contains(path))
				return GetTempDirectory();
			_tempDirectories.Add(path);

			if(!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			return path;
		}

		public static string GetTempFile(string str)
		{
			if (str.StartsWith("/"))
				str = str.Substring(1);

			var dir = Path.Combine(GetTempDirectory() , str);
			return dir;
		}

		public static void DeleteAll()
		{
			try
			{
				foreach (var k in _tempDirectories)
				{
					Directory.Delete(k, true);
				}
			}
			catch (Exception) { }
		}

	}
}
