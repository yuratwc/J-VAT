using System;
using System.Collections.Generic;
using System.Text;

namespace JouhouVPNTool.Connections
{
	public class VPNFileException : Exception
	{
		public VPNFileException(string msg)
			:base(msg.Trim())
		{

		}
	}
}
