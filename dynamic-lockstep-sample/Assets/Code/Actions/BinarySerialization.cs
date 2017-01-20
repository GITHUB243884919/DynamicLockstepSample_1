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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class BinarySerialization
{
	public static string SerializeObjectToString(object o)
	{
		byte[] byteArray = SerializeObjectToByteArray(o);
		if(byteArray != null) {
			return Convert.ToBase64String(byteArray);
		} else {
			return null;
		}
	}
	
	public static byte[] SerializeObjectToByteArray(object o) {
		if (!o.GetType().IsSerializable)
	    {
	        return null;
	    }
	
	    using (MemoryStream stream = new MemoryStream())
	    {
	        new BinaryFormatter().Serialize(stream, o);
	        return stream.ToArray();
	    }
	}
	
	public static object DeserializeObject(string str)
	{
	    byte[] bytes = Convert.FromBase64String(str);
	    return DeserializeObject(bytes);
	}
	
	public static object DeserializeObject(byte[] byteArray) {
	    using (MemoryStream stream = new MemoryStream(byteArray))
	    {
	        return new BinaryFormatter().Deserialize(stream);
	    }
	}
	
	public static T DeserializeObject<T>(byte[] byteArray) where T:class {
		object o = DeserializeObject(byteArray);
		return o as T;
	}
	
	public static T DeserializeObject<T>(string str) where T:class {
		object o = DeserializeObject(str);
		return o as T;
	}
}