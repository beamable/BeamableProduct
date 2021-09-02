using System;
using Beamable.Api.Connectivity;

namespace Beamable.Platform.Tests.Connectivity
{
   public class MockConnectivityService : IConnectivityService
   {
      private bool _connectivity = true;
      public bool HasConnectivity => _connectivity;
      public event Action<bool> OnConnectivityChanged;
      public void SetHasInternet(bool hasInternet)
      {
         _connectivity = hasInternet;
         OnConnectivityChanged?.Invoke(_connectivity);
      }

      public void ReportInternetLoss()
      {
         SetHasInternet(false);
      }
   }
}