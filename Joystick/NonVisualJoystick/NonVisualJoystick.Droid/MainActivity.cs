using System;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Timers;
using System.Threading;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace NonVisualJoystick.Droid
{

	public class NetworkThread
	{
		public static void SendBytes(byte[] data, TcpClient client)
		{
			MyClass.SendData(client.GetStream(), data);
			Console.WriteLine(data[0]);
		}
	}

	[Activity (Label = "NonVisualJoystick.Droid", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity, View.IOnTouchListener
	{

		private Button _myButton;
		private Button _conClose;
		private float _viewX;
		private float _viewY;
		private int screenWidth;
		private int screenHeight;
		int xLoc;
		int yLoc;
		static string ip = null;
		int port;
		int droneID;
		private static Boolean check = false;
		System.Timers.Timer timer = new System.Timers.Timer(500);
		public static TcpClient client;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			ip = "192.168.1.12";
			client = new TcpClient(ip, 8500);
			NetworkStream s = client.GetStream();
			byte[] rawData = new byte[2];
			s.Read(rawData, 0, 2);
			droneID = (int)rawData[0];
			port = (int)rawData[1];
			port = port + 8000;
			client.Close();
			client = new TcpClient(ip, port);

			SetContentView(Resource.Layout.Main);
			FindViewById<TextView>(Resource.Id.port).Text = "Port Connection: " + port;

			_myButton = FindViewById<Button>(Resource.Id.myView);
			_myButton.SetOnTouchListener(this);

			_conClose = FindViewById<Button>(Resource.Id.conClose);

			_conClose.Click += delegate
			{
				byte[] data = new byte[10];
				data[0] = (byte)droneID;
				data[1] = 7;
				for(int i = 2; i < 10; i++)
				{
					data[i] = 0;
				}
				MyClass.SendData(client.GetStream(), data);
				client.Close();
			};

			var metrics = Resources.DisplayMetrics;
			screenWidth = metrics.WidthPixels;
			screenHeight = metrics.HeightPixels;
			timer.Enabled = true;
			timer.AutoReset = true;
			timer.Elapsed += OnTimedEvent;
			timer.Start();
		}

		public bool OnTouch(View v, MotionEvent e)
		{
			switch (e.Action)
			{
				case MotionEventActions.Down:
					_viewX = e.GetX();
					_viewY = e.GetY();
					xLoc = screenWidth / 2 - v.Width / 2;
					yLoc = screenHeight / 2 - v.Height / 2;
					try
					{
						byte[] data = new byte[3];
						data[0] = (byte)droneID;
						data[1] = 1;
						data[2] = 0;
					}
					catch
					{

					}
					break;
				case MotionEventActions.Up:
					v.Layout(xLoc, yLoc, xLoc + v.Width, yLoc + v.Height);
					try
					{
						byte[] data = new byte[3];
						data[0] = (byte)droneID;
						data[1] = 2;
						data[2] = 0;
					}
					catch
					{

					}
					break;
				case MotionEventActions.Move:
					var left = (int)(e.RawX - _viewX);
					var right = (int)(left + v.Width);
					var up = (int)(e.RawY - _viewY);
					var down = (int)(up + v.Height);
					v.Layout(left, up, right, down);
					double theta = calculateAngleFromPositions(xLoc, yLoc, left + v.Width / 2, up + v.Height / 2);
					FindViewById<TextView>(Resource.Id.Theta).Text = "Theta: " + theta + " Radians.";
					try
					{
						byte[] data = new byte[10];
						byte[] data2 = new byte[8];
						data[0] = (byte)droneID;
						data[1] = 4;
						data2 = BitConverter.GetBytes(theta);
						for (int i = 0; i < 8; i++)
						{
							data[i + 2] = data2[i];
						}
						if (!check)
						{
							check = true;
							ThreadPool.QueueUserWorkItem(state =>
							{
								NetworkThread.SendBytes(data, client);
							});
						}
					}catch
					{
						Console.WriteLine("Failed to send data!");
					}
					break;
			}
			return true;
		}

		private int ConvertPixelsToDp(float pixelValue)
		{
			var dp = (int)((pixelValue) / Resources.DisplayMetrics.Density);
			return dp;
		}

		private float calculateAngleFromPositions(int originX, int originY, int x, int y)
		{
			float theta = -1;
			if(x < originX)
			{
				if(y > originY)
				{
					float yComp = y - originY;
					float xComp = originX - x;
					theta = (float)Math.Atan(xComp / yComp) + (float)Math.PI;
				}
				else
				{
					float yComp = originY - y;
					float xComp = originX - x;
					theta = (float)Math.Atan(yComp / xComp) + (float)((Math.PI * 3) / 2);
				}
			}
			else
			{
				if(y < originY)
				{
					float yComp = originY - y;
					float xComp = x - originX;
					theta = (float)Math.Atan(xComp / yComp);
				}
				else
				{
					float yComp = y - originY;
					float xComp = x - originX;
					theta = (float)Math.Atan(yComp / xComp) + (float)Math.PI / 2;
				}
			}
			return theta;
		}

		static string NetworkGateway()
		{
			string ip = null;

			foreach (NetworkInterface f in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (f.OperationalStatus == OperationalStatus.Up)
				{
					foreach (GatewayIPAddressInformation d in f.GetIPProperties().GatewayAddresses)
					{
						ip = d.Address.ToString();
					}
				}
			}

			return ip;
		}

		static string getIp()
		{
			string gateway = NetworkGateway();
			string[] array = gateway.Split('.');
			string ping_var = array[0] + "." + array[1] + "." + array[2] + "." + 2;
			return ping_var;
		}

		private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
		{
			check = false;
		}
	}
}


