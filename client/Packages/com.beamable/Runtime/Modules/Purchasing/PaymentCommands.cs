using System;
using System.Collections.Generic;
using Beamable.Api;
using Beamable.ConsoleCommands;
using Beamable.Coroutines;
using Beamable.Api.Payments;
using Beamable.Service;
using UnityEngine.Scripting;

namespace Beamable.Purchasing
{

  [BeamableConsoleCommandProvider]
  public class PaymentCommands
  {
    private BeamableConsole Console => ServiceManager.Resolve<BeamableConsole>();
    private CoroutineService CoroutineService => ServiceManager.Resolve<CoroutineService>();

    [Preserve]
    public PaymentCommands()
    {
    }

    [BeamableConsoleCommand("TRACK_PAYMENT", "Track a test payment audit", "TRACK_PAYMENT")]
    private string TrackPurchase(string[] args)
    {
      var platform = ServiceManager.Resolve<PlatformService>();
      var payments = platform.Payments;

      var obtainCurrency = new List<ObtainCurrency>();
      var obtainItems = new List<ObtainItem>();

      var currency = new ObtainCurrency();
      currency.symbol = "coins";
      currency.amount = 100;

      obtainCurrency.Add(currency);

      var request = new TrackPurchaseRequest(
        "bundle_of_coins",
        "offer_t10",
        "com.beamable.test.offer_t10",
        "main",
        9.99,
        "USD",
        obtainCurrency,
        obtainItems
      );

      payments.Track(request).Then( _ => {
        Console.Log("Purchase Tracked");
      });

      return String.Empty;
    }
  }
}
