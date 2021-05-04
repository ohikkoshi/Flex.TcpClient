using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Flex.Net.Sockets
{
	public class ClientContainer : ClientBehaviour
	{
		#region container

		// Properties
		public static List<ClientContainer> Connections => connections;
		public static int Count => connections.Count;
		public NetworkStream Stream => stream;
		public SynchronizationContext Context => context;
		public CancellationTokenSource Token => token;

		// Container List
		static List<ClientContainer> connections = new List<ClientContainer>();


		public static ClientContainer Create(TcpClient client, SynchronizationContext context)
		{
			return new ClientContainer(client, context);
		}

		public static void Dispose()
		{
			if (connections.Count > 0) {
				foreach (var con in connections) {
					if (con != null) {
						con.Close();
					}
				}

				lock (connections) {
					connections.Clear();
				}
			}
		}

		#endregion

		#region behaviour

		ClientContainer()
		{
		}

		ClientContainer(TcpClient client, SynchronizationContext context)
		{
			// Constructor
			Reachability = true;

			// Connect
			var ep = (IPEndPoint)client.Client.RemoteEndPoint;
			this.HostName = ep.Address.ToString();
			this.Port = ep.Port;
			this.client = client;
			this.context = context;

			// OnConnect
			this.stream = client.GetStream();
			this.token?.Cancel();
			this.token = new CancellationTokenSource();

			if (!connections.Contains(this)) {
				lock (connections) {
					connections.Add(this);
				}

				Log.d($"<color=#ffff00>[Server]:Join({HostName}:{Port}).</color>");
			}
		}

		public void Close()
		{
			base.Close();

			if (connections.Contains(this)) {
				lock (connections) {
					connections.Remove(this);
				}

				Log.d($"<color=#ffff00>[Server]:Exit({HostName}:{Port}).</color>");
			}
		}

		public void Run()
		{
			if (OnStream != null) {
				Task.Run(OnStream, token.Token);
			}
		}

		#endregion
	}
}
