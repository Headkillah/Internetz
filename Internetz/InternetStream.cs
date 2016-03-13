using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Internetz {
	public class InternetStream : Stream {
		public Socket Socket;
		public NetworkStream NetStream;
		public BinaryReader Reader;
		public BinaryWriter Writer;


		public InternetStream(Socket S) {
			Socket = S;
			NetStream = new NetworkStream(S);
			Reader = new BinaryReader(NetStream);
			Writer = new BinaryWriter(NetStream);
		}

		public override bool CanRead
		{
			get
			{
				return NetStream.CanRead;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return NetStream.CanSeek;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return NetStream.CanWrite;
			}
		}

		public override long Length
		{
			get
			{
				return NetStream.Length;
			}
		}

		public override long Position
		{
			get
			{
				return NetStream.Position;
			}

			set
			{
				NetStream.Position = value;
			}
		}

		public override void Flush() {
			NetStream.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count) {
			return NetStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin) {
			return NetStream.Seek(offset, origin);
		}

		public override void SetLength(long value) {
			NetStream.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count) {
			NetStream.Write(buffer, offset, count);
		}

		public virtual void WriteStruct<T>(T Val) where T : struct {
			int Size = Marshal.SizeOf(typeof(T));
			byte[] Bytes = new byte[Size];
			IntPtr Ptr = Marshal.AllocHGlobal(Size);
			Marshal.StructureToPtr(Val, Ptr, true);
			Marshal.Copy(Ptr, Bytes, 0, Bytes.Length);
			Marshal.FreeHGlobal(Ptr);

			for (int i = 0; i < Bytes.Length; i++)
				WriteByte(Bytes[i]);
		}

		public virtual T ReadStruct<T>() where T : struct {
			int Size = Marshal.SizeOf(typeof(T));
			byte[] Bytes = new byte[Size];

			for (int i = 0; i < Bytes.Length; i++)
				Bytes[i] = (byte)ReadByte();

			IntPtr Ptr = Marshal.AllocHGlobal(Size);
			Marshal.Copy(Bytes, 0, Ptr, Bytes.Length);
			T Ret = (T)Marshal.PtrToStructure(Ptr, typeof(T));
			Marshal.FreeHGlobal(Ptr);
			return Ret;
		}
	}
}