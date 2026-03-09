using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mod.ModHelper
{
	public class GameLauncherClient
	{
		static readonly string pathLog = Path.Combine(Utils.dataPath, "log_launcher_client.txt");

		static GameLauncherClient _instance;
		public static GameLauncherClient Instance => _instance ??= new GameLauncherClient();

		ClientWebSocket _ws;
		CancellationTokenSource _cts;
		Thread _receiveThread;

		public bool IsConnected { get; private set; }
		bool _autoLoginDone;
		
		public string Username { get; private set; } = "";
		public string Password { get; private set; } = "";
		public int WsPort { get; private set; } = -1;
		
		public event Action<string, JObject> OnLauncherEvent;
		
		public void ParseStartupArgs()
		{
			try
			{
				string[] args = Environment.GetCommandLineArgs();
				for (int i = 0; i < args.Length; i++)
				{
					switch (args[i])
					{
						case "-port":
							if (i + 1 < args.Length) WsPort = int.Parse(args[++i]);
							break;
						case "-username":
							if (i + 1 < args.Length) Username = args[++i];
							break;
						case "-password":
							if (i + 1 < args.Length) Password = args[++i];
							break;
					}
				}
				
			}
			catch (Exception ex)
			{
				WriteLog("Error parsing startup args: " + ex);
			}
		}
		
		public bool IsLaunchedByLauncher()
		{
			return WsPort > 0;
		}
		
		public void Connect()
		{
			if (WsPort <= 0)
			{
				WriteLog("Cannot connect: wsPort not set.");
				return;
			}

			_cts = new CancellationTokenSource();
			_receiveThread = new Thread(ConnectAndListen)
			{
				Name = "LauncherWSClient",
				IsBackground = true
			};
			_receiveThread.Start();
		}

		void ConnectAndListen()
		{
			try
			{
				_ws = new ClientWebSocket();
				Uri uri = new Uri($"ws://127.0.0.1:{WsPort}");
				
				var sw = Stopwatch.StartNew();
				
				const int timeoutMs = 10_000;
				System.Threading.Tasks.Task connectTask = _ws.ConnectAsync(uri, _cts.Token);
				bool completed = connectTask.Wait(timeoutMs);
				sw.Stop();
				
				if (!completed)
				{
					WriteLog($"Connection timed out after {sw.ElapsedMilliseconds}ms. Launcher may not be running or not completing WebSocket handshake on port {WsPort}.");
					try { _ws.Abort(); }
					catch
					{
						// Ignore abort errors
					}
					try { _ws.Dispose(); }
					catch
					{
						// Ignore dispose errors
					}
					_ws = null;
					MainThreadDispatcher.Dispatch(() =>
					{
						GameCanvas.startOKDlg($"Không thể kết nối tới Launcher (timeout sau {sw.ElapsedMilliseconds / 1000}s).\nKiểm tra Launcher đang chạy WebSocket server trên port {WsPort}.");
					});
					return;
				}
				
				if (connectTask.IsFaulted)
				{
					throw connectTask.Exception?.InnerException ?? connectTask.Exception ?? new Exception("Unknown connection error");
				}
				
				IsConnected = true;
				
				SendMessage(new
				{
					action = "connected",
					id = Process.GetCurrentProcess().Id,
					username = Username
				});
				
				MainThreadDispatcher.Dispatch(() =>
				{
					Utils.IsOpenedByExternalAccountManager = true;
					Utils.username = Username;
					Utils.password = Password;
				});
				
				// Wait 5 seconds for the game to fully boot before auto-login (separate thread to not block receive loop)
				new Thread(() =>
				{
					Thread.Sleep(5000);
					MainThreadDispatcher.Dispatch(TryAutoLogin);
				}) { IsBackground = true }.Start();

				byte[] buffer = new byte[4096];
				while (!_cts.IsCancellationRequested && _ws.State == WebSocketState.Open)
				{
					try
					{
						var ms = new MemoryStream();
						WebSocketReceiveResult result;
						do
						{
							result = _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token).GetAwaiter().GetResult();
							if (result.MessageType == WebSocketMessageType.Close)
								break;
							ms.Write(buffer, 0, result.Count);
						} while (!result.EndOfMessage);

						if (result.MessageType == WebSocketMessageType.Close)
						{
							WriteLog("Launcher closed the WebSocket connection.");
							IsConnected = false;
							break;
						}

						byte[] raw = ms.ToArray();
						string message = Encoding.UTF8.GetString(raw);
						
						if (message.StartsWith("42"))
						{
							string json = message.Substring(2);

							JArray arr = JArray.Parse(json);

							string eventName = arr[0].ToString();
							JObject data = arr[1] as JObject;

							WriteLog($"Event: {eventName}");

							if (data != null)
							{
								MainThreadDispatcher.Dispatch(() => HandleMessage(data));
							}
						}
						else
						{
							WriteLog("Received non-42 message: " + message);
						}
					}
					catch (OperationCanceledException)
					{
						break;
					}
					catch (Exception ex)
					{
						WriteLog("Receive error: " + ex);
					}
				}
			}
			catch (Exception ex)
			{
				WriteLog("Connection error: " + ex);
				MainThreadDispatcher.Dispatch(() =>
				{
					GameCanvas.startOKDlg("Mất kết nối với Launcher");
				});
			}
			finally
			{
				IsConnected = false;
			}
		}
		
		void HandleMessage(JObject msg)
		{
			string action = (string)msg["action"];
			if (string.IsNullOrEmpty(action))
			{
				WriteLog("Received message with no action.");
				return;
			}

			WriteLog($"Received action: {action}");

			switch (action)
			{
				case "stop":
				{
					WriteLog("Launcher requested stop game.");
					Close();
					Application.Quit();
					break;
				}
				default:
				{
					WriteLog($"Unknown action: {action}");
					break;
				}
			}
			OnLauncherEvent?.Invoke(action, msg);
		}
		
		void SendCharacterInfo()
		{
			try
			{
				Char myChar = Char.myCharz();
				Char myPet = Char.myPetz();

				if (myChar == null || string.IsNullOrEmpty(myChar.cName))
				{
					return;
				}
				
				SendMessage(CharacterInfoMessage.Create(myChar, myPet));
			}
			catch (Exception ex)
			{
				WriteLog("Error sending char info: " + ex);
			}
		}
		
		/// <summary>
		/// If the current screen is ServerListScreen, auto-trigger login with the launcher's credentials.
		/// </summary>
		internal void TryAutoLogin()
		{
			if (_autoLoginDone)
				return;
			
			if (GameCanvas.currentScreen is ServerListScreen && GameCanvas.serverScreen != null)
			{
				_autoLoginDone = true;
				Rms.saveRMSString("acc", Utils.username);
				if (!string.IsNullOrEmpty(Utils.password))
				{
					Rms.saveRMSString("pass", Utils.password);
				}
				GameCanvas.serverScreen.perform(3, null);
				DelayedAction.ScheduleRepeating(1f, SendCharacterInfo);
			}
		}

		public void SendMessage(object obj)
		{
			if (_ws == null || _ws.State != WebSocketState.Open)
				return;

			try
			{
				string json = JsonConvert.SerializeObject(obj);
				byte[] bytes = Encoding.UTF8.GetBytes(json);
				_ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None)
					.GetAwaiter().GetResult();
			}
			catch (Exception ex)
			{
				WriteLog("Send error: " + ex);
			}
		}
		
		public void Close()
		{
			try
			{
				_cts?.Cancel();
				if (_ws != null && _ws.State == WebSocketState.Open)
				{
					SendMessage(new { action = "close-socket" });
					_ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None)
						.GetAwaiter().GetResult();
				}
				_ws?.Dispose();
				_ws = null;
				IsConnected = false;
				WriteLog("WebSocket connection closed.");
			}
			catch (Exception ex)
			{
				WriteLog("Error closing: " + ex);
			}
		}
		
		void WriteLog(string log)
		{
			try
			{
				File.AppendAllText(pathLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {log}\n");
			}
			catch
			{
				// Ignore logging errors
			}
		}
	}
}

