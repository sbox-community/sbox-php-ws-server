using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sandbox;

public partial class WSClient
{
	public WebSocket? WS;
	public Settings settings;
	private CancellationTokenSource CTS;
	private Dictionary<string, TaskCompletionSource<Packet>> TCS;
	private float lastSend = 0; // Should be static?, because internal socket flooding causes lost packets?

	public class Settings
	{
		public string Adress { get; set; } = "";
		public string Port { get; set; } = "";
		public string Endpoint { get; set; } = "";
		public string serverPassword { get; set; } = "";
		public bool Secure { get; set; } = false;
		public string SocketID { get; set; } = "My Websocket Client"; // Just for identification, not connection related
		public int maxMessageSize { get; set; } = 65536; // Byte (64KB)
		public bool Reconnect { get; set; } = true; // Reconnect after any disconnection
		public int reconnectDelay { get; set; } = 1000; // MS
		public bool Retry { get; set; } = true; // Retry to connect even if server do not respond
		public int retryDelay { get; set; } = 1000; // MS
		public int sendDelay { get; set; } = 10; // MS, We must wait some time in order to avoid happening annoying problems like losing packets
		public int resultTimeout { get; set; } = 3000; // MS, If server do not respond your expected result, we will terminate this expectation
		public bool WakeupPHPServer { get; set; } = true; // If you do not use to run like "php runSocketserver.php -console" in your PHP socket server, you have to enable this.
		public string phpServerPath { get; set; } = "socket/server/runSocketServer.php";
		public int phpServerWakeupDelay { get; set; } = 3000; // MS
		public bool debug { get; set; } = false;
	}

	[Serializable]
	public partial class Packet
	{
		public string opcode { get; set; }
		public string message { get; set; }
		public string uid { get; set; } = "0x00";
		public string uuid { get; set; }
		public string error { get; set; }
		public string fyi { get; set; }
		public Dictionary<string, string> data { get; set; }
	}

	public WSClient( Settings settings )
	{
		this.settings = settings;
		Create();
	}
	private void Create()
	{
		WS = new( maxMessageSize: settings.maxMessageSize );
		CTS = new();
		TCS = new();

		WS.OnMessageReceived += MessageReceived;
		WS.OnDisconnected += OnDisconnected;

		Log.Info( $"[{settings.SocketID}] Websocket is created and ready." );
	}

	public async void OnDisconnected( int status, string reason )
	{
		Log.Info( $"[{settings.SocketID}] Disconnected/Connection lost" );

		if ( settings.Reconnect && !CTS.IsCancellationRequested )
		{
			Log.Info( $"[{settings.SocketID}] Reconnecting.." );
			await Task.Delay( settings.reconnectDelay, CTS.Token );
			Shutdown();
			Create();
			_ = Connect(); // might be stack overflow
		}
		else
			Shutdown();
	}

	public async Task<bool> Connect()
	{
		await Task.Yield();

		if ( CTS.IsCancellationRequested )
		{
			CTS?.Dispose();
			Log.Info( $"[{settings.SocketID}] Stopped.." );
			return false;
		}

		if ( WS is null )
		{
			Log.Info( $"[{settings.SocketID}] Already removed.." );
			return true;
		}

		if ( WS is not null && WS.IsConnected )
		{
			Log.Info( $"[{settings.SocketID}] Already connected.." );
			return true;
		}

		var server = $"{(settings.Secure ? "wss" : "ws")}://{settings.Adress}:{settings.Port}/{settings.Endpoint}";
		Log.Info( $"[{settings.SocketID}] Connecting to server. ({server})" );

		try
		{
			await WS.Connect( server );
		}
		catch
		{
			Log.Info( $"[{settings.SocketID}] Connection failed." );

			if ( settings.WakeupPHPServer )
			{
				using ( var http = Http.RequestBytesAsync( $"{(settings.Secure ? "https" : "http")}://{settings.Adress}/{settings.phpServerPath}" ) )
				{
					try
					{
						await Task.Delay( settings.phpServerWakeupDelay, CTS.Token );
					}
					catch ( ArgumentException e )
					{
						Log.Info( $"[{settings.SocketID}] Wakeup failed. Error: {e.Message}" );
					}

					http.Dispose();
				}
			}

			if ( settings.Retry || settings.WakeupPHPServer )
			{
				Log.Info( $"[{settings.SocketID}] Retrying: {settings.Retry}, Wakingup: {settings.WakeupPHPServer}" );

				await Task.Delay( settings.retryDelay, CTS.Token );
				Shutdown();
				Create();
				_ = Connect(); // might be stack overflow
				await Task.Yield();

				return false;
			}
			else
				Log.Info( $"[{settings.SocketID}] Terminated." );
		}

		await WS.Send( $"{{\"password\":\"{settings.serverPassword}\"}}" );

		Log.Info( $"[{settings.SocketID}] Login request has sended to websocket. Ready to use." );

		return WS.IsConnected;
	}

	public void Shutdown( bool sendCloseFrame = false )
	{
		CTS?.Cancel();

		if ( WS is not null )
		{
			WS.OnDisconnected -= OnDisconnected;
			WS.OnMessageReceived -= MessageReceived;
		}

		if( sendCloseFrame )
		{
			try
			{
				WS?.Send( "{{\"opcode\":\"quit\"}}" );
			}
			catch {}
		}

		WS?.Dispose();
		WS = null;

		if ( TCS != null )
		{
			foreach ( var tokens in TCS.Values )
				tokens.TrySetCanceled();//TrySetResult( new Packet() { message = "closed" } );
			TCS.Clear();
		}

		Log.Info( $"[{settings.SocketID}] Socket Closed." );
	}

	public async Task<Packet> Send( string message, bool waitResult = true )
	{
		if ( !WS?.IsConnected ?? true )
		{
			Log.Info( $"[{settings.SocketID}] Not connected. Sending failed" );
			return null;
		}
		var diff = Time.Now - lastSend;
		lastSend = Time.Now;
		await Task.Delay( TimeSpan.FromMilliseconds( MathF.Max( settings.sendDelay - diff, 0f ) ) ); // must some wait
		if ( waitResult )
		{
			TaskCompletionSource<Packet> _TCS = new();

			string uid = Guid.NewGuid().ToString().Substring( 0, 7 );
			message = message.Replace( "0x00", uid );

			if ( settings.debug )
				Log.Info( $"outgoing: {message}" );

			await WS.Send( message );

			_ = TCS.TryAdd( uid, _TCS );

			try
			{
				return await _TCS.Task.WaitAsync( TimeSpan.FromMilliseconds( settings.resultTimeout ) ); //, cancellationToken
			}
			catch ( TaskCanceledException )
			{
				Log.Info( $"[{settings.SocketID}] Sending package is cancelled." );
			}
			catch ( TimeoutException )
			{
				Log.Info( $"[{settings.SocketID}] Sending package is timeout." );
			}
			_ = TCS.Remove( uid );
			return null;
		}
		else
		{
			if ( settings.debug )
				Log.Info( $"outgoing (without result): {message}" );
		
			await WS.Send( message );

			return null;
		}
	}

	public async Task<Packet> Send( Packet message, bool waitResult = true ) => _ = await Send( JsonSerializer.Serialize( message ), waitResult );

	private async void MessageReceived( string message )
	{
		if ( message.FastHash() == 786390156 ) // For opcode->next
			return;

		if ( settings.debug )
			Log.Info( $"incoming: {message}" );

		if ( message.Contains( "0x00" ) )
			return;

		var package = await GameTask.RunInThreadAsync( () => JsonSerializer.Deserialize<Packet>( message ) );
		if ( TCS.TryGetValue( package.uid, out var _ ) )
			TCS[package.uid].SetResult( package );
	}

	public bool isReady() => WS?.IsConnected ?? false;
}
