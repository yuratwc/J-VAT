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
	/// ProgressWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class ProgressWindow : Window
	{
		private Func<ProgressWindow, Task<bool>> _action;

		public ProgressWindow()
		{
			InitializeComponent();

			this.Loaded += ProgressWindow_Loaded;
		}

		private async void ProgressWindow_Loaded(object sender, RoutedEventArgs e)
		{
			var result = await _action(this);
			DialogResult = result;
			this.Close();
		}

		public ProgressWindow(string title, string labelStr, bool unknownProgress, Func<ProgressWindow, Task<bool>> act)
			:this()
		{
			this.Title = title;
			label.Content = labelStr;
			progressBar.IsIndeterminate = unknownProgress;
			_action = act;

		}
	}
}
