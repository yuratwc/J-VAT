using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace JouhouVPNTool.Connections
{
	public class VPNAccess : IDisposable
	{
		public const string VpnUrlBase = @"https://vpn.inf.shizuoka.ac.jp/dana-na/auth";
		public const string VpnUrlWelcome = VpnUrlBase  + @"/url_3/welcome.cgi";
		public const string VpnUrlLogout = VpnUrlBase  + @"/logout.cgi";
		public const string VpnUrlLogin = VpnUrlBase + @"/url_3/login.cgi";



		private HttpClient _httpClient;

		private VPNDirectory _directoryAccess;

		public VPNDirectory Directories => _directoryAccess;

		private VPNAccess()
		{
			_httpClient = new HttpClient(new HttpClientHandler() { UseCookies = true });
			_httpClient.DefaultRequestHeaders.Referrer = new Uri(VpnUrlWelcome);
			_httpClient.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:87.0) Gecko/20100101 Firefox/87.0");
			_directoryAccess = new VPNDirectory(_httpClient);
		}

		private async Task<bool> SignIn(string username, string password, string url)
		{
			var param = new Dictionary<string, string>()
			{
				{ "tz_offset", "540" },
				{ "clientMAC", string.Empty },
				{ "username", username },
				{ "password", password },
				{ "realm", "Student-Realm" },
				{ "btnSubmit", "Sign+In" }
			};

			return await SignInImpl(url, param);
		}

		private async Task<bool> SignInImpl(string url, Dictionary<string, string> param)
		{
			var httpContent = new FormUrlEncodedContent(param);
			var postdata = await _httpClient.PostAsync(url, httpContent);

			// redirected
			if (postdata.StatusCode == System.Net.HttpStatusCode.OK)
			{
				if (postdata.RequestMessage.RequestUri.AbsoluteUri.Contains("failed"))
				{
					return false;
				}
				else if (postdata.RequestMessage.RequestUri.AbsoluteUri.Contains("confirm"))
				{
					var parser = new AngleSharp.Html.Parser.HtmlParser();
					var htmlContent = await postdata.Content.ReadAsStreamAsync();
					var page = parser.ParseDocument(htmlContent);

					var parameter = page.GetElementById("DSIDFormDataStr");

					var newParam = new Dictionary<string, string>()
					{
						{ "btnContinue", "セッションを続行します" },
						{ parameter.GetAttribute("name"), parameter.GetAttribute("value") }
					};
					return await SignInImpl(url, newParam);
				}

				return true;
			}

			return false;
		}

		public static async Task<VPNAccess> Create(string username, string password, string url = VpnUrlLogin)
		{
			var access = new VPNAccess();
			var r = await access.SignIn(username, password, url);
			if (r)
			{
				return access;
			}
			return null;
		}

		public void Logout()
		{
			_httpClient.GetAsync(VpnUrlLogout);
		}

		public void Dispose()
		{
			((IDisposable)_httpClient).Dispose();
		}
	}
}
