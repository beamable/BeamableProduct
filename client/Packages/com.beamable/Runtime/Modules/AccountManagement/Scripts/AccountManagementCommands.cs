using System.Linq;
using Beamable.Signals;
using Beamable.ConsoleCommands;
using Beamable.Platform.SDK.Auth;
using UnityEngine;
using UnityEngine.Scripting;

namespace Beamable.AccountManagement
{
   [BeamableConsoleCommandProvider]
   public class AccountManagementCommands
   {
      [Preserve]
      public AccountManagementCommands()
      {

      }

      [BeamableConsoleCommand("account_toggle", "emit an account management toggle event", "account_toggle")]
      public string ToggleAccount(string[] args)
      {
         DeSignalTower.ForAll<AccountManagementSignals>(s => s.OnToggleAccountManagement?.Invoke(!AccountManagementSignals.ToggleState));
         return "okay";
      }

      [BeamableConsoleCommand("account_list", "list user data", "account_list")]
      public string ListCredentials(string[] args)
      {
         API.Instance.Then(de =>
         {
            de.GetDeviceUsers().Then(all =>
            {
               all.ToList().ForEach(bundle =>
               {
                  var user = bundle.User;
                  var userType = user.id == de.User.id ? "CURRENT" : "DEVICE";
                  Debug.Log($"{userType} : EMAIL: [{user.email}] ID: [{user.id}] 3RD PARTIES: [{string.Join(",", user.thirdPartyAppAssociations)}]");
               });
            });
         });
         return "";
      }
   }
}