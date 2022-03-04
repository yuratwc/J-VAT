using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JouhouVPNTool.Connections
{
	public class VPNUploadStatus
	{
		public const string VpnUploadStatusUrl = @"https://vpn.inf.shizuoka.ac.jp/dana-cached/fb/up.cgi?trackid={0}";

		public event EventHandler<EventArgs> ReceivedTrackId;
		public event EventHandler<EventArgs> Complete;
		public event EventHandler<EventArgs> Progess;
		public event EventHandler<EventArgs> Failed;

		private HttpClient _httpClient;

		public bool Success { get; set; }

		public string TrackID { get; set; }
		public string TrackIdUnixTime { get; set; }

		public long CurrentByte { get; set; }
		public long MaxByte { get; set; }

		public string FileName { get; set; } = string.Empty;

		internal void FireReceivedTrackId()
		{
			ReceivedTrackId?.Invoke(this, EventArgs.Empty);
		}

		public async Task<bool> Connect(HttpClient client)
		{
			_httpClient = client;

			if (string.IsNullOrEmpty(TrackID)) return false;

			var stream = await _httpClient.GetStreamAsync(string.Format(VpnUploadStatusUrl, TrackID + TrackIdUnixTime));

			bool result = false;
			using (var sr = new StreamReader(stream))
			{
				while(!sr.EndOfStream)
				{
					var str = sr.ReadLine().Trim();

					if(str.Contains("(") && str.Contains(")"))
					{
						str = str.Replace("uiFrame.SetProgress(", "").Replace(");", "");
						var param = str.Split(',');

						if (param.Length == 3 && !param[2].Contains("unknown"))
						{
							if(long.TryParse(param[0], out var l1))
							{
								CurrentByte = l1;
							}

							if (long.TryParse(param[1], out var l2))
							{
								MaxByte = l2;
							}
							Progess?.Invoke(this, EventArgs.Empty);
						}
						if (param.Length == 3 && param[2].Contains("complete"))
						{
							Complete?.Invoke(this, EventArgs.Empty);
							result = true;
							break;
						}
						if (param.Length == 3 && param[2].Contains("failed"))
						{
							Failed?.Invoke(this, EventArgs.Empty);
							break;
						}
					}

					Console.WriteLine(str);
				}

			}

			return result;
		}


	}
}
