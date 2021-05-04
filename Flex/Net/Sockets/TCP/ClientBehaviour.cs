using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Flex.Net.Sockets
{
	public class ClientBehaviour
	{
		// Properties
		public bool Reachability { get; protected set; }
		public string HostName { get; protected set; }
		public int Port { get; protected set; }
		public string Error { get; protected set; }

		// Initializer
		public int SendBufferSize { get; private set; } = 1024;
		public int SendTimeout { get; private set; } = 0;
		public int ReceiveBufferSize { get; private set; } = 4096;
		public int ReceiveTimeout { get; private set; } = 0;

		// Handler
		public Action<string> OnError;
		public Action OnStream;
		public Action<Object> OnReceive;

		// TcpClient
		protected TcpClient client;
		protected NetworkStream stream;
		protected SynchronizationContext context;
		protected CancellationTokenSource token;


		public ClientBehaviour()
		{
		}

		public ClientBehaviour(int sendBufferSize, int sendTimeout, int receiveBufferSize, int receiveTimeout) : this()
		{
			Debug.Assert(sendBufferSize > 0);
			SendBufferSize = sendBufferSize;

			Debug.Assert(sendTimeout >= 0);
			SendTimeout = sendTimeout;

			Debug.Assert(receiveBufferSize > 0);
			ReceiveBufferSize = receiveBufferSize;

			Debug.Assert(receiveTimeout >= 0);
			ReceiveTimeout = receiveTimeout;
		}

		~ClientBehaviour()
		{
			Close();
		}

		public void Close(string msg = null)
		{
			Error = msg;

			if (!string.IsNullOrEmpty(msg)) {
				Log.d(msg);
				OnError?.Invoke(msg);
			}

			if (token != null) {
				token.Cancel();
				token = null;
			}

			if (stream != null) {
				stream.Close();
				stream = null;
			}

			if (client != null) {
				client.Close();
				client = null;
			}
		}

		public void Connect(string hostName, int port, bool bindSelf = true, bool noDelay = true)
		{
			Debug.Assert(!string.IsNullOrEmpty(hostName));
			Debug.Assert(0 <= port && port <= 65535);

			HostName = hostName;
			Port = port;
			context = SynchronizationContext.Current;

			Close();

			try {
				client = new TcpClient();
				client.Client.SendBufferSize = SendBufferSize;
				client.Client.SendTimeout = SendTimeout;
				client.Client.ReceiveBufferSize = ReceiveBufferSize;
				client.Client.ReceiveTimeout = ReceiveTimeout;
				client.Client.ExclusiveAddressUse = !bindSelf;
				client.Client.NoDelay = noDelay;
				client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
				client.BeginConnect(HostName, Port, OnConnect, client);
			} catch (Exception e) {
				Close(e.Message);
			}
		}

		void OnConnect(IAsyncResult result)
		{
			var tcp = (TcpClient)result.AsyncState;

			try {
				tcp.EndConnect(result);
			} catch (Exception e) {
				Close(e.Message);
				return;
			}

			token?.Cancel();
			token = new CancellationTokenSource();

			stream = tcp.GetStream();

			if (OnStream != null) {
				Task.Run(OnStream, token.Token);
			}
		}

		public void Send(string str)
		{
			if (string.IsNullOrEmpty(str)) {
				return;
			}

			var data = Encoding.UTF8.GetBytes($"{str}{Environment.NewLine}");
			Send(data);
		}

		public void Send(byte[] data)
		{
			if (data == null || data.Length == 0) {
				return;
			}

			if (stream == null || !stream.CanWrite) {
				return;
			}

			try {
				stream.WriteAsync(data, 0, data.Length, token.Token);
			} catch (Exception e) {
				Close(e.Message);
			}
		}
	}
}
