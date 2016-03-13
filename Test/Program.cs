using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;

using Internetz;

namespace Test {
	class Program {
		static int Port = 9999;

		static void Main(string[] args) {
			Console.Title = "Internetz Test";

			Thread ServerThread = new Thread(() => Server());
			Thread ClientThread = new Thread(() => Client());
			ServerThread.Start();
			ClientThread.Start();

			while (ServerThread.ThreadState == ThreadState.Running || ClientThread.ThreadState == ThreadState.Running)
				Thread.Sleep(50);

			Console.WriteLine("Done!");
			Console.ReadLine();
		}

		static void Server() {
			Socket S = SocketUtils.CreateStreamTCP();
			S.Bind(new IPEndPoint(IPAddress.Any, Port));
			S.Listen(0);

			InternetStream Stream = new InternetStream(S.Accept());
			Stream.Writer.Write("Hello World!");
		}

		static void Client() {
			Socket C = SocketUtils.CreateStreamTCP();
			if (C.TryConnect(IPAddress.Loopback, Port)) {
				InternetStream Stream = new InternetStream(C);

				Console.WriteLine(Stream.Reader.ReadString());

				while (C.Connected)
					;
			}
		}
	}
}