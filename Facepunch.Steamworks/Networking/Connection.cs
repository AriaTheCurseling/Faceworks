﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Steamworks.Data
{

	/// <summary>
	/// Used as a base to create your client connection. This creates a socket
	/// to a single connection.
	/// 
	/// You can override all the virtual functions to turn it into what you
	/// want it to do.
	/// </summary>
	public struct Connection : IEquatable<Connection>
	{
		public uint Id { get; set; }

		public bool Equals( Connection other ) => Id == other.Id;
		public override bool Equals( object obj ) => obj is Connection other && Id == other.Id;
		public override int GetHashCode() => Id.GetHashCode();
		public override string ToString() => Id.ToString();
		public static implicit operator Connection( uint value ) => new Connection() { Id = value };
		public static implicit operator uint( Connection value ) => value.Id;
		public static bool operator ==( Connection value1, Connection value2 ) => value1.Equals( value2 );
		public static bool operator !=( Connection value1, Connection value2 ) => !value1.Equals( value2 );

		/// <summary>
		/// Accept an incoming connection that has been received on a listen socket.
		/// </summary>
		public Result Accept()
		{
			return SteamNetworkingSockets.Internal.AcceptConnection( this );
		}

		/// <summary>
		/// Disconnects from the remote host and invalidates the connection handle. Any unread data on the connection is discarded..
		/// reasonCode is defined and used by you.
		/// </summary>
		public bool Close( bool linger = false, int reasonCode = 0, string debugString = "Closing Connection" )
		{
			return SteamNetworkingSockets.Internal.CloseConnection( this, reasonCode, debugString, linger );
		}

		/// <summary>
		/// Get/Set connection user data
		/// </summary>
		public long UserData
		{
			get => SteamNetworkingSockets.Internal.GetConnectionUserData( this );
			set => SteamNetworkingSockets.Internal.SetConnectionUserData( this, value );
		}

		/// <summary>
		/// A name for the connection, used mostly for debugging
		/// </summary>
		public string ConnectionName
		{
			get
			{
				if ( !SteamNetworkingSockets.Internal.GetConnectionName( this, out var strVal ) )
					return "ERROR";

				return strVal;
			}

			set => SteamNetworkingSockets.Internal.SetConnectionName( this, value );
		}

		/// <summary>
		/// This is the best version to use.
		/// </summary>
		public unsafe Result SendMessage( Span<byte> data, SendType sendType = SendType.Reliable, ushort laneIndex = 0 )
		{
			if ( data.Length == 0 )
				throw new ArgumentException( "`size` cannot be zero", nameof( data.Length ) );

			var message = SteamNetworkingUtils.AllocateMessage();

			message->Connection = this;
			message->Flags = sendType;
			message->IdxLane = laneIndex;
			message->Data = data;

			long messageNumber = 0;
			SteamNetworkingSockets.Internal.SendMessages( 1, &message, &messageNumber );

			return messageNumber >= 0
				? Result.OK
				: (Result)(-messageNumber);
		}

		/// <summary>
		/// This creates a ton of garbage - so don't do anything with this beyond testing!
		/// </summary>
		public unsafe Result SendMessage( string str, SendType sendType = SendType.Reliable, ushort laneIndex = 0 )
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes( str );
			return SendMessage( bytes, sendType, laneIndex );
		}

		/// <summary>
		/// Flush any messages waiting on the Nagle timer and send them at the next transmission 
		/// opportunity (often that means right now).
		/// </summary>
		public Result Flush() => SteamNetworkingSockets.Internal.FlushMessagesOnConnection( this );

		/// <summary>
		/// Returns detailed connection stats in text format.  Useful
		/// for dumping to a log, etc.
		/// </summary>
		/// <returns>Plain text connection info</returns>
		public string DetailedStatus()
		{
			if ( SteamNetworkingSockets.Internal.GetDetailedConnectionStatus( this, out var strVal ) != 0 )
				return null;

			return strVal;
		}

		/// <summary>
		/// Returns a small set of information about the real-time state of the connection.
		/// </summary>
		public ConnectionStatus QuickStatus()
		{
			ConnectionStatus connectionStatus = default( ConnectionStatus );

			SteamNetworkingSockets.Internal.GetConnectionRealTimeStatus( this, ref connectionStatus, 0, null );

			return connectionStatus;
		}

		/// <summary>
		/// Configure multiple outbound messages streams ("lanes") on a connection, and
		/// control head-of-line blocking between them.
		/// </summary>
		public Result ConfigureConnectionLanes( int[] lanePriorities, ushort[] laneWeights )
		{
			return SteamNetworkingSockets.Internal.ConfigureConnectionLanes( this, lanePriorities.Length, lanePriorities, laneWeights );
		}
	}
}
