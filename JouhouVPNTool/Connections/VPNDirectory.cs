using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.StaticFiles;

namespace JouhouVPNTool.Connections
{
	public class VPNDirectory
	{
		public const string VpnUrlDownloadFile = @"https://vpn.inf.shizuoka.ac.jp";
		public const string VpnUrlDownloadDirectory = @"https://vpn.inf.shizuoka.ac.jp/dana/download/download.cgi?url=/dana/fb/smb/wfmd.cgi";
		public const string VpnUrlFilesRoot = @"https://vpn.inf.shizuoka.ac.jp/dana/fb/smb/swg.cgi";
		public const string VpnUrlUploadFile = @"https://vpn.inf.shizuoka.ac.jp/dana/fb/smb/wu.cgi";
		public const string VpnUrlDeleteFile = @"https://vpn.inf.shizuoka.ac.jp/dana/fb/smb/wfb.cgi";
		public const string VpnUrlCreateDirectory = @"https://vpn.inf.shizuoka.ac.jp/dana/fb/smb/wnf.cgi";
		public const string VpnUrlSelectZipName = @"https://vpn.inf.shizuoka.ac.jp/dana/fb/smb/wfmd.cgi";

		private const uint UploadFileLimit = 1024 * 500;

		private readonly HttpClient _httpClient;
		private string _xsauth;

		private string _lastPage;

		public VPNDirectory(HttpClient client)
		{
			_httpClient = client;
		}

		public async void UpdateUploadProgress(VPNUploadStatus status)
		{
			await status.Connect(_httpClient);
		}

		public async Task<bool> Download(string url, string dest)
		{
			var response = await _httpClient.GetAsync(VpnUrlDownloadFile + url, HttpCompletionOption.ResponseHeadersRead);
			if (response.StatusCode == System.Net.HttpStatusCode.OK)
			{
				using (var content = response.Content)
				using (var stream = await content.ReadAsStreamAsync())
				using (var fileStream = new FileStream(dest, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))

				{
					stream.CopyTo(fileStream);
				}
				return true;
			}


			return false;

		}

		public async Task<bool> DownloadDirectory(string resourceIndex, string directory, string dirName, string zipName, string dest)
		{
			if (string.IsNullOrWhiteSpace(_xsauth) || string.IsNullOrWhiteSpace(resourceIndex)) return false;

			var dirName2 = System.Web.HttpUtility.UrlEncode(dirName) + ",";
			var param = new Dictionary<string, string>()
			{
				{ "xsauth", _xsauth },
				{ "t", "p" },
				{ "v", resourceIndex },
				{ "si", "0" },
				{ "ri", "0" },
				{ "pi", "0" },
				{ "acttype", "download" },
				{ "ignoreDfs", "1" },
				{ "dir", directory },
				{ "files", dirName }
			};
			/*
			var httpContent = new FormUrlEncodedContent(param);
			var response = await _httpClient.PostAsync(VpnUrlSelectZipName, httpContent);
			if (response.StatusCode != System.Net.HttpStatusCode.OK)
				return false;
			var contentStr = await response.Content.ReadAsStringAsync();
			param = new Dictionary<string, string>()
			{
				{ "xsauth", _xsauth },
				{ "t", "p" },
				{ "v", resourceIndex },
				{ "si", "0" },
				{ "ri", "0" },
				{ "pi", "0" },
				{ "sb", "name" },
				{ "so", "asc" },
				{ "btnDownload", "ファイルのダウンロード" },
				{ "zipArchiveName", "" },
				{ "ignoreDfs", "1" },
				{ "dir", directory },
				{ "files", dirName2 }
			};
			httpContent = new FormUrlEncodedContent(param);
			response = await _httpClient.PostAsync(VpnUrlDeleteFile, httpContent);
			if (response.StatusCode != System.Net.HttpStatusCode.OK)
				return false;
			contentStr = await response.Content.ReadAsStringAsync();
			*/
			param = new Dictionary<string, string>()
			{
				{ "xsauth", _xsauth },
				{ "t", "p" },
				{ "v", resourceIndex },
				{ "si", "0" },
				{ "ri", "0" },
				{ "pi", "0" },
				{ "acttype", "DownloadZip" },
				{ "ignoreDfs", "1" },
				{ "dir", directory },
				{ "files", dirName2 },
				{ "file", "" },
				{ "confirm", "yes" }
			};

			var httpContent = new FormUrlEncodedContent(param);

			var response = await _httpClient.PostAsync(VpnUrlDownloadDirectory, httpContent);
			if (response.StatusCode == System.Net.HttpStatusCode.OK)
			{
				try
				{
					var content = response.Content;
					var stream = await content.ReadAsStreamAsync();
					//Console.WriteLine(stream.Length);
					var fileStream = new FileStream(dest, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
					stream.CopyTo(fileStream);

					fileStream.Dispose();
					stream.Dispose();

				}
				catch(Exception e)
				{
					Console.WriteLine(e.Message);
				}
				return true;
			}


			return false;

		}

		public async Task<bool> Delete(string resourceIndex, string directory, string file)
		{
			if (string.IsNullOrWhiteSpace(_xsauth) || string.IsNullOrWhiteSpace(resourceIndex)) return false;

			var param = new Dictionary<string, string>()
			{
				{ "xsauth", _xsauth },
				{ "t", "p" },
				{ "v", resourceIndex },
				{ "si", "0" },
				{ "ri", "0" },
				{ "pi", "0" },
				{ "acttype", "delete" },
				{ "ignoreDfs", "1" },
				{ "dir", directory },
				{ "files", System.Web.HttpUtility.UrlEncode(file) }
			};

			var httpContent = new FormUrlEncodedContent(param);
			try
			{
				var response = await _httpClient.PostAsync(VpnUrlDeleteFile, httpContent);
				if (response.StatusCode != System.Net.HttpStatusCode.OK)
				{
					return false;
				}

				var pageContent = await response.Content.ReadAsStringAsync();
				CheckDocumentError(pageContent);
				param.Add("confirm", "yes");
				param.Add("btnSubmit", "はい");
				httpContent = new FormUrlEncodedContent(param);
				response = await _httpClient.PostAsync(VpnUrlDeleteFile, httpContent);
				if (response.StatusCode != System.Net.HttpStatusCode.OK)
				{
					return false;
				}
				_lastPage = await response.Content.ReadAsStringAsync();
				CheckDocumentError(_lastPage);

				return true;
			}
			catch (Exception e)
			{
				throw e;
			}
		}

		public async Task<bool> CreateDirectory(string name, string resourceIndex, string path)
		{
			if (string.IsNullOrWhiteSpace(_xsauth) || string.IsNullOrWhiteSpace(resourceIndex) || string.IsNullOrWhiteSpace(name)) return false;

			var param = new Dictionary<string, string>()
			{
				{ "xsauth", _xsauth },
				{ "confirm", "yes" },
				{ "t", "p" },
				{ "v", resourceIndex },
				{ "si", "0" },
				{ "ri", "0" },
				{ "dir", path },
				{ "folder", name },
				{ "create", "フォルダの作成" },
				{ "acttype", "create" },
				{ "ignoreDfs", "1" }
			};
			var httpContent = new FormUrlEncodedContent(param);

			var response = await _httpClient.PostAsync(VpnUrlCreateDirectory, httpContent);
			var htmlContent = await response.Content.ReadAsStringAsync();
			CheckDocumentError(htmlContent);

			if (response.StatusCode != System.Net.HttpStatusCode.OK)
			{
				return false;
			}

			_lastPage = htmlContent;

			return true;
		}



		public async Task<VPNUploadStatus> Upload(string src, string resourceIndex, string destPath, string rename = null, bool extractZip = false, VPNUploadStatus status = null)
		{
			if( status == null)
			{
				status = new VPNUploadStatus();
			}
			
			status.Success = false;
			status.FileName = Path.GetFileName(src);
			if (string.IsNullOrWhiteSpace(_xsauth) || string.IsNullOrWhiteSpace(resourceIndex) || !File.Exists(src))
			{
				return status;
			}
			if (rename == null)
			{
				rename = Path.GetFileName(src);
			}

			var param = new Dictionary<string, string>()
			{
				{ "xsauth", _xsauth },
				{ "t", "p" },
				{ "v", resourceIndex },
				{ "si", "0" },
				{ "ri", "0" },
				{ "dir", destPath },
				{ "acttype", "upload" },
				{ "ignoreDfs", "1" }
			};
			var httpContent = new FormUrlEncodedContent(param);

			var response = await _httpClient.PostAsync(VpnUrlUploadFile, httpContent);

			if (response.StatusCode != System.Net.HttpStatusCode.OK)
			{
				return status;
			}

			var htmlParser = new HtmlParser();
			var htmlContent = await response.Content.ReadAsStringAsync();
			var parsedPage = htmlParser.ParseDocument(htmlContent);
			CheckDocumentError(parsedPage);

			UpdateXsauth(parsedPage);

			var dom = parsedPage.QuerySelector("input[name='trackid']");
			if (dom == null || !dom.HasAttribute("value"))
			{
				return status;
			}

			var trackId = dom.GetAttribute("value");
			status.TrackID = trackId;
			param = new Dictionary<string, string>()
			{
				{ "xsauth", _xsauth },
				{ "txtServerUploadID", "" },

				{ "t", "p" },
				{ "v", resourceIndex },
				{ "si", "0" },
				{ "ri", "0" },
				{ "dir", destPath },
				{ "acttype", "upload" },
				{ "confirm", "yes" },
				{ "trackid",  trackId},
				{ "ignoreDfs", "1" },
				{ "btnUpload", "アップロード"}
			};

			var content = new MultipartFormDataContent();

			
			FileStream fs = null;
			for (int i = 1; i<=5; i++)
			{
				if (i == 1)
				{
					fs = new FileStream(src, FileMode.Open, FileAccess.Read);

					var streamContent = new StreamContent(fs);
					streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = $"file{i}", FileName = System.Web.HttpUtility.UrlEncode(Path.GetFileName(src)) };
					streamContent.Headers.ContentType = new MediaTypeHeaderValue(GetFileMimeType(src));
					content.Add(streamContent);
				}
				else
				{
					var sc = new StringContent(string.Empty);
					sc.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = $"file{i}", FileName= string.Empty };
					sc.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
				}
				param.Add($"txtRenameFile{i}", rename);
				if(extractZip)
				{
					param.Add($"chkUnzip{i}", extractZip ? "on" : "off");
				}
			}

			foreach(var key in param.Keys)
			{
				var sc = new StringContent(param[key]);
				sc.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = key };
				content.Add(sc);
			}

			dom = parsedPage.QuerySelector("form[name='frmUpload']");
			if (dom == null || !dom.HasAttribute("action"))
			{
				return status;
			}

			var uploadUrl = $"{dom.GetAttribute("action")}trackid={trackId}{GetUnixTime(DateTime.Now)}";
			status.TrackIdUnixTime = $"{ GetUnixTime(DateTime.Now)}";
			status.FireReceivedTrackId();
			response = await _httpClient.PostAsync(uploadUrl, content);
			fs.Dispose();

			if (response.StatusCode != System.Net.HttpStatusCode.OK)
			{
				return status;
			}
			_lastPage = await response.Content.ReadAsStringAsync();
			CheckDocumentError(_lastPage);

			var parser = new HtmlParser().ParseDocument(_lastPage);
			var msgDom = parser.QuerySelector("input[name='msg']");
			if (msgDom != null && msgDom.HasAttribute("value"))
			{
				var msg = msgDom.GetAttribute("value").Trim();
				if(!string.IsNullOrWhiteSpace(msg))
				{
					throw new VPNFileException(msg);
				}
			}
			status.Success = true;
			return status;
		}

		public async Task<VPNFile[]> GetFileLastMessage()
		{
			if (string.IsNullOrEmpty(_lastPage))
				return new VPNFile[0];

			var parser = await GetPageParserFromHtml(_lastPage);
			return GetFileFromDocument(parser);
		}

		public async Task<VPNFile[]> GetFiles(string url)
		{
			if (string.IsNullOrEmpty(url) || url == VpnUrlFilesRoot)
			{
				return await GetRootFiles();
			}

			var parsedPage = await GetPageParserFromUrl(url);

			return GetFileFromDocument(parsedPage);
		}

		private void CheckDocumentError(string page)
		{
			var p = new HtmlParser().ParseDocument(page);
			CheckDocumentError(p);
		}

		private void CheckDocumentError(IDocument page)
		{
			var message = page.GetElementById("messageTable");
			if (message != null)
			{
				throw new VPNFileException(message.TextContent);
			}
		}

		private VPNFile[] GetFileFromDocument(IDocument parsedPage)
		{
			CheckDocumentError(parsedPage);

			var expectedFormats = new string[] { "ddd MMM dd HH:mm:ss yyyy", "ddd MMM  d HH:mm:ss yyyy" };

			var files = parsedPage.QuerySelectorAll("#table_wfb_5 > tbody > tr").Skip(1)
				.Where(item =>
				{
					return item.Children.Length == 6;
				})
				.Select(item =>
				{
					var tds = item.GetElementsByTagName("td");
					var hyperlink = tds[2].FirstElementChild as IElement;
					var label = tds[2].TextContent.Trim();
					if (hyperlink.Children.Length > 0)
					{
						label = hyperlink.FirstElementChild.TextContent;
					}

					var trimed = tds[3].TextContent.Trim();
					var isDir = trimed == "Folder" || trimed == "フォルダ";

					var size = tds[4].TextContent.Trim();
					if (string.IsNullOrWhiteSpace(tds[5].TextContent.Trim()))
					{
						return null;
					}
					var ts = DateTime.ParseExact(tds[5].TextContent.Trim(), expectedFormats, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.None);
					var href = hyperlink.HasAttribute("href") ? hyperlink.GetAttribute("href") : string.Empty;
					return new VPNFile() { Name = label, IsDirectory = isDir, Hyperlink = href, TimeStamp = ts, Size = size };
				}).Where(item => item != null);

			return files.ToArray();
		}

		public async Task<VPNFile[]> GetFiles(string resourceIndex, string path)
		{
			string url = $"{VpnUrlFilesRoot}?t={resourceIndex}&amp;si=0&amp;ri=0&amp;pi=0&amp;sb=name&amp;so=asc&amp;dir={path}";
			return await GetFiles(url);
		}

		private async Task<VPNFile[]> GetRootFiles()
		{
			var parsedPage = await GetPageParserFromUrl(VpnUrlFilesRoot);

			var files = parsedPage.QuerySelectorAll("#table_swg_7 > tbody > tr").Select(item =>
			{
				var tds = item.GetElementsByTagName("td");
				if (tds == null)
					return null;
				var hyperlink = tds[1].FirstElementChild as IElement;
				if (hyperlink == null)
					return null;
				var label = tds[1].TextContent.Trim();

				if (hyperlink.Children.Length > 0)
				{
					label = hyperlink.FirstElementChild.TextContent;
				}

				var href = hyperlink.HasAttribute("href") ? hyperlink.GetAttribute("href") : string.Empty;
				return new VPNFile() { Name = label, IsDirectory = true, Hyperlink = href };
			}).Where(item => item != null);

			return files.ToArray();

		}


		private async Task<IDocument> GetPageParserFromUrl(string url)
		{
			var response = await _httpClient.GetAsync(url);
			var c = await response.Content.ReadAsStringAsync();
			return await GetPageParserFromHtml(c);
		}

		private async Task<IDocument> GetPageParserFromHtml(string html)
		{
			var config = Configuration.Default.WithJs();
			var bc = BrowsingContext.New(config);

			var parsedPage = await bc.OpenAsync(req => req.Content(html));
			UpdateXsauth(parsedPage);
			CheckDocumentError(parsedPage);
			return parsedPage;
		}

		private void UpdateXsauth(IDocument doc)
		{
			_xsauth = string.Empty;
			var dom = doc.QuerySelector("input[name='xsauth']");
			if (dom == null)
				return;

			if(dom.HasAttribute("value"))
			{
				_xsauth = dom.GetAttribute("value");
			}
		}

		private string GetFileMimeType(string file)
		{
			string contentType;
			new FileExtensionContentTypeProvider().TryGetContentType(file, out contentType);
			return contentType ?? "application/octet-stream";
		}

		private long GetUnixTime(DateTime targetTime)
		{
			 var unixEpcoch =new DateTime(1970, 1, 1, 0, 0, 0, 0);
			targetTime = targetTime.ToUniversalTime();
			TimeSpan elapsedTime = targetTime - unixEpcoch;
			return (long)elapsedTime.TotalSeconds;
		}


	}
}
