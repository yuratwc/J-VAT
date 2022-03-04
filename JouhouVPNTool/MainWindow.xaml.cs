using JouhouVPNTool.Connections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace JouhouVPNTool
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private VPNAccess _vpn;
		private LoginSettings _loginData;

		public ObservableCollection<TreeElementModel> TreeFileItems { get; set; }
		public ObservableCollection<VPNFile> CurrentFiles { get; set; }

		//[Obsolete]
		//private string _currentUrl;

		private VPNFile _currentDirectory;
		private Dictionary<string, TreeElementModel> _treeDrivePair;

		private Stack<VPNFile> _backHistory = new Stack<VPNFile>();
		private Stack<VPNFile> _forwardHistory = new Stack<VPNFile>();

		private bool _firstReloaded;

		private Point _draggingMousePos;

		public MainWindow()
		{
			InitializeComponent();

			try
			{
				IconRetriever.Init();
			}
			catch(Exception e)
			{
				MessageBox.Show("アイコンの取得に失敗しました\nアイコンが表示できません", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
				LogException(e);
			}

			TreeFileItems = new ObservableCollection<TreeElementModel>();
			var q = new TreeElementModel { Text = "情報学部VPN",Icon = IconRetriever.GetIcon("Folder") };
			TreeFileItems.Add(q);

			CurrentFiles = new ObservableCollection<VPNFile>();
			DataContext = this;
			_treeDrivePair = new Dictionary<string, TreeElementModel>();

		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			LoadSettings();
			var w = new LoginWindow(_loginData);
			w.Owner = this;
			if (w.ShowDialog() == true)
			{
				_vpn = w.LoginResult;
				InitControls();
			}
		}

		private void LoadSettings()
		{
			var dirName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			try
			{
				if (System.IO.File.Exists(dirName + "/user.config"))
				{
					_loginData = LoginSettings.FromJson(System.IO.File.ReadAllText(dirName + "/user.config"));
				}

				if (_loginData == null)
					_loginData = new LoginSettings();
			}
			catch(Exception e)
			{
				LogException(e);
			}
		}

		private void SaveSettings()
		{
			var dirName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			try
			{
				if (_loginData != null && !string.IsNullOrWhiteSpace(_loginData.UserName))
				{
					string json = _loginData.ToJson();
					System.IO.File.WriteAllText(dirName + "/user.config", json);
				}
				else if(System.IO.File.Exists(dirName + "/user.config"))
				{
					System.IO.File.Delete(dirName + "/user.config");
				}
			}
			catch (Exception e)
			{
				LogException(e);
			}
		}

		private async void InitControls()
		{
			// first, get root files

			await ReloadViews(null);
		}

		private TreeElementModel GetTreeElementFromPath(string[] path, int index, TreeElementModel root)
		{
			if (index >= path.Length) return root;
			if (path[index] == string.Empty) return root;
			foreach(var child in root.Children)
			{
				if (child != null && child.Text == path[index])
				{
					return GetTreeElementFromPath(path, index + 1, child);
				}
			}
			return null;
		}

		private string GetDirectoryPath(VPNFile file)
		{
			if (file == null) return "/";

			if (file.DirectoryPath != null && file.ResourceIndex != null && _treeDrivePair.ContainsKey(file.ResourceIndex) && _treeDrivePair[file.ResourceIndex].RawFile != null && _treeDrivePair[file.ResourceIndex].RawFile.Name != null)
			{
				var decoded = HttpUtility.UrlDecode(file.DirectoryPath).Trim().Replace(@"\", "/");
				if (decoded == "/" || decoded == "")
					return $"/{_treeDrivePair[file.ResourceIndex].RawFile.Name}/";
				else
					return $"/{_treeDrivePair[file.ResourceIndex].RawFile.Name}/{decoded}/";
			}
			return "/";
		}


		private async Task<bool> ReloadViews(VPNFile vpnFile, bool getFromUrl=true)
		{
			if (_vpn == null)
				return false;

			var url = vpnFile == null ? null : vpnFile.Hyperlink;

			var controls = new List<Control>{ fileListView, directoryTreeView };

			controls.ForEach(c => c.IsEnabled = false);
			VPNFile[] files = null;
			try
			{
				if (getFromUrl)
					files = await _vpn.Directories.GetFiles(url);
				else
					files = await _vpn.Directories.GetFileLastMessage();
			}
			catch(VPNFileException vpn)
			{
				MessageBox.Show(vpn.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
				controls.ForEach(c => c.IsEnabled = true);
				LogException(vpn);

				return false;
			}

			if (!_firstReloaded && url == null)
			{
				//TreeFileItems.Clear();
				var root = TreeFileItems.First();
				foreach (var file in files)
				{
					if (file == null || !file.IsDirectory) continue;
					var node = new TreeElementModel() { Text = file.Name, RawFile=file, Icon = IconRetriever.GetIcon("Folder") };
					node.Add(new TreeElementModel());
					root.Add(node);
					_treeDrivePair.Add(file.ResourceIndex, node);
				}
				root.ChildrenAdded = true;
				_firstReloaded = true;
			}
			else if(url != null && vpnFile != null)
			{ 
				var pathes = HttpUtility.UrlDecode(vpnFile.DirectoryPath).Split('/');
				var resource = vpnFile.ResourceIndex;
				if(_treeDrivePair.ContainsKey(resource))
				{
					var root = _treeDrivePair[resource];
					var target = GetTreeElementFromPath(pathes, 0, root);
					if (target != null && !target.ChildrenAdded)
					{
						target.Children.Clear();
						foreach (var file in files)
						{
							if (file == null || !file.IsDirectory) continue;
							var node = new TreeElementModel() { Text = file.Name, RawFile = file, Icon = IconRetriever.GetIcon("Folder") };
							node.Add(new TreeElementModel());
							target.Add(node);
						}
						target.ChildrenAdded = true;
					}
				}
				
			}

			CurrentFiles.Clear();

			foreach (var file in files)
			{
				if (file == null) continue;
				
				if (file.Icon == null)
				{
					file.Icon = IconRetriever.GetIcon(file.IsDirectory ? "Folder" : System.IO.Path.GetExtension(file.Name));
				}
				CurrentFiles.Add(file);
			}

			directoryTreeView.ItemsSource = TreeFileItems;
			fileListView.ItemsSource = CurrentFiles;
			controls.ForEach(c => c.IsEnabled = true);

			textboxPath.Text = GetDirectoryPath(vpnFile);
			DataContext = this;

			//_currentUrl = url;
			_currentDirectory = vpnFile;
			return true;
		}

		private async void OpenFile(string url, string filename)
		{
			if (_vpn == null)
				return;
			try
			{
				var tmpFile = System.IO.Path.GetTempFileName();

				if (await _vpn.Directories.Download(url, tmpFile))
				{
					var newPath = System.IO.Path.ChangeExtension(tmpFile, System.IO.Path.GetExtension(filename));
					System.IO.File.Move(tmpFile, newPath);
					var pc = new System.Diagnostics.Process();
					pc.StartInfo.FileName = newPath;
					pc.StartInfo.UseShellExecute = true;
					pc.Start();
				}
			}
			catch(VPNFileException e)
			{
				MessageBox.Show(e.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
				LogException(e);

			}
			catch (Exception e)
			{
				MessageBox.Show("開くのに失敗しました", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
				LogException(e);
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			_vpn?.Logout();
			if (_vpn != null)
				SaveSettings();
			_vpn = null;

			TempFile.DeleteAll();

		}

		private async void ListViewClickExecute(VPNFile lvi)
		{
			var file = lvi;

			if (file == null) return;
			if (file.IsDirectory)
			{
				var backurl = _currentDirectory;
				if (await ReloadViews(file))
				{
					_backHistory.Push(backurl);
					_forwardHistory.Clear();
					_currentDirectory = lvi;
				}
				
			}
			else
			{
				OpenFile(file.Hyperlink, file.Name);
			}
			
		}

		private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var item = sender as ListViewItem;

			if (item != null && item.Content != null)
			{
				ListViewClickExecute(item.Content as VPNFile);
			}

			
		}

		private void btnBack_Click(object sender, RoutedEventArgs e)
		{
			GoBack();
		}

		private async void GoBack()
		{
			if (_backHistory.Count == 0 || (_backHistory.Peek() == null && _currentDirectory == null))
				return;
			var nowDir = _currentDirectory;
			var url = _backHistory.Peek();
			if (await ReloadViews(url))
			{
				_backHistory.Pop();
				_forwardHistory.Push(nowDir);
			}
		}

		private void treeContextOpenBtn_Click(object sender, RoutedEventArgs e)
		{
			if (fileListView.SelectedIndex == -1)
				return;

			ListViewClickExecute(fileListView.SelectedItem as VPNFile);

		}

		private async void treeContextSaveBtn_Click(object sender, RoutedEventArgs e)
		{
			if (fileListView.SelectedIndex == -1)
				return;

			var file = fileListView.SelectedItem as VPNFile;
			if (file == null || file.IsDirectory)
				return;

			var sfd = new Microsoft.Win32.SaveFileDialog();
			sfd.FileName = file.Name;
			sfd.Filter = $"*{System.IO.Path.GetFileName(file.Name)}";
			if(sfd.ShowDialog() == true)
			{
				await _vpn.Directories.Download(file.Hyperlink, sfd.FileName);
			}

		}

		private void fileListView_PreviewDragOver(object sender, DragEventArgs e)
		{
			if (_dragging)
			{
				e.Effects = DragDropEffects.None;
				e.Handled = true;
				return;
			}
			//Console.WriteLine(System.Windows.DataFormats.FileDrop);
			if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop, true))
			{
				e.Effects = System.Windows.DragDropEffects.Copy;
			}
			else
			{
				e.Effects = System.Windows.DragDropEffects.None;
			}
			e.Handled = true;
		}

		private void UploadCurrentWithWindow(string[] dropFiles)
		{
			if (_currentDirectory == null)
			{
				MessageBox.Show("このフォルダにはアップロードすることができません", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}


			Task.Run(() =>
			{
				Dispatcher.Invoke(() =>
				{
					bool update = false;
					// upload
					foreach (var file in dropFiles)
					{
						var destFile = HttpUtility.UrlDecode(_currentDirectory.DirectoryPath);
						var isDirectory = System.IO.Directory.Exists(file);
						var status = new VPNUploadStatus();
						status.ReceivedTrackId += (a, b) =>
						{
							Task.Run(() =>
							{
								_vpn.Directories.UpdateUploadProgress(status);
							});
						};

						var win = new ProgressWindow("アップロード", status.FileName, true, async (window) =>
						{
							try
							{
								if (isDirectory)
								{
									await UploadDirectory(file, _currentDirectory.ResourceIndex, destFile, status);
								}
								else
								{
									await _vpn.Directories.Upload(file, _currentDirectory.ResourceIndex, destFile, null, false, status);
								}
								return true;
							}
							catch (Exception e)
							{
								LogException(e);
							}
							return false;
						});
						win.Owner = this;
						status.Progess += (a, b) =>
						{
							win.Dispatcher.Invoke(() =>
							{
								win.progressBar.IsIndeterminate = false;
								win.progressBar.Maximum = status.MaxByte;
								win.progressBar.Value = status.CurrentByte;
							});

						};

						status.Complete += (a, b) =>
						{
							Dispatcher.Invoke(async () =>
							{
								await ReloadViews(_currentDirectory);
							});
						};


						win.ShowDialog();
						update |= status.Success;


					}
				});
				//if (update)
				//{
				//	await ReloadViews(_currentDirectory, false);
				//}
			});
		}


		private void fileListView_Drop(object sender, DragEventArgs e)
		{
			if (_dragging)
			{
				return;
			}

			var dropFiles = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
			if (dropFiles == null) return;


			UploadCurrentWithWindow(dropFiles);
		}

		private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
		{
			var tvi = (sender as TreeViewItem);
			if (tvi == null || !tvi.IsExpanded)
				return;

			var tem = tvi.Header as TreeElementModel;
			if (tem != null && !tem.ChildrenAdded && tem.RawFile != null)
			{
				VPNFile[] files = null;
				try
				{
					files = await _vpn.Directories.GetFiles(tem.RawFile.Hyperlink);
				}
				catch (VPNFileException vpn)
				{
					MessageBox.Show(vpn.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
				if (files == null) return;

				var pathes = HttpUtility.UrlDecode(tem.RawFile.DirectoryPath).Split('/');
				var resource = tem.RawFile.ResourceIndex;
				if (_treeDrivePair.ContainsKey(resource))
				{
					var root = _treeDrivePair[resource];
					var target = GetTreeElementFromPath(pathes, 0, root);
					if (target != null && !target.ChildrenAdded)
					{
						target.Children.Clear();
						foreach (var file in files)
						{
							if (file == null || !file.IsDirectory) continue;
							var node = new TreeElementModel() { Text = file.Name, RawFile = file, Icon = IconRetriever.GetIcon("Folder") };
							node.Add(new TreeElementModel());
							target.Add(node);
						}
						target.ChildrenAdded = true;
					}


				}
			}
		}

		private bool _treeClickUpstairs;

		private async void TreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var tvi = (sender as TreeViewItem);
			if (tvi == null)
			{
				_treeClickUpstairs = false;
				return;
			}
			//if (directoryTreeView.SelectedItem != tvi)
			//	return;

			var tem = tvi.Header as TreeElementModel;
			if(tem.RawFile==null)
			{
				_treeClickUpstairs = false;
				return;
			}
			//Console.WriteLine(e.OriginalSource);
			if (_treeClickUpstairs)
			{
				return;
			}
			_treeClickUpstairs = true;
			if (tem.RawFile != null)
			{
				var back = _currentDirectory;
				if(await ReloadViews(tem.RawFile))
				{
					_backHistory.Push(back);
				}
				e.Handled = true;
				return;
			}
		}

		private async void CreateNewDirectory()
		{
			if (_vpn == null)
				return;

			if (_currentDirectory == null)
			{
				MessageBox.Show("このフォルダにフォルダを作成することはできません", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			var d = new NewDirectoryWindow();
			if (d.ShowDialog() == true)
			{
				var dir = HttpUtility.UrlDecode(_currentDirectory.DirectoryPath);
				if (await _vpn.Directories.CreateDirectory(d.FolderName, _currentDirectory.ResourceIndex, dir))
				{
					await ReloadViews(_currentDirectory, false);
				}
			}
		}

		private void contextMenuBtnUploadFile_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new Microsoft.Win32.OpenFileDialog();
			dialog.Title = "アップロードファイルを選択";
			dialog.DefaultExt = ".*";
			dialog.Filter = "すべてのファイル(*.*)|*.*";
			if (dialog.ShowDialog() == true)
			{
				UploadCurrentWithWindow(dialog.FileNames);
			}
		}


		private void contextMenuBtnNewDirectory_Click(object sender, RoutedEventArgs e)
		{
			CreateNewDirectory();
		}
		
		private async void contextMenuBtnDel_Click(object sender, RoutedEventArgs e)
		{
			if (fileListView.SelectedIndex == -1)
				return;
			var file = (fileListView.SelectedItem) as VPNFile;
			if (file == null)
				return;

			if(MessageBox.Show($"{file.Name} を 本当に削除しますか?", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
			{
				try
				{
					var k = HttpUtility.UrlDecode(file.ParentDirectoryPath);
					await _vpn.Directories.Delete(file.ResourceIndex, k, file.Name);
				}
				catch(Exception ex)
				{
					MessageBox.Show("削除に失敗しました", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
					LogException(ex);
					return;
				}
				await Task.Delay(500);
				await ReloadViews(_currentDirectory, false);
			}
		}

		private void contextMenuBtnUndo_Click(object sender, RoutedEventArgs e)
		{
			GoBack();
		}
		private void contextMenuBtnRedo_Click(object sender, RoutedEventArgs e)
		{
			GoForward();
		}

		private void btnForward_Click(object sender, RoutedEventArgs e)
		{
			GoForward();
		}

		private void contextMenuBtnHome_Click(object sender, RoutedEventArgs e)
		{
			JumpHome();
		}

		private async void GoForward()
		{
			if (_forwardHistory.Count == 0 || (_forwardHistory.Peek() == null && _currentDirectory == null))
				return;
			var nowDir = _currentDirectory;
			var url = _forwardHistory.Peek();
			if (await ReloadViews(url))
			{
				_forwardHistory.Pop();
				_backHistory.Push(nowDir);
			}
		}

		private GridViewColumnHeader _lastHeaderClicked = null;
		private ListSortDirection _lastDirection = ListSortDirection.Ascending;

		private void Sort(string sortBy, ListSortDirection direction)
		{
			ICollectionView dataView = CollectionViewSource.GetDefaultView(fileListView.ItemsSource);

			dataView.SortDescriptions.Clear();
			SortDescription sd = new SortDescription(sortBy, direction);
			dataView.SortDescriptions.Add(sd);
			dataView.Refresh();
		}

		private void fileListViewHeader_Click(object sender, RoutedEventArgs e)
		{
			var headerClicked = e.OriginalSource as GridViewColumnHeader;
			ListSortDirection direction;
			if (headerClicked != null)
			{
				if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
				{
					if (headerClicked != _lastHeaderClicked)
					{
						direction = ListSortDirection.Ascending;
					}
					else
					{
						if (_lastDirection == ListSortDirection.Ascending)
						{
							direction = ListSortDirection.Descending;
						}
						else
						{
							direction = ListSortDirection.Ascending;
						}
					}

					var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
					var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;

					Sort(sortBy, direction);

					if (direction == ListSortDirection.Ascending)
					{
						headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
					}
					else
					{
						headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;
					}

					// Remove arrow from previously sorted header
					if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
					{
						_lastHeaderClicked.Column.HeaderTemplate = null;
					}

					_lastHeaderClicked = headerClicked;
					_lastDirection = direction;
				}
			}

		}

		private Point _dragStart;
		private bool _dragging;
		private bool _dropStart;
		private bool _canDrop;
		private string _tmpFile;
		private VPNFile _draggingFile;
		private static object HitTest<T>(UIElement itemsControl, Func<IInputElement, Point> getPosition) where T : class
		{
			var pt = getPosition(itemsControl);
			var result = itemsControl.InputHitTest(pt) as DependencyObject;
			return result;
		}

		private void fileListView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			_canDrop = false;
			_dragging = false;
			_dropStart = false;
			_tmpFile = null;
			if (!(sender is ListView))
				return;

			var curPos = e.GetPosition(fileListView);

			if (fileListView.ActualWidth - SystemParameters.VerticalScrollBarWidth <= curPos.X)
			{
				return;
			}

			var parent = sender as ItemsControl;
			var draggedItem = e.Source as FrameworkElement;
			if ((parent == null) || (draggedItem == null))
				return;

			var hit = HitTest<FrameworkElement>(fileListView, e.GetPosition) as FrameworkElement;

			if (hit == null || hit.DataContext == null || !(hit.DataContext is VPNFile))
			{
				return;
			}
			//var draggingItem = parent.ContainerFromElement(draggedItem);
			//if (draggingItem == null)
			//	return;

			var lvi = hit.DataContext as VPNFile;
			if (lvi == null) return;

			if ((e.LeftButton & MouseButtonState.Pressed) == MouseButtonState.Pressed)
			{
				_draggingFile = lvi;
				
				//_tmpFile = tmpFile;
				_dragStart = e.GetPosition(fileListView);
				_dragging = true;
				_dropStart = false;

			}
		}

		private void fileListView_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			bool IsDragStartable(Vector delta)
			{
				return (SystemParameters.MinimumHorizontalDragDistance < Math.Abs(delta.X)) ||
					   (SystemParameters.MinimumVerticalDragDistance < Math.Abs(delta.Y));
			};
			_canDrop = false;

			if ((e.LeftButton & MouseButtonState.Pressed) == MouseButtonState.Pressed)
			{
				var curPos = e.GetPosition(fileListView);
				if (IsDragStartable(curPos - _dragStart) && _dragging && !_dropStart)
				{
					_dropStart = true;
					var newPath = TempFile.GetTempFile(_draggingFile.Name);
					_tmpFile = newPath;
					_canDrop = true;
					DragDrop.DoDragDrop(fileListView, new DataObject(DataFormats.FileDrop, new string[] { _tmpFile }), DragDropEffects.Copy);
					_canDrop = false;
					_dropStart = false;
					_dragging = false;

				}
				
			}
			else
			{
				_dropStart = false;
				_dragging = false;
			}
		}

		private void fileListView_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			if ((e.LeftButton & MouseButtonState.Pressed) == MouseButtonState.Pressed)
			{
				_dragging = false;
				_dropStart = false;
				_tmpFile = null;
			}
		}

		private void fileListView_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
		{
			_draggingMousePos = this.PointFromScreen(DragDropNotification.GetCursorPos());

			var mousePos = _draggingMousePos;
			if (e.EscapePressed)
			{
				e.Action = DragAction.Cancel;
				e.Handled = true;
			}

			if ((e.KeyStates & DragDropKeyStates.RightMouseButton) == DragDropKeyStates.RightMouseButton)
			{
				e.Action = DragAction.Cancel;
				e.Handled = true;
			}
			//Console.WriteLine("{0} {1}", mousePos.X, mousePos.Y);
			if (e.Action == DragAction.Cancel)
			{
				_dragging = false;
				_dropStart = false;
				_tmpFile = null;
				return;
			}

			if (string.IsNullOrEmpty(_tmpFile) || !_canDrop) return;

			if (mousePos.X >= 0 && mousePos.X <= this.ActualWidth && mousePos.Y >= 0 && mousePos.Y <= ActualHeight)
				return;

			
			if (e.Action != DragAction.Drop && (e.KeyStates & DragDropKeyStates.LeftMouseButton) == 0)
			{
				Dragging.NativeMethods.SetCapture(this.Handle);

				var k = new ProgressWindow("ダウンロード", _draggingFile.Name, true, async (w)=>
				{
					bool result = false;
					try
					{
						if (_draggingFile.IsDirectory)
						{
							var zipPath = _tmpFile + ".zip";
							var k = _draggingFile.Name;
							result = await _vpn.Directories.DownloadDirectory(_draggingFile.ResourceIndex, _draggingFile.ParentDirectoryPath, k, "test.zip", zipPath);
							System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, System.IO.Path.GetDirectoryName(_tmpFile), Encoding.GetEncoding("shift_jis"));
						}
						else
						{
							result = await _vpn.Directories.Download(_draggingFile.Hyperlink, _tmpFile);
						}
					}
					catch (Exception e)
					{
						LogException(e);
					}
					return result;
				});
				k.Owner = this;
				if(k.ShowDialog() == true)
				{
					e.Action = DragAction.Drop;
				}
				else
				{
					e.Action = DragAction.Cancel;
				}
			}
			e.Handled = true;
		}

		public IntPtr Handle
		{
			get
			{
				var helper = new System.Windows.Interop.WindowInteropHelper(this);
				return helper.Handle;
			}
		}

		private void btnMenuVersionInfo_Click(object sender, RoutedEventArgs e)
		{
			var w = new VersionWindow();
			w.Owner = this;
			w.ShowDialog();
		}


		private async void btnToolReload_Click(object sender, RoutedEventArgs e)
		{
			if (_currentDirectory == null)
				return;

			await ReloadViews(_currentDirectory);
		}

		private void btnToolHome_Click(object sender, RoutedEventArgs e)
		{
			JumpHome();
		}

		private async void JumpHome()
		{
			var cur = _currentDirectory;
			if (await ReloadViews(null))
			{
				_backHistory.Push(cur);
				_currentDirectory = null;

			}
		}

		private void btnMenuExit_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		public async Task<VPNUploadStatus> UploadDirectory(string src, string resourceIndex, string destPath, VPNUploadStatus status = null)
		{
			if (status == null) status = new VPNUploadStatus();
			status.Success = false;
			if (!System.IO.Directory.Exists(src))
			{
				MessageBox.Show("フォルダが存在しません", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
				return status;
			}
			var tempFile = TempFile.GetTempFile(System.IO.Path.GetFileName(src) + ".zip");

			try
			{
				System.IO.Compression.ZipFile.CreateFromDirectory(src, tempFile, System.IO.Compression.CompressionLevel.Optimal, true, System.Text.Encoding.GetEncoding("shift_jis"));

				if (!System.IO.File.Exists(tempFile))
				{
					return status;
				}
			}
			catch(Exception e)
			{
				MessageBox.Show("一時ファイルの作成に失敗しました", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
				LogException(e);

			}

			try
			{
				var ret = await _vpn.Directories.Upload(tempFile, resourceIndex, destPath, null, true, status);

				return ret;
			}
			catch(VPNFileException e)
			{
				MessageBox.Show(e.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
				LogException(e);
			}
			catch (Exception e)
			{
				MessageBox.Show("アップロードに失敗しました", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
				LogException(e);
			}
			return status;
		}


		private void LogException(Exception e)
		{
			Console.Error.WriteLine(e);
			Console.Error.WriteLine(e.Message);
			Console.Error.WriteLine(e.StackTrace);
		}

	}
}
