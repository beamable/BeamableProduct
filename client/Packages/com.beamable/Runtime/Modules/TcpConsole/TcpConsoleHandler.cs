using Beamable.ConsoleCommands;
using System;
using UnityEngine;

namespace Beamable.TcpConsole
{
	public class TcpConsoleHandler
	{
		private TcpMessagesServer _server = new TcpMessagesServer();
		private BeamContext _ctx;
		private BeamableConsole _console;

		public void Init(BeamContext ctx)
		{
			_ctx = ctx;
			_console = _ctx.ServiceProvider.GetService<BeamableConsole>();
			_server.Init();
			_console.OnLog -= HandleConsoleLog;
			_console.OnLog += HandleConsoleLog;

			_server.MessageReceived -= HandleServerMessage;
			_server.MessageReceived += HandleServerMessage;
		}

		public void Disable()
		{
			_server?.Disable();
		}

		public void Update()
		{
			_server?.Update();
		}

		private void HandleServerMessage(string message)
		{
			var parts = message.Split(' ');
			if (parts.Length == 0) return;
			var args = new string[parts.Length - 1];
			for (var i = 1; i < parts.Length; i++) args[i - 1] = parts[i];
			HandleConsoleLog(_console.Execute(parts[0], args));
		}

		private void HandleConsoleLog(string message)
		{
			_server.Broadcast(message.Replace("\n", "\r\n"));
		}
	}
}
