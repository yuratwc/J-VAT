using JouhouVPNTool.Connections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace JouhouVPNTool
{
	public class TreeElementModel : INotifyPropertyChanged
	{
		private bool _isExpanded;
		private TreeElementModel _parent;
		private ObservableCollection<TreeElementModel> _elements;

		public ObservableCollection<TreeElementModel> Children => _elements;

		public string Text { get; set; } = "";

		private VPNFile _vpnFile;
		public VPNFile File { get { return _vpnFile; } set { _vpnFile = value; } }

		private bool _childrenAdded;
		public bool ChildrenAdded { get { return _childrenAdded; } set { _childrenAdded = value; } }

		public bool IsExpanded { get { return _isExpanded; }set { _isExpanded = value;OnPropertyChanged("IsExpanded"); } }
		public TreeElementModel Parent { get { return _parent; }set { _parent = value;OnPropertyChanged("Parent"); } }

		public VPNFile RawFile { get; set; }

		public System.Windows.Media.ImageSource Icon { get; set; }

		public TreeElementModel()
		{
			_elements = new ObservableCollection<TreeElementModel>();
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged(string name)
		{
			if (null == this.PropertyChanged) return;
			this.PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		public void Add(TreeElementModel child)
		{
			child.Parent = this;
			Children.Add(child);
		}

	}
}
