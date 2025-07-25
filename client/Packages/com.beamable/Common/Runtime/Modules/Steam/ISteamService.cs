// this file was copied from nuget package Beamable.Common@5.1.0
// https://www.nuget.org/packages/Beamable.Common/5.1.0

﻿using System;
using System.Collections.Generic;

namespace Beamable.Common.Steam
{
	public interface ISteamService
	{
		Promise<Unit> RegisterAuthTicket();
		Promise<string> GenerateLoginRequest();
		Promise<SteamProductsResponse> GetProducts();
		void RegisterTransactionCallback(Action<SteamTransaction> callback);
	}

	[Serializable]
	public class SteamTransaction
	{
		public bool authorized;
		public string transactionId;

		public SteamTransaction(bool authorized, string transactionId)
		{
			this.authorized = authorized;
			this.transactionId = transactionId;
		}
	}

	[Serializable]
	public class SteamTicketRequest
	{
		public string ticket;

		public SteamTicketRequest(string ticket)
		{
			this.ticket = ticket;
		}
	}

	[Serializable]
	public class SteamProductsResponse
	{
		public List<SteamProduct> products;
	}

	[Serializable]
	public class SteamProduct
	{
		public string sku;
		public string description;
		public string isoCurrencyCode;
		public string localizedPriceString;
		public double localizedPrice;
	}

	[Serializable]
	public class AuthenticateUserRequest
	{
		public string steamid;
		public string ticket;

		public AuthenticateUserRequest(string steamid, string ticket)
		{
			this.steamid = steamid;
			this.ticket = ticket;
		}
	}
}
