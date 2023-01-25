<?php

/*
 * **********************************************
 * default resource for websockets and sockets
 * **********************************************
 */

class resourceDefault extends resource {

    private $packet; //, $server;

    function onData($SocketID, $M) {
        /*
         * *****************************************
         * $M is JSON like
         * {'opcode':task, <followed by whatever is expected based on the value of opcode>}
         * This is just an example used here , you can send what ever you want.
         * *****************************************
         */

        $packet = $this->getPacket($M);
        if ($packet->opcode === 'jsonerror') {
            $this->server->Log("jsonerror closing #$SocketID");
            $this->server->Close($SocketID);
            return;
        }

        $this->packet = $packet;
        if ($packet->opcode === 'quit') {
            /*
             * *****************************************
             * client quits
             * *****************************************
             */
            $this->server->Log("QUIT; Connection closed to socket #$SocketID");
            $this->server->Close($SocketID);
            return;
        }
        if ($packet->opcode === 'uuid') {
            /*
             * *****************************************
             * web client registers
             * *****************************************
             */
            $this->server->Clients[$SocketID]->uuid = $packet->message;
            $this->server->log("Broadcast $M");
            return;
        }

        if ($packet->opcode === 'feedback') {
            /*
             * *****************************************
             * send feedback to client with uuid found
             * in $packet
             * *****************************************
             */
            $this->server->feedback($packet);
            return;
        }
        if ($packet->opcode === 'echo') {
            /*
             * *****************************************
             * send feedback to client with uuid found
             * in $packet
             * *****************************************
             */
            $this->server->echo($SocketID,$packet);
            return;
        }

        if ($packet->opcode === 'broadcast') {
            $this->server->broadCast($SocketID, $M);
            return;
        }

        if ($packet->opcode === 'mass_data') {

            $this->server->Log("Request result:");

            foreach($packet->data as $keys=>$value){
                // @ for suppressing error
                @$this->server->Log($keys.':'.$value);
            };

            return;
        }

        if ($packet->opcode === 'query') {

            // We can create connection singleton way; https://gist.github.com/ftonato/2973a55baf8eef6795a48804dcdb71dd
            $servername = "localhost";
            $username = "root";
            $password = "";
            $dbname = "test";

            try {
                $pdo = new PDO("mysql:host=$servername;dbname=$dbname", $username, $password);
                // set the PDO error mode to exception
                $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION); //PDO::ERRMODE_WARNING
                if ($pdo) {
    
                    $results = [];

                    foreach($packet->data as $keys=>$value){
                        try {
                            $stmt = $pdo->prepare($value);
                            $stmt->execute();
                            $fetch = $stmt->fetchAll();
                            $results[$keys] = json_encode($fetch);
                        } catch(PDOException $e) {
                            $results[$keys] = $e->getMessage();
                        }
                    };
                    
                    $package;
                    if(count($results) > 0)
                    {
                        $package = (object) ['opcode' => 'query', 'message' => json_encode($results), 'uid' => $packet->uid];
                    }
                    else
                    {
                        $package = (object) ['opcode' => 'query', 'error' => "No Result Found", 'uid' => $packet->uid];
                    }
                    
                    $this->server->Write($SocketID, json_encode($package));
    
                }
                } catch(PDOException $e) {
                    $this->server->Log("Exception: ".$e->getMessage());
    
                    $package = (object) ['opcode' => 'queryExample1', 'message' => $e->getMessage(), 'uid' => $packet->uid];
                    $this->server->Write($SocketID, json_encode($package));
                }
            return;
        }

        if ($packet->opcode === 'queryExample1') {

            $servername = "localhost";
            $username = "root";
            $password = "";
            $dbname = "test";

            try {
            $pdo = new PDO("mysql:host=$servername;dbname=$dbname", $username, $password);
            $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION); 
            if ($pdo) {

                // This str_replace can break multi-line value on your data, be careful
                // We use because on C#, we wrote our 'create_table' query as multi-line to read easily
                
                $stmt = $pdo->prepare(str_replace(array("\r","\n","\t"),"",$packet->data->create_table));
                $stmt->execute();

                $stmt = $pdo->prepare($packet->data->insert_user);
                //$stmt->bindValue(':steamid',$packet->data->steamid );
                //$stmt->bindValue(':nick',$packet->data->nick );
                $stmt->execute([ 'steamid' => $packet->data->steamid, 'nick' => $packet->data->nick ]);

                $package = (object) ['opcode' => 'queryExample1', 'message' => "OK", 'uid' => $packet->uid];
                $this->server->Write($SocketID, json_encode($package));

            }
            } catch(PDOException $e) {
                $this->server->Log("Exception: ".$e->getMessage());

                $package = (object) ['opcode' => 'queryExample1', 'message' => $e->getMessage(), 'uid' => $packet->uid];
                $this->server->Write($SocketID, json_encode($package));
            }

            return;
        }

        if ($packet->opcode === 'queryExample2') {

            $servername = "localhost";
            $username = "root";
            $password = "";
            $dbname = "test";

            try {
            $pdo = new PDO("mysql:host=$servername;dbname=$dbname", $username, $password);
            $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
            if ($pdo) {

                $stmt = $pdo->prepare($packet->data->select_all);
                $stmt->execute();
                $fetch = $stmt->fetchAll();
                
                $data=[];
                foreach ($fetch as $row) {
                    $data[$row["steamid"]] = $row["nick"];
                }

                $package;
                if(count($data) > 0)
                {
                    $package = (object) ['opcode' => 'queryExample2', 'message' => json_encode($data), 'uid' => $packet->uid];
                }
                else
                {
                    $package = (object) ['opcode' => 'queryExample2', 'error' => "No Result Found", 'uid' => $packet->uid];
                }
                $this->server->Write($SocketID, json_encode($package));

            }
            } catch(PDOException $e) {
                $this->server->Log("Exception: ".$e->getMessage());

                $package = (object) ['opcode' => 'queryExample2', 'message' => $e->getMessage(), 'uid' => $packet->uid];
                $this->server->Write($SocketID, json_encode($package));
            }
            return;
        }
        /*
         * *****************************************
         * unknown opcode-> do nothing
         * *****************************************
         */
    }

}
