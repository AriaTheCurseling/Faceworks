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

		internal Span<byte> test => new(_DataPtr, _DataSize);

		internal Span<byte> Data {
			get => new(_DataPtr, _DataSize);
			set {
				_DataHandle = GCHandle.Alloc(@value.GetPinnableReference(), GCHandleType.Pinned);

				_FreeDataPtr = FreeFunctionPointer;
				
				_DataPtr = _DataHandle.AddrOfPinnedObject();
				_DataSize = @value.Length;
			}
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

