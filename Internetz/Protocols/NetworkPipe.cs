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
		NULL, DATA, STREAM, REQUEST_STREAM
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
			try {
				PipePacket Packet = new PipePacket((PacketType)S.Reader.ReadInt32());
				Packet.Name = S.Reader.ReadString();
				Packet.Data = S.ReadByteArray();
				return Packet;
			} catch (IOException) {
			}
			return new PipePacket(PacketType.NULL);
		}
	}

	public class NetworkPipe : IDisposable {
		public event OnInternetStream OnStreamCreated;
		public Socket Socket;
		public EndPoint RemoteEndPoint;

		InternetStream Stream;
		Queue<PipePacket> DataPackets;
		Dictionary<string, InternetStream> DataStreams;
		Thread RunThread;
		bool Disposed = false;

		int PortMin, PortRange, LastPort;

		public NetworkPipe(Socket S, int PortMin = 10000, int PortRange = 10000) {
			this.PortMin = PortMin;
			this.PortRange = PortRange;
			Socket = S;
			RemoteEndPoint = S.RemoteEndPoint;
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
			if (DataStreams.ContainsKey(Name))
				CloseStream(Name);

			LastPort = (LastPort + 1) % PortRange;
			Port = LastPort + PortMin;

			Socket ServerSock = SocketUtils.CreateStreamTCP();
			ServerSock.Bind(new IPEndPoint(IPAddress.Any, Port));
			ServerSock.Listen(0);

			PipePacket Pack = new PipePacket(PacketType.STREAM);
			Pack.Name = Name;
			Pack.Data = BitConverter.GetBytes((int)((IPEndPoint)ServerSock.LocalEndPoint).Port);
			Pack.ToInternetStream(Stream);

			InternetStream S = new InternetStream(ServerSock.Accept());
			S.NamedStream = true;
			S.Name = Name;
			S.Pipe = this;
			S.Userdata = ServerSock;
			DataStreams.Add(Name, S);

			if (OnStreamCreated != null)
				OnStreamCreated(Name, S);
			return S;
		}

		public InternetStream RequestStream(string Name) {
			if (DataStreams.ContainsKey(Name))
				return DataStreams[Name];

			PipePacket Pack = new PipePacket(PacketType.REQUEST_STREAM);
			Pack.Name = Name;
			Pack.ToInternetStream(Stream);
			return GetStream(Name);
		}

		public InternetStream GetStream(string Name) {
			while (!DataStreams.ContainsKey(Name))
				;
			return DataStreams[Name];
		}

		public InternetStream[] GetStreams() {
			List<InternetStream> Streams = new List<InternetStream>();
			foreach (var KV in DataStreams)
				Streams.Add(KV.Value);
			return Streams.ToArray();
		}

		public void CloseStream(string Name) {
			if (DataStreams.ContainsKey(Name)) {
				InternetStream S = DataStreams[Name];
				S.Flush();

				DataStreams.Remove(Name);
				S.Close();
				if (S.Userdata != null && S.Userdata is Socket) {
					Socket ServerSock = (Socket)S.Userdata;
					if (ServerSock.Connected)
						ServerSock.Disconnect(false);
				}
			}
		}

		public void CloseStream(InternetStream S) {
			string Name = null;

			if (DataStreams.ContainsValue(S))
				foreach (var KV in DataStreams)
					if (KV.Value == S) {
						Name = KV.Key;
						break;
					}

			if (Name != null)
				CloseStream(Name);
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
					S.NamedStream = true;
					S.Name = P.Name;
					S.Pipe = this;

					if (DataStreams.ContainsKey(P.Name))
						CloseStream(P.Name);
					DataStreams.Add(P.Name, S);

					if (OnStreamCreated != null)
						OnStreamCreated(P.Name, S);
				} else if (P.Type == PacketType.REQUEST_STREAM) {
					CreateStream(P.Name);
				}
			}
		}

		public void StartRun() {
			RunThread = new Thread(() => Run());
			RunThread.IsBackground = true;
			RunThread.Start();
		}

		public void Dispose() {
			if (Disposed)
				return;
			Disposed = true;

			if (RunThread.ThreadState == ThreadState.Running)
				RunThread.Abort();
			Stream.Dispose();
		}
	}
}