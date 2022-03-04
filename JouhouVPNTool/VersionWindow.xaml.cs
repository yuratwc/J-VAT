using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
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
	/// VesionWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class VersionWindow : Window
	{
		public VersionWindow()
		{
			InitializeComponent();

			var assembly = Assembly.GetExecutingAssembly();
			var asmName = assembly.GetName();
			var version = asmName.Version;

			labelMain.Content = $"J-VAT v {version}";
		}
	}
}
