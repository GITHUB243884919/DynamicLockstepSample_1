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

/// <summary>
/// 维护了两个数组，数组的长度是玩家的个数，一个是实际的，一个是Averages，但这个Averages也不是真正的Averages，详见http://jjyy.guru/unity3d-lock-step-part-2
/// 关于Averages：当新的>旧的，旧的等于新的;当新的<=旧的, playerAverages[playerID] = (playerAverages[playerID] * (9) + newValue * (1)) / 10;
/// 
/// 如果玩家多的话，这里需要用map来维护
/// 另外GetMax()是可以优化的，记录一个max值， 首次max=首次Add，以后每次Add检查是不是大于max。这样GetMax()就不用遍历
/// </summary>
public class RollingAverage 
{
	
	public int[] currentValues; //used only for logging
	
	private int[] playerAverages;
	public RollingAverage(int numofPlayers, int initValue) 
    {
		playerAverages = new int[numofPlayers];
		currentValues = new int[numofPlayers];
		for(int i=0; i<numofPlayers; i++) 
        {
			playerAverages[i] = initValue;
			currentValues[i] = initValue;
		}
	}
	
	public void Add(int newValue, int playerID) 
    {
		if(newValue > playerAverages[playerID]) 
        {
			//rise quickly160329

			playerAverages[playerID] = newValue;
		} 
        else 
        {
			//slowly fall down
			playerAverages[playerID] = (playerAverages[playerID] * (9) + newValue * (1)) / 10;
		}
		
		currentValues[playerID] = newValue;
	}
	
    /// <summary>
    /// 取最大的Averages
    /// </summary>
    /// <returns></returns>
	public int GetMax() 
    {
		int max = int.MinValue;
		foreach(int average in playerAverages) 
        {
			if(average > max) 
            {
				max = average;
			}
		}
		
		return max;
	}
}
