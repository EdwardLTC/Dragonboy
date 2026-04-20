using System;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Mod
{
	internal static class ModStorage
	{
		static Mutex windowsSharedDataMutex;
		static readonly object windowsSharedDataMutexInit = new object();

		internal static string PersistentDataPath => Application.persistentDataPath;

		static bool UseWindowsCrossProcessFileLock =>
			Application.platform == RuntimePlatform.WindowsPlayer;

		static string SanitizeMutexSegment(string s)
		{
			if (string.IsNullOrEmpty(s))
				return "Game";
			char[] chars = s.ToCharArray();
			for (int i = 0; i < chars.Length; i++)
			{
				char c = chars[i];
				if (!char.IsLetterOrDigit(c) && c != '_' && c != '.' && c != '-')
					chars[i] = '_';
			}
			return new string(chars);
		}

		internal static void ExecuteWithCrossProcessDataLock(Action action)
		{
			if (!UseWindowsCrossProcessFileLock)
			{
				action();
				return;
			}
			if (windowsSharedDataMutex == null)
			{
				lock (windowsSharedDataMutexInit)
				{
					if (windowsSharedDataMutex == null)
					{
						string id = Application.identifier;
						if (string.IsNullOrEmpty(id))
							id = Application.companyName + "." + Application.productName;
						windowsSharedDataMutex = new Mutex(false, @"Local\" + SanitizeMutexSegment(id) + "_ModAndRmsData");
					}
				}
			}
			windowsSharedDataMutex.WaitOne();
			try
			{
				action();
			}
			finally
			{
				windowsSharedDataMutex.ReleaseMutex();
			}
		}

		internal static string RootDataPath
		{
			get
			{
				string baseDirectory = Path.GetDirectoryName(Application.dataPath) ?? Application.persistentDataPath;
				string result = Path.Combine(baseDirectory, "Data");
				if (Utils.IsEditor() || Utils.IsAndroidBuild())
				{
					result = PersistentDataPath;
				}
				return result;
			}
		}

		static string CommonDataPath => Path.Combine(RootDataPath, "CommonModData");

		internal static string GetCommonDataPath(params string[] segments)
		{
			if (segments == null || segments.Length == 0)
				return CommonDataPath;

			string[] parts = new string[segments.Length + 1];
			parts[0] = CommonDataPath;
			Array.Copy(segments, 0, parts, 1, segments.Length);
			return Path.Combine(parts);
		}

		internal static void EnsureCommonDataDirectory()
		{
			Directory.CreateDirectory(CommonDataPath);
		}

		static string GetStorageDirectory(bool isCommon)
		{
			return isCommon ? CommonDataPath : Path.Combine(Rms.GetiPhoneDocumentsPath(), "ModData");
		}

		static string GetFilePath(string name, bool isCommon)
		{
			return Path.Combine(GetStorageDirectory(isCommon), name);
		}

		static void EnsureStorageDirectory(bool isCommon)
		{
			Directory.CreateDirectory(GetStorageDirectory(isCommon));
		}

		internal static long ReadLong(string name, long defaultValue = 0, bool isCommon = true)
		{
			long result = defaultValue;
			ExecuteWithCrossProcessDataLock(() =>
			{
				try
				{
					string filePath = GetFilePath(name, isCommon);
					if (!File.Exists(filePath))
						return;
					byte[] buffer = File.ReadAllBytes(filePath);
					if (buffer.Length < sizeof( long ))
						return;
					result = BitConverter.ToInt64(buffer, 0);
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}
			});
			return result;
		}

		internal static bool ReadBool(string name, bool defaultValue = false, bool isCommon = true)
		{
			bool result = defaultValue;
			ExecuteWithCrossProcessDataLock(() =>
			{
				try
				{
					string filePath = GetFilePath(name, isCommon);
					if (!File.Exists(filePath))
						return;
					byte[] buffer = File.ReadAllBytes(filePath);
					if (buffer.Length < 1)
						return;
					result = buffer[0] == 1;
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}
			});
			return result;
		}

		internal static string ReadString(string name, string defaultValue = "", bool isCommon = true)
		{
			string result = defaultValue;
			ExecuteWithCrossProcessDataLock(() =>
			{
				try
				{
					string filePath = GetFilePath(name, isCommon);
					if (!File.Exists(filePath))
						return;
					string text = File.ReadAllText(filePath, Encoding.UTF8);
					if (!string.IsNullOrEmpty(text))
						result = text;
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}
			});
			return result;
		}

		internal static double ReadDouble(string name, double defaultValue = 0, bool isCommon = true)
		{
			double result = defaultValue;
			ExecuteWithCrossProcessDataLock(() =>
			{
				try
				{
					string filePath = GetFilePath(name, isCommon);
					if (!File.Exists(filePath))
						return;
					byte[] buffer = File.ReadAllBytes(filePath);
					if (buffer.Length < sizeof( double ))
						return;
					result = BitConverter.ToDouble(buffer, 0);
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}
			});
			return result;
		}

		internal static void WriteLong(string name, long value, bool isCommon = true)
		{
			ExecuteWithCrossProcessDataLock(() =>
			{
				EnsureStorageDirectory(isCommon);
				File.WriteAllBytes(GetFilePath(name, isCommon), BitConverter.GetBytes(value));
			});
		}

		internal static void WriteBool(string name, bool value, bool isCommon = true)
		{
			ExecuteWithCrossProcessDataLock(() =>
			{
				EnsureStorageDirectory(isCommon);
				File.WriteAllBytes(GetFilePath(name, isCommon), new[]
				{
					(byte)(value ? 1 : 0)
				});
			});
		}

		internal static void WriteInt(string name, int value, bool isCommon = true)
		{
			ExecuteWithCrossProcessDataLock(() =>
			{
				EnsureStorageDirectory(isCommon);
				File.WriteAllBytes(GetFilePath(name, isCommon), BitConverter.GetBytes(value));
			});
		}

		internal static int ReadInt(string name, int defaultValue, bool isCommon = true)
		{
			int result = defaultValue;
			ExecuteWithCrossProcessDataLock(() =>
			{
				try
				{
					string filePath = GetFilePath(name, isCommon);
					if (!File.Exists(filePath))
						return;
					byte[] buffer = File.ReadAllBytes(filePath);
					if (buffer.Length < sizeof( int ))
						return;
					result = BitConverter.ToInt32(buffer, 0);
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}
			});
			return result;
		}

		internal static void WriteString(string name, string data, bool isCommon = true)
		{
			ExecuteWithCrossProcessDataLock(() =>
			{
				EnsureStorageDirectory(isCommon);
				File.WriteAllText(GetFilePath(name, isCommon), data ?? string.Empty, Encoding.UTF8);
			});
		}

		internal static void WriteText(string filePath, string data)
		{
			ExecuteWithCrossProcessDataLock(() =>
			{
				string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
				if (!string.IsNullOrEmpty(directory))
					Directory.CreateDirectory(directory);
				File.WriteAllText(filePath, data ?? string.Empty, Encoding.UTF8);
			});
		}

		internal static string ReadText(string filePath, string defaultValue = "")
		{
			string result = defaultValue;
			ExecuteWithCrossProcessDataLock(() =>
			{
				try
				{
					if (!File.Exists(filePath))
						return;
					string text = File.ReadAllText(filePath, Encoding.UTF8);
					if (!string.IsNullOrEmpty(text))
						result = text;
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}
			});
			return result;
		}

		internal static void WriteDouble(string name, double value, bool isCommon = true)
		{
			ExecuteWithCrossProcessDataLock(() =>
			{
				EnsureStorageDirectory(isCommon);
				File.WriteAllBytes(GetFilePath(name, isCommon), BitConverter.GetBytes(value));
			});
		}
	}
}
