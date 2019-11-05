using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Flex.Net.Sockets
{
	public class AsciiClient : ClientBehaviour
	{
		public AsciiClient() : base()
		{
			#region stream handle

			OnStream = () => {
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
									OnReceive?.Invoke(str);
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
		}

		public AsciiClient(	int sendBufferSize,
							int sendTimeout,
							int receiveBufferSize,
							int receiveTimeout
							) : base(sendBufferSize, sendTimeout, receiveBufferSize, receiveTimeout)
		{
		}
	}
}
