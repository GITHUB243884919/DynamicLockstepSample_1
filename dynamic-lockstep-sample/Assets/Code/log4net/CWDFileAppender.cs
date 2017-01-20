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
using log4net.Appender;
using System.IO;

class CWDFileAppender : FileAppender
{
    public override string File
    {
        set
        {
			string path;
			if(Application.isEditor) {
				path = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
			} else {
				path = Path.Combine(Application.dataPath, "Logs");
				//uncomment for persistent storage of logs
				//path = Path.Combine(Application.persistentDataPath, "Logs");
			}
            base.File = Path.Combine(path, value);
        }
    }
}
