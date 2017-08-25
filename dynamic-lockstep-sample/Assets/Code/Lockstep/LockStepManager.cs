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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

/// <summary>
/// fzy begin
/// 详见http://jjyy.guru/unity3d-lock-step-part-1
/// fzy end
/// </summary>
[RequireComponent(typeof(NetworkView))]
public class LockStepManager : MonoBehaviour {
	
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	

	public static readonly int FirstLockStepTurnID = 0;
	
	public static LockStepManager Instance;

    //fzy begin
    //LockStepTurnID是回合次数记次，只增加不减少
    //fzy end
	public int LockStepTurnID = FirstLockStepTurnID;
    //fzy begin
    //GameFrame 是在当前LockStepTurnID下的帧数记次
    //当(GameFrame == GameFramesPerLockstepTurn) 时GameFrame重设置为0
    //fzy end
    private int GameFrame = 0; //Current Game Frame number in the currect lockstep turn
	public int numberOfPlayers;

	

	private PendingActions pendingActions;
	private ConfirmedActions confirmedActions;
	
	private Queue<Action> actionsToSend;
	
	private NetworkView nv;
	private NetworkManager gameSetup;
	
	private List<string> readyPlayers;
	private List<string> playersConfirmedImReady;
	
	private bool initialized = false; //indicates if we are initialized and ready for game start
	
	//Variables for adjusting Lockstep and GameFrame length
	RollingAverage networkAverage;
	RollingAverage runtimeAverage;
	long currentGameFrameRuntime; //used to find the maximum gameframe runtime in the current lockstep turn
	private Stopwatch gameTurnSW;
    //fzy begin
    //initialLockStepTurnLength 初始化单lockstep回合时长，毫秒,
    //也就是一次lockstep的时长，帧同步回合时长。
    //fzy end
	private int initialLockStepTurnLength = 200; //in Milliseconds
    //fzy begin
    //initialGameFrameTurnLength 初始化单个游戏帧回合时长，毫秒
    //这个并不是Mono里的一次Update的帧，实际上它会而是在一次帧同步回合中会执行若干次游戏帧回合
    //而游戏帧回合中会执行多个“Update”，在这里是持有的多个 IHasGameFrame对象的GameFrameTurn函数
    //fzy end
	private int initialGameFrameTurnLength = 50; //in Milliseconds
    //fzy begin
    //以下两个LockstepTurnLength，GameFrameTurnLength 对应上面的带initial的两个变量，这两个值是
    //浮动的，需要再看看http://jjyy.guru/unity3d-lock-step-part-2
    //fzy end
	private int LockstepTurnLength;
	private int GameFrameTurnLength;

    //fzy begin
    //GameFramesPerLockstepTurn是一个lockstep回合中，有多少个游戏帧回合
    //在UpdateGameFrameRate函数中被这样算
    //GameFramesPerLockstepTurn = LockstepTurnLength / GameFrameTurnLength;
    //UpdateGameFrameRate函数需要仔细看看，好多关于长度，时长等在这里处理
    //fzy end
	private int GameFramesPerLockstepTurn;

    //fzy begin
    //LockstepsPerSecond 是每秒内lockstep回合的次数
    //fzy end
	private int LockstepsPerSecond;

    //fzy begin
    //GameFramesPerSecond 是每秒内游戏回合的次数
    //GameFramesPerSecond 可以理解成类似Time.deltaTime，在lockstep中IHasGameFrame对象的GameFrameTurn相当于Update。
    //因此GameFrameTurn也需要要一个Time.deltaTime，但显然又不能用Time.deltaTime。
    //这个值怎么来的需要好好看看。因为游戏中很多代码是要被IHasGameFrame对象的GameFrameTurn函数驱动，
    //上面说过，这个函数相当于Update，这个值相当于Time.deltaTime。整个lockstep机制中，一个重要的环节
    //就是要计算出基于lockstep的Time.deltaTime
    //fzy end
	private int GameFramesPerSecond;
	
	private int playerIDToProcessFirst = 0; //used to rotate what player's action gets processed first
	
	private int AccumilatedTime = 0; //the accumilated time in Milliseconds that have passed since the last time GameFrame was called
	
	// Use this for initialization
	void Start () {
		enabled = false;
		
		Instance = this;
		nv = GetComponent<NetworkView>();
		gameSetup = FindObjectOfType(typeof(NetworkManager)) as NetworkManager;
		
		gameSetup.OnGameStart += PrepGameStart;
	}
	
	#region GameStart
	public void InitGameStartLists() {
		if(initialized) { return; }
		
		readyPlayers = new List<string>(numberOfPlayers);
		playersConfirmedImReady = new List<string>(numberOfPlayers);
		
		initialized = true;
	}
	
	public void PrepGameStart() {
		
		log.Debug ("GameStart called. My PlayerID: " + Network.player.ToString());
		LockStepTurnID = FirstLockStepTurnID;
		numberOfPlayers = gameSetup.NumberOfPlayers;
		pendingActions = new PendingActions(this);
		confirmedActions = new ConfirmedActions(this);
		actionsToSend = new Queue<Action>();
		
		gameTurnSW = new Stopwatch();
		currentGameFrameRuntime = 0;
		networkAverage = new RollingAverage(numberOfPlayers, initialLockStepTurnLength);
		runtimeAverage = new RollingAverage(numberOfPlayers, initialGameFrameTurnLength);
		
		InitGameStartLists();
		
		nv.RPC ("ReadyToStart", RPCMode.AllBuffered, Network.player.ToString());
	}
	
	private void CheckGameStart() {
		if(playersConfirmedImReady == null) {
			log.Debug("WARNING!!! Unexpected null reference during game start. IsInit? " + initialized);
			return;
		}
		//check if all expected players confirmed our gamestart message
		if(playersConfirmedImReady.Count == numberOfPlayers) {
			//check if all expected players sent their gamestart message
			if(readyPlayers.Count == numberOfPlayers) {
				//we are ready to start
				log.Debug("All players are ready to start. Starting Game.");
				
				//we no longer need these lists
				playersConfirmedImReady = null;
				readyPlayers = null;
				
				GameStart ();
			}
		}
	}
	
	private void GameStart() {
		//start the LockStep Turn loop
		enabled = true;
	}
	
	[RPC]
	public void ReadyToStart(string playerID) {
		log.Debug("Player " + playerID + " is ready to start the game.");
		
		//make sure initialization has already happened -incase another player sends game start before we are ready to handle it
		InitGameStartLists();;
		
		readyPlayers.Add (playerID);
		
		if(Network.isServer) {
			//don't need an rpc call if we are the server
			ConfirmReadyToStartServer(Network.player.ToString() /*confirmingPlayerID*/, playerID /*confirmedPlayerID*/);
		} else {
			nv.RPC("ConfirmReadyToStartServer", RPCMode.Server, Network.player.ToString() /*confirmingPlayerID*/, playerID /*confirmedPlayerID*/);
		}
		
		//Check if we can start the game
		CheckGameStart();
	}
	
	[RPC]
	public void ConfirmReadyToStartServer(string confirmingPlayerID, string confirmedPlayerID) {
		if(!Network.isServer) { return; } //workaround when multiple players running on same machine
		
		log.Debug("Server Message: Player " + confirmingPlayerID + " is confirming Player " + confirmedPlayerID + " is ready to start the game.");
		
		//validate ID
		if(!gameSetup.players.ContainsKey(confirmingPlayerID)) {
			//TODO: error handling
			log.Debug("Server Message: WARNING!!! Unrecognized confirming playerID: " + confirmingPlayerID);
			return;
		}
		if(!gameSetup.players.ContainsKey(confirmedPlayerID)) {
			//TODO: error handling
			log.Debug("Server Message: WARNING!!! Unrecognized confirmed playerID: " + confirmingPlayerID);
		}
		
		//relay message to confirmed client
		if(Network.player.ToString().Equals(confirmedPlayerID)) {
			//don't need an rpc call if we are the server
			ConfirmReadyToStart(confirmedPlayerID, confirmingPlayerID);
		} else {
			nv.RPC ("ConfirmReadyToStart", RPCMode.OthersBuffered, confirmedPlayerID, confirmingPlayerID);
		}
		
	}
	
	[RPC]
	public void ConfirmReadyToStart(string confirmedPlayerID, string confirmingPlayerID) {
		if(!Network.player.ToString().Equals(confirmedPlayerID)) { return; }
		
		//log.Debug ("Player " + confirmingPlayerID + " confirmed I am ready to start the game.");
		playersConfirmedImReady.Add (confirmingPlayerID);
		
		//Check if we can start the game
		CheckGameStart();
	}
	#endregion
	
	#region Actions
	public void AddAction(Action action) {
		log.Debug ("Action Added");
		if(!initialized) {
			log.Debug("Game has not started, action will be ignored.");
			return;
		}
		actionsToSend.Enqueue(action);
	}
	
	private bool LockStepTurn() 
    {
		log.Debug ("LockStepTurnID: " + LockStepTurnID);
        //fzy begin
        //检查两点
        //1.我们已经收到了所有客户端的下一轮动作了吗？
        //2.每个客户端都确认得到我们的动作了吗？
        //fzy end
		//Check if we can proceed with the next turn
		bool nextTurn = NextTurn();
		if(nextTurn) 
        {
            //fzy begin
            //向其他服务器/客户端发送下本客户端下一轮的动作
            //fzy end
			SendPendingAction ();
			//the first and second lockstep turn will not be ready to process yet
			if(LockStepTurnID >= FirstLockStepTurnID + 3) 
            {
                //fzy begin
                //处理Action，这些Action都是其他客户端确认过的吗？貌似上面的bool nextTurn = NextTurn();可以判断
                //从道理上讲，这些Action中有本客户端控制的玩家，也有其他客户端控制的玩家
                //比如说，可能本客户端的玩家移动，也有同步的其他客户端的玩家移动
                //fzy end
				ProcessActions ();
			}
		}
		//otherwise wait another turn to recieve all input from all players
		
		UpdateGameFrameRate();
		return nextTurn;
	}
	
	/// <summary>
	/// Check if the conditions are met to proceed to the next turn.
	/// If they are it will make the appropriate updates. Otherwise 
	/// it will return false.
	/// </summary>
	private bool NextTurn() 
    {
		//		log.Debug ("Next Turn Check: Current Turn - " + LockStepTurnID);
		//		log.Debug ("    priorConfirmedCount - " + confirmedActions.playersConfirmedPriorAction.Count);
		//		log.Debug ("    currentConfirmedCount - " + confirmedActions.playersConfirmedCurrentAction.Count);
		//		log.Debug ("    allPlayerCurrentActionsCount - " + pendingActions.CurrentActions.Count);
		//		log.Debug ("    allPlayerNextActionsCount - " + pendingActions.NextActions.Count);
		//		log.Debug ("    allPlayerNextNextActionsCount - " + pendingActions.NextNextActions.Count);
		//		log.Debug ("    allPlayerNextNextNextActionsCount - " + pendingActions.NextNextNextActions.Count);
		
        //fzy begin
        //检查两点
        //1.我们已经收到了所有客户端的下一轮动作了吗？
        //2.每个客户端都确认得到我们的动作了吗？
        //fzy end
		if(confirmedActions.ReadyForNextTurn()) 
        {
			if(pendingActions.ReadyForNextTurn()) 
            {
                //fzy begin
                //LockStepTurnID是回合次数记次，只增加不减少
                //fzy end
				//increment the turn ID
                LockStepTurnID++;

				//move the confirmed actions to next turn
				confirmedActions.NextTurn();
				//move the pending actions to this turn
				pendingActions.NextTurn();
				
				return true;
			} 
            else 
            {
				StringBuilder sb = new StringBuilder();
				sb.Append ("Have not recieved player(s) actions: ");
				foreach(int i in pendingActions.WhosNotReady ()) 
                {
					sb.Append (i + ", ");
				}
				log.Debug(sb.ToString ());
			}
		} 
        else 
        {
			StringBuilder sb = new StringBuilder();
			sb.Append ("Have not recieved confirmation from player(s): ");
			foreach(int i in pendingActions.WhosNotReady ()) 
            {
				sb.Append (i + ", ");
			}
			log.Debug(sb.ToString ());
		}
		
		//		if(confirmedActions.ReadyForNextTurn() && pendingActions.ReadyForNextTurn()) {
		//			//increment the turn ID
		//			LockStepTurnID++;
		//			//move the confirmed actions to next turn
		//			confirmedActions.NextTurn();
		//			//move the pending actions to this turn
		//			pendingActions.NextTurn();
		//			
		//			return true;
		//		}
		
		return false;
	}
	
	private void SendPendingAction() {
		Action action = null;
		if(actionsToSend.Count > 0) {
			action = actionsToSend.Dequeue();
		}
		
		//if no action for this turn, send the NoAction action
		if(action == null) {
			action = new NoAction();
		}
		
		//action.NetworkAverage = Network.GetLastPing (Network.connections[0/*host player*/]);
		if(LockStepTurnID > FirstLockStepTurnID + 1) {
			action.NetworkAverage = confirmedActions.GetPriorTime();
		} else {
			action.NetworkAverage = initialLockStepTurnLength;
		}
		action.RuntimeAverage = Convert.ToInt32 (currentGameFrameRuntime);
		//clear the current runtime average
		currentGameFrameRuntime = 0;
		
		//add action to our own list of actions to process
		pendingActions.AddAction(action, Convert.ToInt32(Network.player.ToString()), LockStepTurnID, LockStepTurnID);
		//start the confirmed action timer for network average
		confirmedActions.StartTimer ();
		//confirm our own action
		confirmedActions.ConfirmAction(Convert.ToInt32(Network.player.ToString ()), LockStepTurnID, LockStepTurnID);
		//send action to all other players
		nv.RPC("RecieveAction", RPCMode.Others, LockStepTurnID, Network.player.ToString(), BinarySerialization.SerializeObjectToByteArray(action));
		
		log.Debug("Sent " + (action.GetType().Name) + " action for turn " + LockStepTurnID);
	}
	
	private void ProcessActions() {
		//process action should be considered in runtime performance
		gameTurnSW.Start ();
		
        //fzy begin
        //这里为什么要pendingActions拆分成两段执行
        //fzy end
		//Rotate the order the player actions are processed so there is no advantage given to
		//any one player
		for(int i=playerIDToProcessFirst; 
            i< pendingActions.CurrentActions.Length; i++)
        {
			pendingActions.CurrentActions[i].ProcessAction();
			runtimeAverage.Add (pendingActions.CurrentActions[i].RuntimeAverage, i);
			networkAverage.Add (pendingActions.CurrentActions[i].NetworkAverage, i);
		}
		
		for(int i=0; i<playerIDToProcessFirst; i++) 
        {
			pendingActions.CurrentActions[i].ProcessAction();
			runtimeAverage.Add (pendingActions.CurrentActions[i].RuntimeAverage, i);
			networkAverage.Add (pendingActions.CurrentActions[i].NetworkAverage, i);
		}
		
		playerIDToProcessFirst++;
		if(playerIDToProcessFirst >= pendingActions.CurrentActions.Length) {
			playerIDToProcessFirst = 0;
		}
		
		//finished processing actions for this turn, stop the stopwatch
		gameTurnSW.Stop ();
	}
	
	[RPC]
	public void RecieveAction(int lockStepTurn, string playerID, byte[] actionAsBytes) {
		//log.Debug ("Recieved Player " + playerID + "'s action for turn " + lockStepTurn + " on turn " + LockStepTurnID);
		Action action = BinarySerialization.DeserializeObject<Action>(actionAsBytes);
		if(action == null) {
			log.Debug ("Sending action failed");
			//TODO: Error handle invalid actions recieve
		} else {
			pendingActions.AddAction(action, Convert.ToInt32(playerID), LockStepTurnID, lockStepTurn);
			
			//send confirmation
			if(Network.isServer) {
				//we don't need an rpc call if we are the server
				ConfirmActionServer (lockStepTurn, Network.player.ToString(), playerID);
			} else {
				nv.RPC ("ConfirmActionServer", RPCMode.Server, lockStepTurn, Network.player.ToString(), playerID);
			}
		}
	}
	
	[RPC]
	public void ConfirmActionServer(int lockStepTurn, string confirmingPlayerID, string confirmedPlayerID) {
		if(!Network.isServer) { return; } //Workaround - if server and client on same machine
		
		//log.Debug("ConfirmActionServer called turn:" + lockStepTurn + " playerID:" + confirmingPlayerID);
		//log.Debug("Sending Confirmation to player " + confirmedPlayerID);
		
		if(Network.player.ToString().Equals(confirmedPlayerID)) {
			//we don't need an RPC call if this is the server
			ConfirmAction(lockStepTurn, confirmingPlayerID);
		} else {
			nv.RPC("ConfirmAction", gameSetup.players[confirmedPlayerID], lockStepTurn, confirmingPlayerID);
		}
	}
	
	[RPC]
	public void ConfirmAction(int lockStepTurn, string confirmingPlayerID) {
		confirmedActions.ConfirmAction (Convert.ToInt32(confirmingPlayerID), LockStepTurnID, lockStepTurn);
	}
	#endregion
	
	#region Game Frame
	private void UpdateGameFrameRate() {
		//log.Debug ("Runtime Average is " + runtimeAverage.GetMax ());
		//log.Debug ("Network Average is " + networkAverage.GetMax ());
		LockstepTurnLength = (networkAverage.GetMax () * 2/*two round trips*/) + 1/*minimum of 1 ms*/;
		GameFrameTurnLength = runtimeAverage.GetMax ();
		
		//lockstep turn has to be at least as long as one game frame
		if(GameFrameTurnLength > LockstepTurnLength) {
			LockstepTurnLength = GameFrameTurnLength;
		}
		
		GameFramesPerLockstepTurn = LockstepTurnLength / GameFrameTurnLength;
		//if gameframe turn length does not evenly divide the lockstep turn, there is extra time left after the last
		//game frame. Add one to the game frame turn length so it will consume it and recalculate the Lockstep turn length
		if(LockstepTurnLength % GameFrameTurnLength > 0) {
			GameFrameTurnLength++;
			LockstepTurnLength = GameFramesPerLockstepTurn * GameFrameTurnLength;
		}
		
        //fzy begin
        //LockstepsPerSecond 是每秒内lockstep回合的次数
        //LockstepTurnLength的单位是毫秒，所以得用1000去除以LockstepTurnLength
        //fzy end
		LockstepsPerSecond = (1000 / LockstepTurnLength);
		if(LockstepsPerSecond == 0) 
        {
            LockstepsPerSecond = 1; //minimum per second
        } 
		
        //fzy begin
        //GameFramesPerSecond 是每秒内游戏回合的次数
        //fzy end
		GameFramesPerSecond = LockstepsPerSecond * GameFramesPerLockstepTurn;		
	}
	
	//called once per unity frame
	public void Update() 
    {
		//Basically same logic as FixedUpdate, but we can scale it by adjusting FrameLength
		AccumilatedTime = AccumilatedTime + Convert.ToInt32((Time.deltaTime * 1000)); //convert sec to milliseconds
		
		//in case the FPS is too slow, we may need to update the game multiple times a frame
		while(AccumilatedTime > GameFrameTurnLength) 
        {
			GameFrameTurn ();
			AccumilatedTime = AccumilatedTime - GameFrameTurnLength;
		}
	}
	
	private void GameFrameTurn() 
    {
		//first frame is used to process actions
		if(GameFrame == 0) 
        {
			if(!LockStepTurn()) 
            {
				//if the lockstep turn is not ready to advance, do not run the game turn
				return;
			}
		}

        //fzy begin 
        //gameTurnSW 是c#的Stopwatch类，用于计算运行时间。
        //Start()和Stop()配对Reset()重置。ElapsedMilliseconds是时间
        //fzy end
		//start the stop watch to determine game frame runtime performance
		gameTurnSW.Start();


        //fzy begin
        //IHasGameFrame相当于游戏中所有需要Update的组件，GameFrameTurn接口相当于Update，
        //int gameFramesPerSecond 作为参数带入相当于Time.deltaTime
        //这个Finished怎么用？
        //fzy end
		//update game
		//SceneManager.Manager.TwoDPhysics.Update (GameFramesPerSecond);
		List<IHasGameFrame> finished = new List<IHasGameFrame>();
		foreach(IHasGameFrame obj in SceneManager.Manager.GameFrameObjects) 
        {
			obj.GameFrameTurn(GameFramesPerSecond);
            //fzy begin
            //完成了的放入List待移除
            //fzy end
			if(obj.Finished) 
            {
				finished.Add (obj);
			}
		}

        //fzy begin
        //完成了的移除
        //fzy end
		foreach(IHasGameFrame obj in finished) 
        {
			SceneManager.Manager.GameFrameObjects.Remove (obj);
		}
		
        //fzy begin
        //按前面GameFrame定义的注释理解。
        //完成了一轮所有IHasGameFrame的GameFrameTurn，那么GameFrame就加1
        //Current Game Frame number in the currect lockstep turn
        //fzy end
		GameFrame++;
		if(GameFrame == GameFramesPerLockstepTurn) 
        {
			GameFrame = 0;
		}
		
		//stop the stop watch, the gameframe turn is over
		gameTurnSW.Stop ();

        //fzy begin
        //gameTurnSW记录了本轮执行所有IHasGameFrame的GameFrameTurn时间。
        //runtime = gameTurnSW + Time.deltaTime 就成了本轮真实的时间
        //然后把最大的runtime记录到currentGameFrameRuntime。
        //这样currentGameFrameRuntime 就成了最长的游戏帧的时间
        //fzy end
		//update only if it's larger - we will use the game frame that took the longest in this lockstep turn
        //deltaTime is in secounds, convert to milliseconds
		long runtime = Convert.ToInt32 ((Time.deltaTime * 1000)) + gameTurnSW.ElapsedMilliseconds;
		if(runtime > currentGameFrameRuntime) 
        {
			currentGameFrameRuntime = runtime;
		}

		//clear for the next frame
		gameTurnSW.Reset();
	}
	#endregion
}

