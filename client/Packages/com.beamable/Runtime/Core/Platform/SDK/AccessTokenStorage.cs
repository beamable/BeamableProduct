using System;
using System.Globalization;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using JetBrains.Annotations;
using UnityEngine;

namespace Beamable.Api
{
   public class AccessTokenStorage
   {
      private string _prefix;

      private const char DeviceTokenDelimiter = ',';
      private const char DeviceTokenSeparator = '|';
      private const string DeviceTokenDelimiterStr = ",";

      private string GetDeviceTokenKey(string cid, [CanBeNull] string pid) => $"{_prefix}device-tokens{cid}{pid ?? ""}";

      public AccessTokenStorage(string prefix = "")
      {
         _prefix = prefix;
      }

      public Promise<AccessToken> LoadTokenForCustomer(string cid)
      {
         string accessToken = PlayerPrefs.GetString($"{_prefix}{cid}.access_token");
         string refreshToken = PlayerPrefs.GetString($"{_prefix}{cid}.refresh_token");
         string expires = PlayerPrefs.GetString($"{_prefix}{cid}.expires");

         if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(expires))
            return Promise<AccessToken>.Successful(null);

         return Promise<AccessToken>.Successful(new AccessToken(this, cid, null, accessToken, refreshToken, expires));
      }

      public Promise<AccessToken> LoadTokenForRealm(string cid, string pid)
      {
         string accessToken = PlayerPrefs.GetString($"{_prefix}{cid}.{pid}.access_token");
         string refreshToken = PlayerPrefs.GetString($"{_prefix}{cid}.{pid}.refresh_token");
         string expires = PlayerPrefs.GetString($"{_prefix}{cid}.{pid}.expires");

         if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(expires))
            return Promise<AccessToken>.Successful(null);

         return Promise<AccessToken>.Successful(new AccessToken(this, cid, pid, accessToken, refreshToken, expires));
      }

      public Promise<Unit> SaveTokenForCustomer(string cid, AccessToken token)
      {
         PlayerPrefs.SetString($"{_prefix}{cid}.access_token", token.Token);
         PlayerPrefs.SetString($"{_prefix}{cid}.refresh_token", token.RefreshToken);
         PlayerPrefs.SetString(
            $"{_prefix}{cid}.expires",
            token.ExpiresAt.ToString("O", CultureInfo.InvariantCulture)
         );
         StoreDeviceRefreshToken(cid, null, token);
         PlayerPrefs.Save();
         return Promise<Unit>.Successful(PromiseBase.Unit);
      }

      public Promise<Unit> SaveTokenForRealm(string cid, string pid, AccessToken token)
      {
         PlayerPrefs.SetString($"{_prefix}{cid}.{pid}.access_token", token.Token);
         PlayerPrefs.SetString($"{_prefix}{cid}.{pid}.refresh_token", token.RefreshToken);
         PlayerPrefs.SetString(
            $"{_prefix}{cid}.{pid}.expires",
            token.ExpiresAt.ToString("O", CultureInfo.InvariantCulture)
         );
         StoreDeviceRefreshToken(cid, pid, token);
         PlayerPrefs.Save();
         return Promise<Unit>.Successful(PromiseBase.Unit);
      }

      public Promise<Unit> DeleteTokenForCustomer(string cid)
      {
         PlayerPrefs.DeleteKey($"{_prefix}{cid}.access_token");
         PlayerPrefs.DeleteKey($"{_prefix}{cid}.refresh_token");
         PlayerPrefs.DeleteKey($"{_prefix}{cid}.expires");
         PlayerPrefs.Save();
         return Promise<Unit>.Successful(PromiseBase.Unit);
      }

      public Promise<Unit> DeleteTokenForRealm(string cid, string pid)
      {
         PlayerPrefs.DeleteKey($"{_prefix}{cid}.{pid}.access_token");
         PlayerPrefs.DeleteKey($"{_prefix}{cid}.{pid}.refresh_token");
         PlayerPrefs.DeleteKey($"{_prefix}{cid}.{pid}.expires");
         PlayerPrefs.Save();
         return Promise<Unit>.Successful(PromiseBase.Unit);
      }

      public void StoreDeviceRefreshToken(string cid, string pid, AccessToken token)
      {
         string key = GetDeviceTokenKey(cid, pid);
         var compressedTokens = PlayerPrefs.GetString(key, "");
         PlayerPrefs.SetString(key, NextCompressedTokens(compressedTokens, token));
      }

      private string NextCompressedTokens(string compressedTokens, AccessToken token)
      {
         // this should overwrite any existing account that shares the same refresh token, so that the latest access token is kept up to date.
         var codedToken = Convert(token);
         if (string.IsNullOrEmpty(compressedTokens))
         {
            return codedToken;
         }

         var set = compressedTokens.Split(Constants.DelimiterSplit, StringSplitOptions.RemoveEmptyEntries);
         if (set.Length == 0)
         {
            return codedToken;
         }

         for (int i = set.Length - 1; i >= 0; --i)
         {
            if (MatchesRefreshToken(set[i], token.RefreshToken))
            {
               set[i] = codedToken;
               var nextCompressedTokens = string.Join(DeviceTokenDelimiterStr, set);
               return nextCompressedTokens;
            }
         }

         return $"{compressedTokens}{DeviceTokenDelimiter}{codedToken}";
      }

      public void RemoveDeviceRefreshToken(string cid, string pid, TokenResponse token)
      {
         string key = GetDeviceTokenKey(cid, pid);
         var compressedTokens = PlayerPrefs.GetString(key, "");
         var set = compressedTokens.Split(Constants.DelimiterSplit, StringSplitOptions.RemoveEmptyEntries);
         set = Array.FindAll(set, curr => !MatchesRefreshToken(curr, token.refresh_token));
         var nextCompressedTokens = string.Join(DeviceTokenDelimiterStr, set);

         PlayerPrefs.SetString(key, nextCompressedTokens);
      }

      public void ClearDeviceRefreshTokens(string cid, string pid)
      {
         PlayerPrefs.DeleteKey(GetDeviceTokenKey(cid, pid));
      }

      public TokenResponse[] RetrieveDeviceRefreshTokens(string cid, string pid)
      {
         var compressedTokens = PlayerPrefs.GetString(GetDeviceTokenKey(cid, pid), "");
         var refreshTokens = compressedTokens.Split(Constants.DelimiterSplit, StringSplitOptions.RemoveEmptyEntries);
         return Array.ConvertAll(refreshTokens, Convert);
      }

      private string Convert(AccessToken token)
      {
         return $"{token.Token}{DeviceTokenSeparator}{token.RefreshToken}";
      }

      private static bool MatchesRefreshToken(string encoded, string refreshToken)
      {
         return encoded.EndsWith(refreshToken, StringComparison.Ordinal) &&
                encoded.Length > refreshToken.Length &&
                encoded[encoded.Length - refreshToken.Length - 1] == DeviceTokenSeparator;
      }

      private TokenResponse Convert(string token)
      {
         var parts = token.Split(Constants.SeparatorSplit, StringSplitOptions.None);
         return new TokenResponse
         {
            access_token = parts[0],
            refresh_token = parts.Length == 2 ? parts[1] : ""
         };
      }

      private static class Constants
      {
         public static readonly char[] SeparatorSplit = new[] {DeviceTokenSeparator};
         public static readonly char[] DelimiterSplit = new[] {DeviceTokenDelimiter};
      }

   }
}