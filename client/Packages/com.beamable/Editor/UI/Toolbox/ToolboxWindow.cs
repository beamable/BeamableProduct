

using System;

namespace Beamable.Editor.Toolbox.UI
{
	public partial class ToolboxWindow
	{
		/// <summary>
		/// This method has a hard-coded reference from the Beamable Installer, and cannot be removed. 
		/// </summary>
		public static void Init()
		{
			// toolbox no longer exists in 2.0+, so instead, we'll open the login page. 
			// LoginWindow.Show();
			throw new NotImplementedException("todo; open the new account window");
		}
	}
}
