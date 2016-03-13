using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Internetz {
	public static class SocketUtils {
		public static Socket CreateStreamTCP() {
			return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		}

		public static bool TryConnect(this Socket This, IPAddress Addr, int Port) {
			try {
				This.Connect(Addr, Port);
				return true;
			} catch (SocketException) {
			}
			return false;
		}

		public static bool TryConnect(this Socket This, IPAddress Addr, int Port, int RetryCount) {
			if (RetryCount == -1) {
				while (!This.TryConnect(Addr, Port))
					;
				return true;
			} else {
				for (int i = 0; i < RetryCount; i++)
					if (This.TryConnect(Addr, Port))
						return true;
			}
			return false;
		}
	}
}