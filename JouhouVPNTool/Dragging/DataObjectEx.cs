using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Permissions;
using System.IO;
using System.Windows;

namespace JouhouVPNTool.Dragging
{
	public class DataObjectEx : System.Windows.IDataObject
	{
		private static readonly TYMED[] ALLOWED_TYMEDS =
			new TYMED[] {
				TYMED.TYMED_ENHMF,
				TYMED.TYMED_GDI,
				TYMED.TYMED_HGLOBAL,
				TYMED.TYMED_ISTREAM,
				TYMED.TYMED_MFPICT};
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct FILEDESCRIPTOR
		{
			public UInt32 dwFlags;
			public Guid clsid;
			public System.Drawing.Size sizel;
			public System.Drawing.Point pointl;
			public UInt32 dwFileAttributes;
			public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
			public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
			public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
			public UInt32 nFileSizeHigh;
			public UInt32 nFileSizeLow;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public String cFileName;
		}
		public struct SelectedItem
		{
			public string FileName;
			public DateTime WriteTime;
			public long FileSize;
		}
		private SelectedItem[] m_SelectedItems;
		private Int32 m_lindex;

		private DataObject _dObj;
		public DataObjectEx(SelectedItem[] selectedItems)
		{
			m_SelectedItems = selectedItems;
			_dObj = new DataObject();
		}


		public object GetData(string format, bool autoConvert)
		{
			if (String.Compare(format, NativeMethods.CFSTR_FILEDESCRIPTORW,
				StringComparison.OrdinalIgnoreCase) == 0 && m_SelectedItems != null)
			{
				_dObj.SetData(NativeMethods.CFSTR_FILEDESCRIPTORW,
					GetFileDescriptor(m_SelectedItems));
			}
			else if (String.Compare(format, NativeMethods.CFSTR_FILECONTENTS,
				StringComparison.OrdinalIgnoreCase) == 0)
			{
				_dObj.SetData(NativeMethods.CFSTR_FILECONTENTS,
					GetFileContents(m_SelectedItems, m_lindex));
			}
			else if (String.Compare(format, NativeMethods.CFSTR_PERFORMEDDROPEFFECT,
				StringComparison.OrdinalIgnoreCase) == 0)
			{
				//TODO: Cleanup routines after paste has been performed
			}
			return _dObj.GetData(format, autoConvert);
		}


		private MemoryStream GetFileDescriptor(SelectedItem[] SelectedItems)
		{
			MemoryStream FileDescriptorMemoryStream = new MemoryStream();
			// Write out the FILEGROUPDESCRIPTOR.cItems value
			FileDescriptorMemoryStream.Write
				(BitConverter.GetBytes(SelectedItems.Length), 0, sizeof(UInt32));
			FILEDESCRIPTOR FileDescriptor = new FILEDESCRIPTOR();
			foreach (SelectedItem si in SelectedItems)
			{
				FileDescriptor.cFileName = si.FileName;
				Int64 FileWriteTimeUtc = si.WriteTime.ToFileTimeUtc();
				FileDescriptor.ftLastWriteTime.dwHighDateTime =
					(Int32)(FileWriteTimeUtc >> 32);
				FileDescriptor.ftLastWriteTime.dwLowDateTime =
					(Int32)(FileWriteTimeUtc & 0xFFFFFFFF);
				FileDescriptor.nFileSizeHigh = (UInt32)(si.FileSize >> 32);
				FileDescriptor.nFileSizeLow = (UInt32)(si.FileSize & 0xFFFFFFFF);
				FileDescriptor.dwFlags = NativeMethods.FD_WRITESTIME |
					NativeMethods.FD_FILESIZE | NativeMethods.FD_PROGRESSUI;
				// Marshal the FileDescriptor structure into a 
				// byte array and write it to the MemoryStream.
				Int32 FileDescriptorSize = Marshal.SizeOf(FileDescriptor);
				IntPtr FileDescriptorPointer = Marshal.AllocHGlobal(FileDescriptorSize);
				Marshal.StructureToPtr(FileDescriptor, FileDescriptorPointer, true);
				Byte[] FileDescriptorByteArray = new Byte[FileDescriptorSize];
				Marshal.Copy(FileDescriptorPointer,
					FileDescriptorByteArray, 0, FileDescriptorSize);
				Marshal.FreeHGlobal(FileDescriptorPointer);
				FileDescriptorMemoryStream.Write
					(FileDescriptorByteArray, 0, FileDescriptorByteArray.Length);
			}
			return FileDescriptorMemoryStream;
		}


		private MemoryStream GetFileContents(SelectedItem[] SelectedItems, Int32 FileNumber)
		{
			MemoryStream FileContentMemoryStream = null;
			if (SelectedItems != null && FileNumber < SelectedItems.Length)
			{
				FileContentMemoryStream = new MemoryStream();
				SelectedItem si = SelectedItems[FileNumber];
				// ******************************************************************
				// TODO: Get the virtual file contents and place 
				// the contents in the byte array bBuffer.
				// If the contents are zero length then a single byte 
				// must be supplied to Windows
				// Explorer otherwise the transfer will fail.  
				// If this is part of a multi-file transfer,
				// the entire transfer will fail at this point 
				// if the buffer is zero length.
				// ******************************************************************
				Byte[] bBuffer = new byte[] { 0x30, 0x31, 0x32, 0x33};

				// Must send at least one byte for a zero length file to prevent stoppages.
				if (bBuffer.Length == 0)
					bBuffer = new Byte[1];
				FileContentMemoryStream.Write(bBuffer, 0, bBuffer.Length);
			}
			return FileContentMemoryStream;
		}

		/*
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		void System.Windows.IDataObject.GetData
	(ref System.Runtime.InteropServices.ComTypes.FORMATETC formatetc,
	out System.Runtime.InteropServices.ComTypes.STGMEDIUM medium)
		{
			if (formatetc.cfFormat == (Int16)DataFormats.GetFormat
				(NativeMethods.CFSTR_FILECONTENTS).Id)
				m_lindex = formatetc.lindex;
			medium = new System.Runtime.InteropServices.ComTypes.STGMEDIUM();
			if (GetTymedUseable(formatetc.tymed))
			{
				if ((formatetc.tymed & TYMED.TYMED_HGLOBAL) != TYMED.TYMED_NULL)
				{
					medium.tymed = TYMED.TYMED_HGLOBAL;
					medium.unionmember = NativeMethods.GlobalAlloc
					(NativeMethods.GHND | NativeMethods.GMEM_DDESHARE, 1);
					if (medium.unionmember == IntPtr.Zero)
					{
						throw new OutOfMemoryException();
					}
					try
					{
						((System.Runtime.InteropServices.ComTypes.IDataObject)this).
						GetDataHere(ref formatetc, ref medium);
						return;
					}
					catch
					{
						NativeMethods.GlobalFree(new HandleRef((STGMEDIUM)medium,
						medium.unionmember));
						medium.unionmember = IntPtr.Zero;
						throw;
					}
				}
				medium.tymed = formatetc.tymed;
				((System.Runtime.InteropServices.ComTypes.IDataObject)this).
				GetDataHere(ref formatetc, ref medium);
			}
			else
			{
				Marshal.ThrowExceptionForHR(NativeMethods.DV_E_TYMED);
			}
		}
		*/
		private static Boolean GetTymedUseable(TYMED tymed)
		{
			for (Int32 i = 0; i < ALLOWED_TYMEDS.Length; i++)
			{
				if ((tymed & ALLOWED_TYMEDS[i]) != TYMED.TYMED_NULL)
				{
					return true;
				}
			}
			return false;
		}

		public object GetData(string format)
		{
			return GetData(format, false);
		}

		public object GetData(Type format)
		{
			return GetData(format.ToString()) ;
		}

		public bool GetDataPresent(string format)
		{
			return GetDataPresent(format, false);
		}

		public bool GetDataPresent(string format, bool autoConvert)
		{
			if (format == "FileDrop") return true;
			if (string.Compare(format, NativeMethods.CFSTR_FILEDESCRIPTORW, StringComparison.OrdinalIgnoreCase) == 0 && m_SelectedItems != null)
			{
				_dObj.SetData(NativeMethods.CFSTR_FILEDESCRIPTORW, GetFileDescriptor(m_SelectedItems));
				return true;
			}
			else if (string.Compare(format, NativeMethods.CFSTR_FILECONTENTS,
				StringComparison.OrdinalIgnoreCase) == 0)
			{
				_dObj.SetData(NativeMethods.CFSTR_FILECONTENTS, GetFileContents(m_SelectedItems, m_lindex));
				return true;
			}
			else if (string.Compare(format, NativeMethods.CFSTR_PERFORMEDDROPEFFECT,
				StringComparison.OrdinalIgnoreCase) == 0)
			{
				//TODO: Cleanup routines after paste has been performed
				return true;
			}
			return _dObj.GetDataPresent(format, autoConvert);

		}

		public bool GetDataPresent(Type format)
		{
			return GetDataPresent(format.ToString());
		}

		public string[] GetFormats()
		{
			return _dObj.GetFormats();
		}

		public string[] GetFormats(bool autoConvert)
		{
			return _dObj.GetFormats(autoConvert);
		}

		public void SetData(object data)
		{
			_dObj.SetData(data);
		}

		public void SetData(string format, object data)
		{
			_dObj.SetData(format, data);
		}

		public void SetData(string format, object data, bool autoConvert)
		{
			_dObj.SetData(format, data, autoConvert);
		}

		public void SetData(Type format, object data)
		{
			_dObj.SetData(format, data);
		}
	}
}
