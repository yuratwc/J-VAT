using JouhouVPNTool.Connections;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JouhouVPNTool
{
	/// <summary>
	/// LoginWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class LoginWindow : Window
	{

		private VPNAccess _access;

		public VPNAccess LoginResult => _access;

		private LoginSettings _loginSettings;

		public LoginWindow(LoginSettings ls)
		{
			InitializeComponent();
			_loginSettings = ls;

			this.KeyDown += (sender, e) =>
			{
				if (e.Key != Key.Enter) return;
				var direction = Keyboard.Modifiers == ModifierKeys.Shift ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next;
				(FocusManager.GetFocusedElement(this) as FrameworkElement)?.MoveFocus(new TraversalRequest(direction));
			};

			if(!string.IsNullOrEmpty( _loginSettings.UserName))
			{
				 textboxUsername.Text =  _loginSettings.UserName;
				checkboxRemember.IsChecked = true;	
			}

			if (!string.IsNullOrEmpty(_loginSettings.Password))
			{
				textboxPassword.Password = _loginSettings.Password;
				checkboxRemember.IsChecked = true;
			}
		}

		private async void btnLogin_Click(object sender, RoutedEventArgs e)
		{
			if(textboxUsername.Text.Length == 0 || textboxPassword.Password.Length == 0)
			{
				return;
			}

			textboxUsername.IsEnabled = false;
			textboxPassword.IsEnabled = false;
			checkboxRemember.IsEnabled = false;
			btnLogin.IsEnabled = false;
			string username = textboxUsername.Text;
			string password = textboxPassword.Password;
			var result = await Task<bool>.Run(async () =>
			{
				try
				{
					_access = await VPNAccess.Create(username, password);
					return _access != null;
				}
				catch (Exception e)
				{
					Console.Write(e.Message.ToString());
					Console.Write(e.StackTrace.ToString());
					return false;
				}
			});

			if (result)
			{
				if(checkboxRemember.IsChecked == true)
				{
					_loginSettings.UserName = textboxUsername.Text;
					_loginSettings.Password = textboxPassword.Password;

				}
				DialogResult = true;
				Hide();
			}
			else
			{
				textboxUsername.IsEnabled = true;
				textboxPassword.IsEnabled = true;
				checkboxRemember.IsEnabled = true;
				btnLogin.IsEnabled = true;

			}
		}

		private void Window_Closed(object sender, EventArgs e)
		{
#if !DEBUG
			if (DialogResult == null || DialogResult == false)
				Application.Current.Shutdown();
#endif
		}
	}
}
