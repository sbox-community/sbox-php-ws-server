using Sandbox;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using static WSClient;

static class WSClient_Example
{
	static WSClient? WSC;

	[ConCmd.Server( "wsc_connect" )]
	public static async void create_wsc()
	{
		shutdown_wsc();

		// Some settings commented as read from file, because serverside .cs files are shared (for now).
		// If you create .txt file into data/(releated gamemode)/ on your server, will be safe
		// Example; FileSystem.Data.ReadAllText("sql_address.txt");
		var settings = new Settings()
		{
			// Example: ws://localhost:8095/ or wss://localhost:8095/endpoint

			Adress = "localhost", // Reading from file recommended. 
			Port = "8095",
			Endpoint = "", // Reading from file recommended
			serverPassword = "test", // Reading from file recommended
			Secure = false,
			SocketID = "WebSocket Client",
			phpServerPath = "socket/server/runSocketServer.php", // Reading from file recommended
		};

		WSC = new WSClient( settings );

		WSC.WS.OnMessageReceived += MessageReceived;

		bool isConnected = await WSC.Connect();
		if ( isConnected ) Log.Info( $"Connection to \"{settings.SocketID}\" Successful" );
	}
	private static void MessageReceived( string message )
	{
		if ( message.FastHash() == 786390156 ) // For opcode->next
			return;

		//Log.Info( message );
	}

	[ConCmd.Server( "wsc_disconnect" )]
	public static void shutdown_wsc()
	{
		if( WSC is not null ) {
			WSC.Shutdown( true );
			Log.Info( $"Disconnected to \"{WSC.settings.SocketID}\"" );
			WSC = null;
		}
	}

	[ConCmd.Server( "wsc_send" )]
	public async static void send_example_wsc()
	{
		if ( !WSC?.isReady() ?? true )
			return; //Log.Error( WSC is null ? "WSClient is not created yet" : $"WSClient is not connected to {WSC.settings.SocketID}" );

		///////////////////////////////////////////////////////////////////////////////////////////////////////
		// Example 1 (echo):

		Log.Info("");
		Log.Info( "Example 1 (echo):" );

		for ( var i = 0; i < 5; i++ )
		{
			var randomString = new string( Enumerable.Repeat( "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 10 ).Select( s => s[Game.Random.Next( s.Length )] ).ToArray() );
			Packet? val = await WSC.Send(
				new Packet()
				{
					opcode = "echo",
					message = randomString
				},
				waitResult: true
			); ;
			Log.Info( $"Echo: {(val?.message ?? "error/connection problem")}" );
		}

		///////////////////////////////////////////////////////////////////////////////////////////////////////
		// Example 2 (without result, good enough for logging):

		Log.Info( "" );
		Log.Info( "Example 2 (without result, good enough for logging):" );

		// settings.maxMessageSize, $bufferLength and $bufferChunk on webSocketServer.php
		Packet? val2 = await WSC.Send(
			new Packet()
			{
				opcode = "mass_data",
				data = new()
				{
					{ "data1", "100" },
					{ "data2", "200" },
					{ "data3", "300" },
					{ "data4", "400" },
					{ "data5", "500" },
					{ "data6", "600" },
					{ "data7", "700" },
					{ "data8", "800" },
					{ "data9", "900" },
				}
			},
			waitResult: false
		);

		///////////////////////////////////////////////////////////////////////////////////////////////////////
		// Example 3 (parameterized query):

		Log.Info( "" );
		Log.Info( "Example 3 (parameterized query):" );

		var SQLVal = await WSC.Send(
			new Packet()
			{
				opcode = "queryExample1",
				data = new()
				{
					{ "create_table",
										@"CREATE TABLE IF NOT EXISTS Users (
											steamid bigint PRIMARY KEY,
											nick varchar(255)
										);"
					},
					{"insert_user", "INSERT IGNORE INTO Users (steamid, nick) VALUES (:steamid, :nick)" },
					{"steamid", Game.Random.Int(int.MaxValue - 1).ToString() },
					{"nick", "unsafe_nick" }
				}
			},
			waitResult: true
		);

		Log.Info( $"SQL Result: {(SQLVal?.message ?? "error/connection problem")}" );

		///////////////////////////////////////////////////////////////////////////////////////////////////////
		// Example 4 (custom query):

		Log.Info( "" );
		Log.Info( "Example 4 (custom query):" );

		SQLVal = await WSC.Send(
			new Packet()
			{
				opcode = "queryExample2",
				data = new()
				{
					{"select_all", "SELECT * FROM Users LIMIT 50 OFFSET 0" },
				}
			},
			waitResult: true
		);

		if ( SQLVal?.message is not null )
		{
			var result = JsonSerializer.Deserialize<Dictionary<string, string>>( SQLVal.message );
			Log.Info( $"SQL Result: ({ result.Count })" );
			foreach ( var row in result )
				Log.Info( $"{row.Key} : {row.Value}" );
		}
		else
			Log.Info( $"SQL Result: {(SQLVal?.error ?? "error/connection problem")}" );


		///////////////////////////////////////////////////////////////////////////////////////////////////////
		// Example 5 (simple query):

		Log.Info( "" );
		Log.Info( "Example 5 (simple query):" );

		// Not safe way, you prevent sql injection codes, sanitize them.
		// SqlCommand as parameterized is blacklisted, unfortunately can not use.

		SQLVal = await WSC.Send(
			new Packet()
			{
				opcode = "query",
				data = new()
				{
						{"create a table", "CREATE TABLE IF NOT EXISTS Test123( test int PRIMARY KEY );" },
						{"select", "SELECT * FROM Test123" },
						{"insert user", $"INSERT IGNORE INTO Test123 (test) VALUES ({Game.Random.Int(99999)})" },
						{"select 2", "SELECT * FROM Test123" },
						{"select asd", "SELECT * FROM asd" },
						{"broken", "SELECT * FROM asd,, ,,d, " },
						{"drop", "DROP TABLE Test123" },
						{"drop again", "DROP TABLE Test123" },
				}
			},
			waitResult: true
		);

		if ( SQLVal?.message is not null )
		{
			var result = JsonSerializer.Deserialize<Dictionary<string, string>>( SQLVal.message );
			Log.Info( $"SQL Result: ({result.Count})" );
			foreach ( var row in result )
				Log.Info( $"{row.Key}: {row.Value}" );
		}
		else
			Log.Info( $"SQL Result: {(SQLVal?.error ?? "error/connection problem")}" );
	}
}
