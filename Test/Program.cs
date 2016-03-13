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
			BinarySocket S = new BinarySocket(ProtocolType.Tcp);
		}

		static void Client() {
			BinarySocket C = new BinarySocket(ProtocolType.Tcp);
		}
	}
}
