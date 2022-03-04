using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;

namespace JouhouVPNTool.Connections
{
	public class VPNFile
	{
		public string Name { get; set; }
		public bool IsDirectory { get; set; }

		public string Hyperlink { get; set; }

		public DateTime TimeStamp { get; set; }

		public string Size { get; set; } = string.Empty;

		public ImageSource Icon { get; set; }

		public string BaseUrl
		{
			get
			{
				(var url, _) = SplitImpl(Hyperlink);
				return url;
			}
		}

		public string ResourceIndex
		{
			get
			{
				(_, var t) = SplitImpl(Hyperlink);
				return t.ContainsKey("v") ? t["v"] : string.Empty;
			}
		}

		public string FileType
		{
			get { return IsDirectory ? "フォルダ" : "ファイル"; }
		}

		public string ParentDirectoryPath
		{
			get
			{
				(_, var t) = SplitImpl(Hyperlink);
				if(t.ContainsKey("dir"))
				{
					return !IsDirectory ? t["dir"] : System.IO.Path.GetDirectoryName(t["dir"]);
				}
				return string.Empty;
			}
		}

		public string DirectoryPath
		{
			get
			{
				(_, var t) = SplitImpl(Hyperlink);
				if (t.ContainsKey("dir"))
				{
					return t["dir"];
				}
				return string.Empty;
			}
		}


		public string TimeStampString
		{
			get
			{
				if (TimeStamp == DateTime.MinValue) return string.Empty;
				return TimeStamp.ToString("g");
			}
		}

		private (string url, Dictionary<string,string> query) SplitImpl(string url)
		{
			if (string.IsNullOrWhiteSpace(url))
				return (string.Empty, new Dictionary<string, string>());

			var urls = url.Split('?');

			var dic = new Dictionary<string, string>();
			if (urls.Length > 1)
			{
				if(urls[1].Contains("url") && url.Length > 2)
				{
					urls[1] = urls[2];
				}
				foreach(var pair in urls[1].Replace("&amp;", "&").Split('&'))
				{
					var pairstr = pair.Split('=');
					dic.Add(pairstr[0], pairstr[1]);
				}
			}
			return (urls[0], dic);
		}

	}
}
