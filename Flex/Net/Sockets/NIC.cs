using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace Flex.Net.Sockets
{
	public class NIC
	{
		public static string IPv4()
		{
			try {
				var hostName = Dns.GetHostName();
				var addresses = Dns.GetHostAddresses(hostName);

				foreach (var address in addresses) {
					if (address.AddressFamily == AddressFamily.InterNetwork) {
						return address.ToString();
					}
				}
			} catch (System.Exception) {
				throw;
			}

			return null;
		}

		public static string IPv4(NetworkInterfaceType networkInterfaceType)
		{
			try {
				var array = NetworkInterface.GetAllNetworkInterfaces();

				foreach (var nic in array) {
					if (nic.NetworkInterfaceType == networkInterfaceType) {
						var props = nic.GetIPProperties();

						foreach (var ip in props.UnicastAddresses) {
							if (ip.Address.AddressFamily == AddressFamily.InterNetwork) {
								return ip.Address.ToString();
							}
						}
					}
				}
			} catch (System.Exception) {
				throw;
			}

			return null;
		}
	}
}
