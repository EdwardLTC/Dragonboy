using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace Mod
{
	internal static class ModDataStorage
	{
		static readonly string persistentDataPath = Application.persistentDataPath;

		internal static string PersistentDataPath => persistentDataPath;

		internal static readonly string DataPath = Path.Combine(GetRootDataPath(), "CommonModData");

		internal static readonly string PathAutoChat = Path.Combine(DataPath, "autochat.txt");
		internal static readonly string PathChatCommand = Path.Combine(DataPath, "chatCommands.json");
		internal static readonly string PathChatHistory = Path.Combine(DataPath, "chat.txt");
		internal static readonly string PathHotkeyCommand = Path.Combine(DataPath, "hotkeyCommands.json");

		internal static string GetRootDataPath()
		{
			string result = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Data");
			if (Application.isEditor || Application.platform == RuntimePlatform.Android)
				result = PersistentDataPath;
			return result;
		}

		internal static void EnsureDataDirectory()
		{
			EnsureDirectory(DataPath);
		}

		internal static void EnsureDirectory(string directoryPath)
		{
			if (!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath);
		}

		static void EnsureParentDirectory(string filePath)
		{
			string directoryPath = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrEmpty(directoryPath))
				EnsureDirectory(directoryPath);
		}

		internal static string ReadTextOrDefault(string path, string defaultValue = "")
		{
			EnsureParentDirectory(path);

			if (!File.Exists(path))
			{
				File.WriteAllText(path, defaultValue ?? string.Empty);
				return defaultValue ?? string.Empty;
			}

			string content = File.ReadAllText(path);
			if (string.IsNullOrWhiteSpace(content))
			{
				string fallback = defaultValue ?? string.Empty;
				File.WriteAllText(path, fallback);
				return fallback;
			}

			return content;
		}

		internal static string[] ReadLinesOrDefault(string path, string[] defaultLines)
		{
			EnsureParentDirectory(path);
			string[] fallback = defaultLines ?? Array.Empty<string>();

			if (!File.Exists(path))
			{
				File.WriteAllLines(path, fallback);
				return fallback;
			}

			string[] lines = File.ReadAllLines(path);
			if (lines.Length == 0 || (fallback.Length > 0 && lines.Length < fallback.Length))
			{
				File.WriteAllLines(path, fallback);
				return fallback;
			}

			return lines;
		}

		internal static T ReadJsonOrDefault<T>(string path, T defaultValue)
		{
			string defaultJson = JsonConvert.SerializeObject(defaultValue, Formatting.Indented);
			string content = ReadTextOrDefault(path, defaultJson);

			if (string.IsNullOrWhiteSpace(content))
				return defaultValue;

			try
			{
				return JsonConvert.DeserializeObject<T>(content) ?? defaultValue;
			}
			catch
			{
				File.WriteAllText(path, defaultJson);
				return defaultValue;
			}
		}

		internal static void WriteText(string path, string content)
		{
			EnsureParentDirectory(path);
			File.WriteAllText(path, content ?? string.Empty);
		}

		internal static void WriteLines(string path, string[] lines)
		{
			EnsureParentDirectory(path);
			File.WriteAllLines(path, lines ?? Array.Empty<string>());
		}

		internal static void WriteJson(string path, object value)
		{
			EnsureParentDirectory(path);
			File.WriteAllText(path, JsonConvert.SerializeObject(value));
		}

		internal static long LoadDataLong(string name, bool isCommon = true)
		{
			string path = GetDataDirectory(isCommon);
			EnsureDirectory(path);
			using FileStream fileStream = new FileStream(Path.Combine(path, name), FileMode.OpenOrCreate, FileAccess.Read);
			byte[] buffer = new byte[8];
			_ = fileStream.Read(buffer, 0, buffer.Length);
			return BitConverter.ToInt64(buffer, 0);
		}

		internal static bool LoadDataBool(string name, bool isCommon = true)
		{
			string path = GetDataDirectory(isCommon);
			EnsureDirectory(path);
			using FileStream fileStream = new FileStream(Path.Combine(path, name), FileMode.OpenOrCreate, FileAccess.Read);
			byte[] buffer = new byte[1];
			int read = fileStream.Read(buffer, 0, 1);
			return read > 0 && buffer[0] == 1;
		}

		internal static string LoadDataString(string name, bool isCommon = true)
		{
			string path = GetDataDirectory(isCommon);
			EnsureDirectory(path);
			using FileStream fileStream = new FileStream(Path.Combine(path, name), FileMode.OpenOrCreate, FileAccess.Read);
			using StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8);
			return streamReader.ReadToEnd();
		}

		internal static double LoadDataDouble(string name, bool isCommon = true)
		{
			string path = GetDataDirectory(isCommon);
			EnsureDirectory(path);
			using FileStream fileStream = new FileStream(Path.Combine(path, name), FileMode.OpenOrCreate, FileAccess.Read);
			byte[] buffer = new byte[8];
			_ = fileStream.Read(buffer, 0, buffer.Length);
			return BitConverter.ToDouble(buffer, 0);
		}

		internal static bool TryLoadDataLong(string name, out long value, bool isCommon = true)
		{
			value = default;
			try
			{
				value = LoadDataLong(name, isCommon);
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				return false;
			}
		}

		internal static bool TryLoadDataBool(string name, out bool value, bool isCommon = true)
		{
			value = default;
			try
			{
				value = LoadDataBool(name, isCommon);
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				return false;
			}
		}

		internal static bool TryLoadDataString(string name, out string value, bool isCommon = true)
		{
			value = default;
			try
			{
				value = LoadDataString(name, isCommon);
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				return false;
			}
		}

		internal static bool TryLoadDataDouble(string name, out double value, bool isCommon = true)
		{
			value = default;
			try
			{
				value = LoadDataDouble(name, isCommon);
				return true;
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				return false;
			}
		}

		internal static void SaveData(string name, long value, bool isCommon = true)
		{
			string path = GetDataDirectory(isCommon);
			EnsureDirectory(path);
			using FileStream fileStream = new FileStream(Path.Combine(path, name), FileMode.Create, FileAccess.Write);
			fileStream.Write(BitConverter.GetBytes(value), 0, 8);
		}

		internal static void SaveData(string name, bool status, bool isCommon = true)
		{
			string path = GetDataDirectory(isCommon);
			EnsureDirectory(path);
			using FileStream fileStream = new FileStream(Path.Combine(path, name), FileMode.Create, FileAccess.Write);
			fileStream.Write(new[]
			{
				(byte)(status ? 1 : 0)
			}, 0, 1);
		}

		internal static void SaveData(string name, string data, bool isCommon = true)
		{
			string path = GetDataDirectory(isCommon);
			EnsureDirectory(path);
			using FileStream fileStream = new FileStream(Path.Combine(path, name), FileMode.Create, FileAccess.Write);
			byte[] buffer = Encoding.UTF8.GetBytes(data ?? string.Empty);
			fileStream.Write(buffer, 0, buffer.Length);
		}

		internal static void SaveData(string name, double value, bool isCommon = true)
		{
			string path = GetDataDirectory(isCommon);
			EnsureDirectory(path);
			using FileStream fileStream = new FileStream(Path.Combine(path, name), FileMode.Create, FileAccess.Write);
			fileStream.Write(BitConverter.GetBytes(value), 0, 8);
		}

		static string GetDataDirectory(bool isCommon)
		{
			return isCommon
				? DataPath
				: Path.Combine(Rms.GetiPhoneDocumentsPath(), "ModData");
		}
	}
}
