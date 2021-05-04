using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Flex.Net.Sockets
{
	public class ServerBehaviour
	{
		// Properties
		public bool Reachability { get; }
		public string HostName { get; }
		public string ClientName { get; }
		public int Port { get; private set; }
		public string Error { get; protected set; }

		// Handler
		public Action<string> OnError;
		public Action<ClientContainer> OnAcceptTcpClient;

		// TcpListener
		TcpListener listener;
		CancellationTokenSource token;


		public ServerBehaviour()
		{
			try {
				var address = NIC.IPv4(NetworkInterfaceType.Wireless80211);

				if (string.IsNullOrEmpty(address)) {
					address = NIC.IPv4(NetworkInterfaceType.Ethernet);
				}

				if (string.IsNullOrEmpty(address)) {
					address = NIC.IPv4();
				}

				if (!string.IsNullOrEmpty(address)) {
					var array = address.Split('.');
					HostName = $"{array[0]}.{array[1]}.{array[2]}.255";
					ClientName = address;
					Reachability = true;
				}
			} catch (Exception e) {
				Close(e.Message);
			}
		}

		~ServerBehaviour()
		{
			Close();
		}

		public void Close(string msg = null)
		{
			Error = msg;

			if (!string.IsNullOrEmpty(msg)) {
				Log.d($"<color=#00ff00>[Server]:{Error}.</color>");
				OnError?.Invoke(msg);
			}

			ClientContainer.Dispose();

			if (token != null) {
				token.Cancel();
				token = null;
			}

			if (listener != null) {
				listener.Stop();
				listener = null;
				Log.d($"<color=#00ff00>[Server]:Shutdown().</color>");
			}
		}

		public void Start(int port)
		{
			Debug.Assert(0 <= port && port <= 65535);
			Port = port;

			try {
				listener?.Stop();
				listener = new TcpListener(IPAddress.Any, Port);
				listener.Start();
			} catch (Exception e) {
				Close(e.Message);
				return;
			}

			Log.d($"<color=#00ff00>[Server]:Start({ClientName}:{Port}).</color>");

			token?.Cancel();
			token = new CancellationTokenSource();

			var context = SynchronizationContext.Current;

			Task.Run(() => {
				while (!token.IsCancellationRequested) {
					try {
						var client = listener.AcceptTcpClient();
						var container = ClientContainer.Create(client, context);
						OnAcceptTcpClient?.Invoke(container);
					} catch (Exception e) {
						if (listener != null) {
							Close(e.Message);
						}
					}

					Thread.Yield();
				}
			}, token.Token);
		}
	}
}
