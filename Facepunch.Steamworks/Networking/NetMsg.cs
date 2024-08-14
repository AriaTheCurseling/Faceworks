using System;
using System.Runtime.InteropServices;

namespace Steamworks.Data
{
	[StructLayout( LayoutKind.Sequential )]
	internal unsafe partial struct NetMsg
	{
		private byte* _DataPtr;
		private int _DataSize;
		internal Connection Connection;
		internal readonly NetIdentity Identity;
		internal long ConnectionUserData;
		internal long RecvTime;
		internal long MessageNumber;
		private IntPtr _FreeDataPtr;
		private readonly IntPtr _ReleasePtr;
		internal int Channel;
		internal SendType Flags;
		private GCHandle64 _DataHandle;
		internal ushort IdxLane;
		private ushort _pad1__;

		internal static Span<byte> GetData(NetMsg* msg) => new(msg->_DataPtr, msg->_DataSize);
		internal static void SetData(NetMsg* msg, Span<byte> data) {
			msg->_DataHandle = GCHandle.Alloc(data.GetPinnableReference(), GCHandleType.Pinned);

			msg->_FreeDataPtr = FreeFunctionPointer;
			
			msg->_DataPtr = msg->_DataHandle.AddrOfPinnedObject();
			msg->_DataSize = data.Length;
		}

		#region Free Data
		[UnmanagedFunctionPointer( CallingConvention.Cdecl )]
		private delegate void FreeDataDelegate( NetMsg* msg );
		
		private static readonly FreeDataDelegate FreeFunctionPin = new( Free );

		private static readonly IntPtr FreeFunctionPointer = Marshal.GetFunctionPointerForDelegate( FreeFunctionPin );

		[MonoPInvokeCallback]
		private static void Free( NetMsg* msg ) => msg->_DataHandle.Free();
		#endregion
	}

	[StructLayout( LayoutKind.Sequential, Size = 4 )]
	internal struct GCHandle64 {
		public GCHandle value;

		public GCHandle64( GCHandle handle ) : this() => value = handle;

		public void Free() => value.Free();
		public unsafe byte* AddrOfPinnedObject() => (byte*) value.AddrOfPinnedObject();

		public static implicit operator GCHandle(GCHandle64 handle) => handle.value;
		public static implicit operator GCHandle64(GCHandle handle) => new(handle);
	}
}

