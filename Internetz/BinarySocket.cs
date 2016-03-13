using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Internetz {
	public class BinarySocket : Socket {
		public BinarySocket(AddressFamily AddrFam, SocketType SType, ProtocolType PType) : base(AddrFam, SType, PType) {
		}

		public BinarySocket(SocketType SType, ProtocolType PType) : this(AddressFamily.InterNetwork, SType, PType) {
		}

		public BinarySocket(ProtocolType PType) :this(SocketType.Stream, PType) {
		}

		public bool TryConnect(IPAddress Addr, int Port) {
			try {
				Connect(Addr, Port);
			} catch (SocketException) {
				return false;
			}
			return true;
		}
	}
}