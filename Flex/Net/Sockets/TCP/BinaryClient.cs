using System;
using System.IO;
using System.Threading;

namespace Flex.Net.Sockets
{
	public class BinaryClient : ClientBehaviour
	{
		public BinaryClient() : base()
		{
			#region stream handle

			OnStream = () => {
				using (var ms = new MemoryStream()) {
					var buffer = new byte[128];

					while (!token.IsCancellationRequested) {
						if (!stream.CanRead) {
							Thread.Yield();
							continue;
						}

						try {
							do {
								var length = stream.Read(buffer, 0, buffer.Length);

								if (length > 0) {
									ms.Write(buffer, 0, length);
								} else {
									Close();
									return;
								}
							} while (stream.DataAvailable);

							if (ms.Length > 0) {
								ms.Flush();

								context?.Post(_ => {
									var tmp = ms.GetBuffer();
									OnReceive?.Invoke(tmp);
								}, null);

								ms.Position = 0;
								ms.SetLength(0L);
							}
						} catch (Exception e) {
							Error = e.Message;
							Close();
						} finally {
							Thread.Yield();
						}
					}

					ms.Close();
				}
			};

			#endregion
		}

		public BinaryClient(	int sendBufferSize,
								int sendTimeout,
								int receiveBufferSize,
								int receiveTimeout
								) : base(sendBufferSize, sendTimeout, receiveBufferSize, receiveTimeout)
		{
		}
	}
}
