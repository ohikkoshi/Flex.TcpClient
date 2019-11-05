using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Flex.Net.Sockets
{
	public class EchoServer : ServerBehaviour
	{
		public EchoServer()
		{
			OnAcceptTcpClient = (container) => {
				#region receive handle

				container.OnReceive = (obj) => {
					var msg = (string)obj;

					foreach (var client in ClientContainer.Connections) {
						client.Send(msg);
					}
				};

				#endregion
				#region stream handle

				container.OnStream = () => {
					var stream = container.Stream;
					var token = container.Token;
					var context = container.Context;

					using (var reader = new StreamReader(stream, Encoding.UTF8)) {
						while (!token.IsCancellationRequested) {
							if (!stream.CanRead) {
								Thread.Yield();
								continue;
							}

							try {
								var str = reader.ReadLine();

								if (!string.IsNullOrEmpty(str)) {
									context?.Post(_ => {
										container.OnReceive?.Invoke(str);
									}, null);
								}
							} catch (Exception e) {
								Error = e.Message;
								Close();
							} finally {
								Thread.Yield();
							}
						}

						reader.Close();
					}
				};

				#endregion

				container.Run();
			};
		}
	}
}
