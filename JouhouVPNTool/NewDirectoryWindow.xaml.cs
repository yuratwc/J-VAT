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
	public partial class NewDirectoryWindow : Window
	{
		private string _folderName = null;
		public string FolderName => _folderName;

		public NewDirectoryWindow()
		{
			InitializeComponent();
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Hide();
		}

		private void btnOK_Click(object sender, RoutedEventArgs e)
		{
			if(!IsValidFolderName())
			{
				return;
			}
			_folderName = textBoxFolderName.Text;
			DialogResult = true;

			Hide();


		}

		private bool IsValidFolderName()
		{
			var chars = System.IO.Path.GetInvalidFileNameChars();
			return !(string.IsNullOrWhiteSpace(textBoxFolderName.Text) || textBoxFolderName.Text.IndexOfAny(chars) >= 0);
		}

		private void textBoxFolderName_TextChanged(object sender, TextChangedEventArgs e)
		{
			btnOK.IsEnabled = IsValidFolderName();
		}
	}
}
