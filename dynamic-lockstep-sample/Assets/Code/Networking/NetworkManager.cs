//The MIT License (MIT)

//Copyright (c) 2013 Clinton Brennan

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkView))]
public class NetworkManager : MonoBehaviour {
	
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	
	private string gameTypeName = "Sample_LockStep_Network";
	private bool refreshing = false;
	private HostData[] hostData;
	//TODO: Add ability to allow hosting user to set this number
	public int NumberOfPlayers = 1;
	
	public Dictionary<string, NetworkPlayer> players;
	
	float btnX;
	float btnY;
	float btnW;
	float btnH;
	
	public delegate void NetworkManagerEvent();
	public NetworkManagerEvent OnConnectedToGame;
	public NetworkManagerEvent OnGameStart;
	
	private NetworkView nv;
	
	private void Start() {
		btnX = 50;
		btnY = Screen.height - 200;
		btnW = 100;
		btnH = 50;
		
		players = new Dictionary<string, NetworkPlayer>(NumberOfPlayers);
		nv = GetComponent<NetworkView>();
		nv.stateSynchronization = NetworkStateSynchronization.Off;
	}
	
	private void Update() {
		if(refreshing) {
			if(MasterServer.PollHostList().Length > 0) {
				refreshing = false;
				log.Debug ("HostList Length: " + MasterServer.PollHostList().Length);
				hostData = MasterServer.PollHostList();
			}
		}
	}
	
	private void startServer() {
		log.Debug("startServer called");
		
		bool useNAT = !Network.HavePublicAddress();
		Network.InitializeServer(32, 25001, useNAT);
		MasterServer.RegisterHost (gameTypeName, "Sample_Game_Name", NetworkHostMessages.GenerateHostComment(NumberOfPlayers));
	}
	
	private void refreshHostList() {
		MasterServer.RequestHostList(gameTypeName);
		refreshing = true;
	}
	
	private void PrintHostData() {
		HostData[] hostData = MasterServer.PollHostList();
		log.Debug ("HostData length " + hostData.Length);
	}
	
	#region Messages
	private void OnServerInitialized() {
		log.Debug ("Server initialized");
		log.Debug("Expected player count : " + NumberOfPlayers);
		//Notify any delegates that we are connected to the game
		if(OnConnectedToGame != null) {
			OnConnectedToGame();
		}

		players.Add (Network.player.ToString(), Network.player);
		if(NumberOfPlayers == 1) {
			StartGame ();
		}
	}
	
	private void OnMasterServerEvent(MasterServerEvent mse) {
		log.Debug("Master Server Event: " + mse.ToString());
	}
	
	private void OnPlayerConnected (NetworkPlayer player) {
		players.Add (player.ToString(), player);
		log.Debug ("OnPlayerConnected, playerID:" + player.ToString());
		log.Debug ("Player Count : " + players.Count);
		//Once all expected players have joined, send all clients the list of players
		if(players.Count == NumberOfPlayers) {
			foreach(NetworkPlayer p in players.Values) {
				log.Debug ("Calling RegisterPlayerAll...");
				nv.RPC("RegisterPlayerAll", RPCMode.Others, p);
			}
			
			//start the game
			nv.RPC ("StartGame", RPCMode.All);
		}
	}
	
	/// <summary>
	/// Called on clients only. Passes all connected players to be added to the players dictionary.
	/// </summary>
	[RPC]
	public void RegisterPlayerAll(NetworkPlayer player) {
		log.Debug ("Register Player All called for " + player.ToString());
		players.Add (player.ToString(), player);
	}
	
	[RPC]
	public void StartGame() {
		log.Debug ("StartGame called");
		//send the start of game event
		if(OnGameStart!=null) {
			OnGameStart();
		}
	}
	
	void OnDisconnectedFromServer(NetworkDisconnection info) {
        if (Network.isServer)
            log.Debug("Local server connection disconnected");
        else
            if (info == NetworkDisconnection.LostConnection)
                log.Debug("Lost connection to the server");
            else
                log.Debug("Successfully diconnected from the server");
    }
	#endregion
	
	#region GUI
	private void OnGUI() {
		if(!Network.isClient && !Network.isServer) {
			if(GUI.Button (new Rect(btnX, btnY, btnW, btnH), "Start Server")) {
				log.Debug ("Starting Server");
				startServer();
			}
			
			if(GUI.Button (new Rect(btnX, btnY * 1.2f + btnH, btnW, btnH), "Refresh Hosts")) {
				log.Debug ("Refreshing Hosts");
				refreshHostList();
			}
			
			if(hostData!=null) {
				int i =0;
				foreach(HostData hd in hostData) {
					if(GUI.Button (new Rect(btnX * 1.5f + btnW, btnY * 1.2f + (btnH * i), btnW * 3f, btnH * 0.5f), hd.gameName)) {
						log.Debug ("Connecting to server");
						Network.Connect (hd);
						//Notify any delegates that we are connected to the game
						if(OnConnectedToGame != null) {
							OnConnectedToGame();
						}
					}
					i++;
				}
			}
		}
	}
	#endregion
}
