using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Internetz;

namespace Internetz.Protocols {
	public delegate void OnNamedData(string Name, byte[] Data);
	public delegate void OnInternetStream(string Name, InternetStream Stream);

	enum PacketType : int {
		DATA, STREAM, REQUEST_STREAM
	}

	class PipePacket {
		public PacketType Type;
		public string Name;
		public byte[] Data;

		public PipePacket(PacketType Type) {
			this.Type = Type;
			Data = new byte[] { };
		}

		public void ToInternetStream(InternetStream S) {
			S.Writer.Write((int)Type);
			S.Writer.Write(Name);
			S.WriteByteArray(Data);
			S.Flush();
		}

		public static PipePacket FromInternetStream(InternetStream S) {
			PipePacket Packet = new PipePacket((PacketType)S.Reader.ReadInt32());
			Packet.Name = S.Reader.ReadString();
			Packet.Data = S.ReadByteArray();
			return Packet;
		}
	}

	public class NetworkPipe {
		public event OnInternetStream OnStreamCreated;
		public Socket Socket;

		InternetStream Stream;
		Queue<PipePacket> DataPackets;
		Dictionary<string, InternetStream> DataStreams;

		public NetworkPipe(Socket S) {
			Socket = S;
			Stream = new InternetStream(S);
			DataPackets = new Queue<PipePacket>();
			DataStreams = new Dictionary<string, InternetStream>();
		}

		public void Send(string Name, byte[] Data) {
			PipePacket P = new PipePacket(PacketType.DATA);
			P.Name = Name;
			P.Data = Data;
			P.ToInternetStream(Stream);
		}

		public string Read(out byte[] Data) {
			while (DataPackets.Count == 0)
				;
			PipePacket P = DataPackets.Dequeue();
			Data = P.Data;
			return P.Name;
		}

		public InternetStream CreateStream(string Name, int Port = 0) {
			Socket ServerSock = SocketUtils.CreateStreamTCP();
			ServerSock.Bind(new IPEndPoint(IPAddress.Any, Port));
			ServerSock.Listen(0);

			PipePacket Pack = new PipePacket(PacketType.STREAM);
			Pack.Name = Name;
			Pack.Data = BitConverter.GetBytes((int)((IPEndPoint)ServerSock.LocalEndPoint).Port);
			Pack.ToInternetStream(Stream);

			InternetStream S = new InternetStream(ServerSock.Accept());
			S.Userdata = ServerSock;
			DataStreams.Add(Name, S);

			if (OnStreamCreated != null)
				OnStreamCreated(Name, S);
			return S;
		}

		public void RequestStream(string Name) {
			PipePacket Pack = new PipePacket(PacketType.REQUEST_STREAM);
			Pack.Name = Name;
			Pack.ToInternetStream(Stream);
		}

		public InternetStream GetStream(string Name) {
			while (!DataStreams.ContainsKey(Name))
				;
			return DataStreams[Name];
		}

		public void Run() {
			while (Socket.Connected) {
				PipePacket P = PipePacket.FromInternetStream(Stream);
				if (P.Type == PacketType.DATA) {
					DataPackets.Enqueue(P);
				} else if (P.Type == PacketType.STREAM) {
					Socket DataSocket = SocketUtils.CreateStreamTCP();
					int Port = BitConverter.ToInt32(P.Data, 0);
					DataSocket.Connect(((IPEndPoint)Socket.RemoteEndPoint).Address, Port);
					InternetStream S = new InternetStream(DataSocket);
					DataStreams.Add(P.Name, S);

					if (OnStreamCreated != null)
						OnStreamCreated(P.Name, S);
				} else if (P.Type == PacketType.REQUEST_STREAM) {
					CreateStream(P.Name);
				}
			}
		}

		public void StartRun() {
			Thread T = new Thread(() => Run());
			T.IsBackground = true;
			T.Start();
		}
	}
}