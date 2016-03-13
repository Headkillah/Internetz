using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net.Sockets;
using System.Net;

using Internetz;
using Internetz.Protocols;

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
			
			Console.ReadLine();
		}

		static void Server() {
			Socket S = SocketUtils.CreateStreamTCP();
			S.Bind(new IPEndPoint(IPAddress.Any, Port));
			S.Listen(0);

			NetworkPipe PipeToClient = new NetworkPipe(S.Accept());
			PipeToClient.StartRun();

			PipeToClient.OnStreamCreated += (Name, Stream) => {
				Console.WriteLine("{0}: {1}", Name, Stream.Reader.ReadString());
			};

			Thread.Sleep(200);
			PipeToClient.CreateStream("Test2");
		}

		static void Client() {
			Socket C = SocketUtils.CreateStreamTCP();
			C.TryConnect(IPAddress.Loopback, Port);

			NetworkPipe PipeToServer = new NetworkPipe(C);
			PipeToServer.StartRun();

			PipeToServer.OnStreamCreated += (Name, Stream) => {
				Stream.Writer.Write("Hello " + Name);
			};

			PipeToServer.RequestStream("Test");
		}
	}
}