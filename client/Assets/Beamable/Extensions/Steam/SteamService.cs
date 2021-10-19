
#if USE_STEAMWORKS
using System;
using System.Collections.Generic;
using UnityEngine;
using Beamable.Common;
using Beamable.Common.Steam;
using Steamworks;
using System.Text;

public class SteamService : ISteamService
{
   private bool _transactionRegistered = false;
   private List<Action<SteamTransaction>> _callbacks;

   Promise<Unit> ISteamService.RegisterAuthTicket()
   {
      var promise = new Promise<Unit>();

      GenerateAuthTicket().Then(ticket =>
      {
         Beamable.API.Instance.FlatMap(beamable =>
         {
            return beamable.Requester.Request<Beamable.Common.Api.EmptyResponse>(
                        Beamable.Common.Api.Method.POST,
                        $"/basic/payments/steam/auth",
                        new SteamTicketRequest(ticket));
         }).Then(f => promise.CompleteSuccess(PromiseBase.Unit))
         .Error(ex => promise.CompleteError(ex));
      });

      return promise;
   }

   Promise<string> ISteamService.GenerateLoginRequest()
   {
      return GenerateAuthTicket().Map(ticket =>
       {
          var request = new AuthenticateUserRequest(SteamUser.GetSteamID().ToString(), ticket);
          var steamRequest = JsonUtility.ToJson(request);
          var encodedRequest = Encoding.UTF8.GetBytes(steamRequest);
          return Convert.ToBase64String(encodedRequest);
       });

   }

   private Promise<string> GenerateAuthTicket()
   {
      var promise = new Promise<string>();

      if (!SteamManager.Initialized)
      {
         promise.CompleteError(new Exception("Steamworks not initialized."));
         return promise;
      }

      byte[] steamAuthTicketBuffer = new byte[1024];
      uint steamAuthTicketBufferSize = 1024;

      Callback<GetAuthSessionTicketResponse_t>.Create(_ =>
      {
         byte[] usedBytes = new List<byte>(steamAuthTicketBuffer).GetRange(0, (int)steamAuthTicketBufferSize).ToArray();
         string ticket = BitConverter.ToString(usedBytes).Replace("-", string.Empty);
         promise.CompleteSuccess(ticket);
      });

      SteamUser.GetAuthSessionTicket(steamAuthTicketBuffer, (int)steamAuthTicketBufferSize, out steamAuthTicketBufferSize);

      return promise;
   }

   Promise<SteamProductsResponse> ISteamService.GetProducts()
   {
      if (!SteamManager.Initialized)
      {
         var promise = new Promise<SteamProductsResponse>();
         promise.CompleteError(new Exception("Steamworks not initialized."));
         return promise;
      }

      long steamID = (long)SteamUser.GetSteamID().m_SteamID;
      return Beamable.API.Instance.FlatMap(beamable =>
      {
         return beamable.Requester.Request<SteamProductsResponse>(
               Beamable.Common.Api.Method.GET,
               $"/basic/payments/steam/products?steamId={steamID}");
      });
   }

   void ISteamService.RegisterTransactionCallback(Action<SteamTransaction> callback)
   {
      if (!SteamManager.Initialized)
      {
         Debug.LogError("Steamworks not initialized.");
         return;
      }

      if (callback != null)
      {
         if (_callbacks == null)
         {
            _callbacks = new List<Action<SteamTransaction>>();
         }

         _callbacks.Add(callback);

         if (!_transactionRegistered)
         {
            Callback<MicroTxnAuthorizationResponse_t>.Create(OnTransactionAuthorized);
            _transactionRegistered = true;
         }
      }
   }

   private void OnTransactionAuthorized(MicroTxnAuthorizationResponse_t data)
   {
      if (_callbacks != null)
      {
         var authorized = Convert.ToBoolean(data.m_bAuthorized);
         var steamTransaction = new SteamTransaction(authorized, data.m_ulOrderID.ToString());
         foreach (var callback in _callbacks)
         {
            callback?.Invoke(steamTransaction);
         }
      }
   }
}
#endif