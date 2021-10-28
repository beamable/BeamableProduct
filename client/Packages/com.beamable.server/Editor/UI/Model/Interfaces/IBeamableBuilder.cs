using System;
using System.Threading.Tasks;

namespace Beamable.Editor.UI.Model
{
    public interface IBeamableBuilder
    {
        Action<bool> OnIsRunningChanged { get; set; }
        bool IsRunning { get; set; }
        Task CheckIfIsRunning();
        Task TryToStart();
        Task TryToStop();
        Task TryToRestart();
    }
}