using System;

namespace Beamable.Editor.UI
{
	public interface IDelayedActionWindow
	{
		void AddDelayedAction(Action act);
		void Repaint();
	}
}
