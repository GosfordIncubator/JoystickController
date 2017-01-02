using System;
using System.IO;
using System.Text;

namespace NonVisualJoystick
{
	public class MyClass
	{
		public static void SendData(Stream stream, byte[] data)
		{
			stream.Write(data, 0, data.Length);
			stream.Flush();
		}
	}
}

