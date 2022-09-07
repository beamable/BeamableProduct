using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;

namespace Beamable.TcpConsole
{
	public class TcpMessagesServer
	{
		public const int PORT = 8080;
		public event Action NewUserConnected;
		public event Action<string> StartInfoGenerated;
		public event Action<string> MessageReceived;
		private TcpListener server;
		private List<TcpClient> clients = new List<TcpClient>();
		private List<TcpClient> disconnectedClients = new List<TcpClient>();
		private bool running;

		[Conditional("UNITY_STANDALONE"), Conditional("UNITY_IOS"), Conditional("UNITY_ANDROID")]
		public void Init()
		{
			try
			{
				var ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
				server = new TcpListener(ipAddress, PORT);
				server.Start();
				StartListening();

				{
					var message =
						$"Server started, ip: '{IPAddress.Parse(((IPEndPoint)server.LocalEndpoint).Address.ToString())}', port: {((IPEndPoint)server.LocalEndpoint).Port}";
					StartInfoGenerated?.Invoke(message);
				}

				running = true;
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogError($"Socket error: {e.Message}");
			}
		}

		public void Disable()
		{
			server.Stop();
		}

		public void Update()
		{
			if (!running)
				return;

			foreach (var client in clients)
			{
				if (!IsConnected(client))
				{
					client.Close();
					disconnectedClients.Add(client);
					continue;
				}

				NetworkStream s = client.GetStream();
				if (s.DataAvailable)
				{
					var reader = new StreamReader(s, true);
					var data = reader.ReadLine();

					if (!string.IsNullOrEmpty(data))
					{
						OnIncomingData(data);
					}
				}
			}
		}

		private void OnIncomingData(string data)
		{
			var strippedString = new string(
				data.Where(c => c <= sbyte.MaxValue).ToArray()
			);
			UnityMainThreadDispatcher.Instance().Enqueue(() => MessageReceived?.Invoke(strippedString));
		}

		[Conditional("UNITY_STANDALONE"), Conditional("UNITY_IOS"), Conditional("UNITY_ANDROID")]
		public void Broadcast(string data)
		{
			foreach (var client in clients)
			{
				try
				{
					var writer = new StreamWriter(client.GetStream());
					writer.Write($"{data}\r\n");
					writer.Flush();
				}
				catch (Exception e)
				{
					UnityEngine.Debug.LogException(e);
				}
			}
		}

		private void StartListening()
		{
			server.BeginAcceptTcpClient(HandleAcceptClient, server);
		}

		private void HandleAcceptClient(IAsyncResult ar)
		{
			var listener = (TcpListener)ar.AsyncState;
			clients.Add(listener.EndAcceptTcpClient(ar));
			StartListening();
			UnityEngine.Debug.Log("User connected!");
			Broadcast("User connected");

			UnityMainThreadDispatcher.Instance().Enqueue(() => NewUserConnected?.Invoke());
		}

		private bool IsConnected(TcpClient c)
		{
			try
			{
				if (c != null && c.Client != null && c.Client.Connected)
				{
					if (c.Client.Poll(0, SelectMode.SelectRead))
						return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
					else
					{
						return true;
					}
				}
			}
			catch
			{
				return false;
			}

			return false;
		}
	}
}
