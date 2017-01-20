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
using System;
using System.Collections.Generic;

public class PendingActions
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
	
	public Action[] CurrentActions;
	private Action[] NextActions;
	private Action[] NextNextActions;
	//incase other players advance to the next step and send their action before we advance a step
	private Action[] NextNextNextActions;
	
	private int currentActionsCount;
	private int nextActionsCount;
	private int nextNextActionsCount;
	private int nextNextNextActionsCount;
	
	LockStepManager lsm;
	
	public PendingActions (LockStepManager lsm) {
		this.lsm = lsm;
		
		CurrentActions = new Action[lsm.numberOfPlayers];
		NextActions = new Action[lsm.numberOfPlayers];
		NextNextActions = new Action[lsm.numberOfPlayers];
		NextNextNextActions = new Action[lsm.numberOfPlayers];
		
		currentActionsCount = 0;
		nextActionsCount = 0;
		nextNextActionsCount = 0;
		nextNextNextActionsCount = 0;
	}
	
	public void NextTurn() {
		//Finished processing this turns actions - clear it
		for(int i=0; i<CurrentActions.Length; i++) {
			CurrentActions[i] = null;
		}
		Action[] swap = CurrentActions;
		
		//last turn's actions is now this turn's actions
		CurrentActions = NextActions;
		currentActionsCount = nextActionsCount;
		
		//last turn's next next actions is now this turn's next actions
		NextActions = NextNextActions;
		nextActionsCount = nextNextActionsCount;
		
		NextNextActions = NextNextNextActions;
		nextNextActionsCount = nextNextNextActionsCount;
		
		//set NextNextNextActions to the empty list
		NextNextNextActions = swap;
		nextNextNextActionsCount = 0;
	}
	
	public void AddAction(Action action, int playerID, int currentLockStepTurn, int actionsLockStepTurn) {
		//add action for processing later
		if(actionsLockStepTurn == currentLockStepTurn + 1) {
			//if action is for next turn, add for processing 3 turns away
			if(NextNextNextActions[playerID] != null) {
				//TODO: Error Handling
				log.Debug ("WARNING!!!! Recieved multiple actions for player " + playerID + " for turn "  + actionsLockStepTurn);
			}
			NextNextNextActions[playerID] = action;
			nextNextNextActionsCount++;
		} else if(actionsLockStepTurn == currentLockStepTurn) {
			//if recieved action during our current turn
			//add for processing 2 turns away
			if(NextNextActions[playerID] != null) {
				//TODO: Error Handling
				log.Debug ("WARNING!!!! Recieved multiple actions for player " + playerID + " for turn "  + actionsLockStepTurn);
			}
			NextNextActions[playerID] = action;
			nextNextActionsCount++;
		} else if(actionsLockStepTurn == currentLockStepTurn - 1) {
			//if recieved action for last turn
			//add for processing 1 turn away
			if(NextActions[playerID] != null) {
				//TODO: Error Handling
				log.Debug ("WARNING!!!! Recieved multiple actions for player " + playerID + " for turn "  + actionsLockStepTurn);
			}
			NextActions[playerID] = action;
			nextActionsCount++;
		} else {
			//TODO: Error Handling
			log.Debug ("WARNING!!!! Unexpected lockstepID recieved : " + actionsLockStepTurn);
			return;
		}
	}
	
	public bool ReadyForNextTurn() {
		if(nextNextActionsCount == lsm.numberOfPlayers) {
			//if this is the 2nd turn, check if all the actions sent out on the 1st turn have been recieved
			if(lsm.LockStepTurnID == LockStepManager.FirstLockStepTurnID + 1) {
				return true;
			}
			
			//Check if all Actions that will be processed next turn have been recieved
			if(nextActionsCount == lsm.numberOfPlayers) {
				return true;
			}
		}
		
		//if this is the 1st turn, no actions had the chance to be recieved yet
		if(lsm.LockStepTurnID == LockStepManager.FirstLockStepTurnID) {
			return true;
		}
		//if none of the conditions have been met, return false
		return false;
	}
	
	public int[] WhosNotReady() {
		if(nextNextActionsCount == lsm.numberOfPlayers) {
			//if this is the 2nd turn, check if all the actions sent out on the 1st turn have been recieved
			if(lsm.LockStepTurnID == LockStepManager.FirstLockStepTurnID + 1) {
				return null;
			}
			
			//Check if all Actions that will be processed next turn have been recieved
			if(nextActionsCount == lsm.numberOfPlayers) {
				return null;
			}else {
				return WhosNotReady (NextActions, nextActionsCount);
			}
			
		} else if(lsm.LockStepTurnID == LockStepManager.FirstLockStepTurnID) {
			//if this is the 1st turn, no actions had the chance to be recieved yet
			return null;
		} else {
			return WhosNotReady (NextNextActions, nextNextActionsCount);
		}
	}
	
	private int[] WhosNotReady(Action[] actions, int count) {
		if(count < lsm.numberOfPlayers) {
			int[] notReadyPlayers = new int[lsm.numberOfPlayers - count];
			
			int index = 0;
			for(int playerID = 0; playerID < lsm.numberOfPlayers; playerID++) {
				if(actions[playerID] == null) {
					notReadyPlayers[index] = playerID;
					index++;
				}
			}
			
			return notReadyPlayers;
		} else {
			return null;
		}
	}
}