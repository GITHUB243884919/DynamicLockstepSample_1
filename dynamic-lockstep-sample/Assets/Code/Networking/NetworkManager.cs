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

    //fzy begin
    //参与同步的客户端数量，在这里prefab中这个值实际是2
    //fzy end
	//TODO: Add ability to allow hosting user to set this number
	public int NumberOfPlayers = 1;
	
	public Dictionary<string, NetworkPlayer> players;
	
	float btnX;
	float btnY;
	float btnW;
	float btnH;
	
	public delegate void NetworkManagerEvent();
    //fzy begin
    //在这里例子中没有用到
    //fzy end
	public NetworkManagerEvent OnConnectedToGame;
	
    //fzy begin
    //这个回调很重要，是在LockStepManager 在Start()中注册过来的
    //当服务器发现所有客户端都连接上来后，执行这个回调
    //fzy end
    public NetworkManagerEvent OnGameStart;
	
	private NetworkView nv;
	
	private void Start() 
    {
		btnX = 50;
		btnY = Screen.height - 200;
		btnW = 100;
		btnH = 50;
		
		players = new Dictionary<string, NetworkPlayer>(NumberOfPlayers);
		nv = GetComponent<NetworkView>();
		nv.stateSynchronization = NetworkStateSynchronization.Off;
	}
	
	private void Update() 
    {
		if(refreshing) 
        {
			if(MasterServer.PollHostList().Length > 0) 
            {
				refreshing = false;
				Debug.Log("HostList Length: " + MasterServer.PollHostList().Length);
				hostData = MasterServer.PollHostList();
			}
		}
	}
	
	private void startServer() 
    {
		Debug.Log("startServer called");
		
		bool useNAT = !Network.HavePublicAddress();
		Network.InitializeServer(32, 25001, useNAT);
		MasterServer.RegisterHost(gameTypeName, "Sample_Game_Name", NetworkHostMessages.GenerateHostComment(NumberOfPlayers));
	}
	
	private void refreshHostList() 
    {
		MasterServer.RequestHostList(gameTypeName);
		refreshing = true;
	}
	
	private void PrintHostData() 
    {
		HostData[] hostData = MasterServer.PollHostList();
		Debug.Log("HostData length " + hostData.Length);
	}
	
	#region Unity内置网络回调函数
	private void OnServerInitialized() 
    {
		Debug.Log("Server initialized");
		Debug.Log("Expected player count : " + NumberOfPlayers);
		//Notify any delegates that we are connected to the game
        //Debug.LogWarning("OnConnectedToGame " +(OnConnectedToGame != null).ToString());
		if(OnConnectedToGame != null) 
        {
			OnConnectedToGame();
		}

        //fzy begin
        //这时Network.player是服务器本身，这个例子是把服务器也当一个端，所以把自己也加到players中去了
        //fzy end
        Debug.Log("OnServerInitialized Add playerID " + Network.player.ToString());
		players.Add(Network.player.ToString(), Network.player);
        //fzy begin
        //NumberOfPlayers最少是2，所以以下这行代码实际是不会被调用
        //fzy end
		if(NumberOfPlayers == 1) 
        {
			StartGame();
		}
	}
	
	private void OnMasterServerEvent(MasterServerEvent mse) {
		Debug.Log("Master Server Event: " + mse.ToString());
	}
	
    /// <summary>
    /// fzy
    /// 当有客户端连接到服务器后
    /// 1、连接上的客户端放入players 中
    /// 2、如果所有客户端已经连上players.Count == NumberOfPlayers
    ///     （1）向所有客户端RPC RegisterPlayerAll,这个函数实际上是让所有
    ///          客户端都的players列表都装满所有的player
    ///     （2）所有客户端，服务端 RPC StartGame
    /// </summary>
    /// <param name="player"></param>
	private void OnPlayerConnected(NetworkPlayer player) 
    {
		players.Add(player.ToString(), player);
		Debug.Log("OnPlayerConnected, add playerID:" + player.ToString());
		Debug.Log("Player Count : " + players.Count);

        //fzy begin
        //检查是否所有客户端都连接上
        //fzy end
		//Once all expected players have joined, send all clients the list of players
		if(players.Count == NumberOfPlayers) 
        {
			foreach(NetworkPlayer _player in players.Values) 
            {
				Debug.Log("Calling RegisterPlayerAll...");
                nv.RPC("RegisterPlayerAll", RPCMode.Others, _player);
			}
			
			//start the game
			nv.RPC("StartGame", RPCMode.All);
		}
	}

    void OnDisconnectedFromServer(NetworkDisconnection info)
    {
        if(Network.isServer)
            Debug.Log("Local server connection disconnected");
        else
            if(info == NetworkDisconnection.LostConnection)
                Debug.Log("Lost connection to the server");
            else
                Debug.Log("Successfully diconnected from the server");
    }
    #endregion

	/// <summary>
	/// Called on clients only. Passes all connected players to be added to the players dictionary.
	/// </summary>
	[RPC]
	public void RegisterPlayerAll(NetworkPlayer player) 
    {
		Debug.Log("Register Player All called for " + player.ToString());
		players.Add(player.ToString(), player);
	}
	
	[RPC]
	public void StartGame() 
    {
		Debug.Log("StartGame called");
		//send the start of game event
		if(OnGameStart != null) 
        {
			OnGameStart();
		}
	}
	
	#region GUI
	private void OnGUI() 
    {
		if(!Network.isClient && !Network.isServer) 
        {
			if(GUI.Button(new Rect(btnX, btnY, btnW, btnH), "Start Server")) 
            {
                Debug.Log("Starting Server");
				startServer();
			}
			
			if(GUI.Button(new Rect(btnX, btnY + btnH, btnW, btnH), "Refresh Hosts")) 
            {
				Debug.Log("Refreshing Hosts");
				refreshHostList();
			}
			
			if(hostData != null) 
            {
				int i =0;
				foreach(HostData hd in hostData) 
                {
					if(GUI.Button(new Rect(btnX * 1.5f + btnW, btnY * 1.2f +(btnH * i), btnW * 3f, btnH * 0.5f), hd.gameName)) 
                    {
						Debug.Log("Connecting to server");
						Network.Connect(hd);
						//Notify any delegates that we are connected to the game
                        Debug.LogWarning("OnConnectedToGame " +(OnConnectedToGame != null).ToString());
						if(OnConnectedToGame != null) 
                        {
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
