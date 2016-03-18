using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using System.IO;
using System.Net;
using System.Net.Sockets;
using Internetz.Protocols;

namespace Internetz {
	public class InternetStream : Stream {
		public bool NamedStream;
		public string Name;
		public NetworkPipe Pipe;

		public NetworkStream NetStream;
		public BinaryReader Reader;
		public BinaryWriter Writer;
		public object Userdata;

		public InternetStream(NetworkStream NetStream) {
			this.NetStream = NetStream;
			Reader = new BinaryReader(NetStream);
			Writer = new BinaryWriter(NetStream);
		}

		public InternetStream(Socket S) : this(new NetworkStream(S, true)) {
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

		protected override void Dispose(bool disposing) {
			if (disposing && NamedStream && Name != null && Pipe != null) {
				Pipe.CloseStream(Name);
				if (Reader is IDisposable)
					((IDisposable)Reader).Dispose();
				if (Writer is IDisposable)
					((IDisposable)Writer).Dispose();
			}
			base.Dispose(disposing);
		}

		public override void Close() {
			NetStream.Close();
			Reader.Close();
			Writer.Close();
			base.Close();
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

		public virtual void WriteByteArray(byte[] Bytes) {
			Writer.Write((int)Bytes.Length);
			for (int i = 0; i < Bytes.Length; i++)
				WriteByte(Bytes[i]);
		}

		public virtual byte[] ReadByteArray() {
			int Count = Reader.ReadInt32();
			byte[] Bytes = new byte[Count];
			for (int i = 0; i < Bytes.Length; i++)
				Bytes[i] = (byte)ReadByte();
			return Bytes;
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

		const int CopySize = 4096;

		public void CopyTo(Stream S) {
			byte[] Buffer = new byte[CopySize];
			int Len;
			while ((Len = Read(Buffer, 0, Buffer.Length)) > 0)
				S.Write(Buffer, 0, Len);
		}

		public void CopyFrom(Stream S) {
			byte[] Buffer = new byte[CopySize];
			int Len;
			while ((Len = S.Read(Buffer, 0, Buffer.Length)) > 0)
				Write(Buffer, 0, Len);
		}
	}
}