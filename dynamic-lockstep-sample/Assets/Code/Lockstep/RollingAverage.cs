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
public class RollingAverage {
	
	public int[] currentValues; //used only for logging
	
	private int[] playerAverages;
	public RollingAverage(int numofPlayers, int initValue) {
		playerAverages = new int[numofPlayers];
		currentValues = new int[numofPlayers];
		for(int i=0; i<numofPlayers; i++) {
			playerAverages[i] = initValue;
			currentValues[i] = initValue;
		}
	}
	
	public void Add(int newValue, int playerID) {
		if(newValue > playerAverages[playerID]) {
			//rise quickly
			playerAverages[playerID] = newValue;
		} else {
			//slowly fall down
			playerAverages[playerID] = (playerAverages[playerID] * (9) + newValue * (1)) / 10;
		}
		
		currentValues[playerID] = newValue;
	}
	
	public int GetMax() {
		int max = int.MinValue;
		foreach(int average in playerAverages) {
			if(average > max) {
				max = average;
			}
		}
		
		return max;
	}
}
