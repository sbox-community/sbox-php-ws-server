# sbox-php-ws-server
PHP Websocket Server, no dependencies, no installation, drag and drop, ready to use for **Database** access :tada: of your s&amp;box server.

If you have s&box server(s) and a PHP web server, this websocket server will provide a best solution to your organization/community.

Also you can benefit for another application from your web server. With websocket, you can even run your custom networked s&box server.

Other features are;
- Can connect another client(s) (like other game servers, player's client, websites, web browser connections..) and communicate between via the websocket server. (broadcasting, etc..)
- IP blocking and password securing. ( Strongly advised use CDN like cloudflare in order to prevent DDOS attacks )
- PHP Server's other important features..

___

# Configuration:
For WebServer;
- Drag and drop "server/socket" folder to your web server on FTP.
- (If your game server and web server is not on the same machine) Edit 'websock.ini' and change localhost to web server's ip.
- Edit 'webSocketServer.php' and change $password = 'test'
- You can find the implementations of the examples on 'resourceDefault.php'
- For further information, read "server/README.md" and go to phpWebSocketServer's github link below.
- Don't forget to add your url to gamemode's Http allow list https://wiki.facepunch.com/sbox/http

For GameServer;
- Drag and drop the files inside 'game' folder to your gamemode files "code/".
- Edit 'WSClient_Example.cs' from line 23. and read also the comments.
- For testing, use these concommands; wsc_connect, wsc_send and wsc_disconnect
- Now it's your turn, integrate to your gamemode like the examples.

# Notes:
- Tested on PHP 8.1.2, 8.2

# Example:

Output for [https://github.com/sbox-community/sbox-php-ws-server/blob/main/game/WSClient_Example.cs](https://github.com/sbox-community/sbox-php-ws-server/blob/main/game/WSClient_Example.cs#L58)

```c#
> wsc_connect
[WebSocket Client] Websocket is created and ready.
[WebSocket Client] Connecting to server. (ws://localhost:8095/)
[WebSocket Client] Login request has sended to websocket. Ready to use.
Connection to "WebSocket Client" Successful

> wsc_send
Example 1 (echo):
Echo: MSWLKLCNTI
Echo: EF9JO3P8AT
Echo: IOYCJXHTGW
Echo: 8G2WMB2GZ3
Echo: J18Y0R0YZD

Example 2:

Example 3 (parameterized query):
SQL Result: OK

Example 4:
SQL Result: (5)
15334321 : unsafe_nick
86305273 : unsafe_nick
869288614 : unsafe_nick
1133568487 : unsafe_nick
1282368640 : unsafe_nick

Example 5:
SQL Result: (8)
create a table: []
select: []
insert user: []
select 2: [{"test":86923,"0":86923}]
select asd: SQLSTATE[42S02]: Base table or view not found: 1146 Table 'test.asd' doesn't exist
broken: SQLSTATE[42000]: Syntax error or access violation: 1064 You have an error in your SQL syntax; check the manual that corresponds to your MariaDB server version for the right syntax to use near ' ,,d,' at line 1
drop: []
drop again: SQLSTATE[42S02]: Base table or view not found: 1051 Unknown table 'test.test123'

> wsc_disconnect
[WebSocket Client] Socket Closed.
Disconnected to "WebSocket Client"
```

# Credits:
phpWebSocketServer - https://github.com/napengam/phpWebSocketServer
