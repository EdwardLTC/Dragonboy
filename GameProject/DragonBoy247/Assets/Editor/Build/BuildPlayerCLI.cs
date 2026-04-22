using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DragonBoy.Build
{
	public static class BuildPlayerCLI
	{
		// Folder pattern:
		// Supports placeholders: {product}, {platform}, {version}, {bundleCode}, {date}, {time}, {datetime}, {unity}
		// Pattern can be absolute, or relative to the Unity project root.
		// Example (outside project): ../BuildArtifacts/{product}/{platform}/{version}_{datetime}
		const string DefaultFolderPattern = "../BuildArtifacts/{product}_{platform}";

		// macOS player: Unity writes a .app bundle; placing it under /Applications lets Finder run it like any Mac app.
		// Note: On a Mac host, writing here can require admin rights depending on ownership of /Applications.
		const string MacOsApplicationsFolderPattern = "/Applications";

		const string ArgBuildTarget = "-buildTarget";           // e.g. StandaloneWindows64, StandaloneOSX, iOS
		const string ArgOutputPattern = "-outputFolderPattern"; // e.g. BuildArtifacts/{product}/{platform}/{version}_{datetime}
		const string ArgBuildName = "-buildName";               // optional; defaults to product name
		const string ArgAndroidFormat = "-androidFormat";       // apk | aab

		[MenuItem("Tools/Build/Build Windows (64-bit)")]
		public static void BuildWindowsFromMenu()
		{
			BuildInternal(BuildTarget.StandaloneWindows64, DefaultFolderPattern, null, null);
		}

		[MenuItem("Tools/Build/Build macOS → Applications", true)]
		static bool ValidateBuildMacApplicationsFromMenu()
		{
			return Application.platform == RuntimePlatform.OSXEditor;
		}

		[MenuItem("Tools/Build/Build macOS → Applications")]
		public static void BuildMacApplicationsFromMenu()
		{
			BuildInternal(BuildTarget.StandaloneOSX, MacOsApplicationsFolderPattern, null, null);
		}

		[MenuItem("Tools/Build/Build iOS")]
		public static void BuildIosFromMenu()
		{
			BuildInternal(BuildTarget.iOS, DefaultFolderPattern, null, null);
		}

		// CLI entrypoint:
		// Windows: -buildTarget StandaloneWindows64
		// macOS: -buildTarget StandaloneOSX (default output folder: /Applications when Unity runs on macOS)
		// iOS: -buildTarget iOS
		// Android: -buildTarget Android -androidFormat aab
		// Unity -batchmode -quit -projectPath <path> -executeMethod DragonBoy.Build.BuildPlayerCLI.Build -buildTarget StandaloneWindows64 -outputFolderPattern "../BuildArtifacts/{product}_{platform}"
		public static void Build()
		{
			string[] args = Environment.GetCommandLineArgs();

			string targetName = GetArgValue(args, ArgBuildTarget);
			if (string.IsNullOrWhiteSpace(targetName))
			{
				Fail($"Missing required {ArgBuildTarget} (e.g. {ArgBuildTarget} StandaloneWindows64)");
				return;
			}

			if (!Enum.TryParse(targetName, true, out BuildTarget target))
			{
				Fail($"Invalid {ArgBuildTarget}: '{targetName}'");
				return;
			}

			string pattern = GetArgValue(args, ArgOutputPattern);
			if (string.IsNullOrWhiteSpace(pattern)) pattern = GetDefaultOutputFolderPattern(target);

			string buildName = GetArgValue(args, ArgBuildName);
			string androidFormat = GetArgValue(args, ArgAndroidFormat);

			BuildInternal(target, pattern, buildName, androidFormat);
		}

		/// <summary>
		///     Default output parent folder when <see cref="ArgOutputPattern" /> is omitted.
		///     macOS builds default to the system Applications folder only when Unity is running on macOS
		///     so <c>/Applications</c> resolves correctly; other hosts fall back to the artifact pattern.
		/// </summary>
		static string GetDefaultOutputFolderPattern(BuildTarget target)
		{
			if (target == BuildTarget.StandaloneOSX && Application.platform == RuntimePlatform.OSXEditor)
				return MacOsApplicationsFolderPattern;

			return DefaultFolderPattern;
		}

		static void BuildInternal(BuildTarget target, string outputFolderPattern, string buildName, string androidFormat)
		{
			try
			{
				string[] scenes = EditorBuildSettings.scenes
					.Where(s => s.enabled)
					.Select(s => s.path)
					.Where(p => !string.IsNullOrWhiteSpace(p))
					.ToArray();

				if (scenes.Length == 0)
				{
					Fail("No enabled scenes found in Build Settings. Enable at least one scene.");
					return;
				}

				if (target == BuildTarget.Android)
				{
					ApplyAndroidFormat(androidFormat);
				}

				string folderRel = ExpandPattern(outputFolderPattern, target);
				string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
				string folderAbs = Path.IsPathRooted(folderRel)
					? Path.GetFullPath(folderRel)
					: Path.GetFullPath(Path.Combine(projectRoot, folderRel));

				Directory.CreateDirectory(folderAbs);

				string name = string.IsNullOrWhiteSpace(buildName) ? PlayerSettings.productName : buildName.Trim();
				string location = Path.Combine(folderAbs, GetLocationFileName(target, name));

				BuildPlayerOptions options = new BuildPlayerOptions
				{
					scenes = scenes,
					target = target,
					locationPathName = location,
					options = BuildOptions.None
				};

				Debug.Log($"[Build] Target={target} OutputFolder='{folderAbs}' Location='{location}'");

				BuildReport report = BuildPipeline.BuildPlayer(options);
				if (report.summary.result != BuildResult.Succeeded)
				{
					Fail($"Build failed: {report.summary.result} (errors={report.summary.totalErrors})");
					return;
				}

				Debug.Log($"[Build] Success. Size={report.summary.totalSize} bytes Time={report.summary.totalTime}");
			}
			catch (Exception ex)
			{
				Fail(ex.ToString());
			}
		}

		static void ApplyAndroidFormat(string androidFormat)
		{
			if (string.IsNullOrWhiteSpace(androidFormat)) return;

			string v = androidFormat.Trim().ToLowerInvariant();
			switch (v)
			{
			case "apk":
				EditorUserBuildSettings.buildAppBundle = false;
				break;
			case "aab":
				EditorUserBuildSettings.buildAppBundle = true;
				break;
			default:
				Fail($"Invalid {ArgAndroidFormat}: '{androidFormat}' (use apk or aab)");
				break;
			}
		}

		static string GetLocationFileName(BuildTarget target, string baseName)
		{
			string safe = MakeFileNameSafe(baseName);

			switch (target)
			{
			case BuildTarget.Android:
				return safe + (EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk");
			case BuildTarget.StandaloneOSX:
				return safe + ".app";
			case BuildTarget.StandaloneWindows:
			case BuildTarget.StandaloneWindows64:
				return safe + ".exe";
			case BuildTarget.StandaloneLinux64:
				return safe; // Unity outputs an executable without extension on Linux
			default:
				// Many targets expect a folder or specific extension; default to baseName.
				return safe;
			}
		}

		static string ExpandPattern(string pattern, BuildTarget target)
		{
			DateTime now = DateTime.Now;
			Dictionary<string, string> dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				["product"] = PlayerSettings.productName,
				["platform"] = target.ToString(),
				["version"] = PlayerSettings.bundleVersion,
				["bundleCode"] = PlayerSettings.Android.bundleVersionCode.ToString(CultureInfo.InvariantCulture),
				["date"] = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
				["time"] = now.ToString("HHmmss", CultureInfo.InvariantCulture),
				["datetime"] = now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture),
				["unity"] = Application.unityVersion
			};

			string result = pattern;
			foreach (KeyValuePair<string, string> kv in dict)
			{
				result = result.Replace("{" + kv.Key + "}", MakePathSegmentSafe(kv.Value));
			}

			return result;
		}

		static string MakeFileNameSafe(string s)
		{
			if (string.IsNullOrWhiteSpace(s)) return "Build";
			foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
			return s.Trim();
		}

		static string MakePathSegmentSafe(string s)
		{
			if (string.IsNullOrWhiteSpace(s)) return "_";
			foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
			s = s.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');
			return s.Trim();
		}

		static string GetArgValue(string[] args, string key)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (!string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase)) continue;
				if (i + 1 >= args.Length) return null;
				return args[i + 1];
			}

			return null;
		}

		static void Fail(string message)
		{
			Debug.LogError("[Build] " + message);
			// Non-zero exit code so CI/scripts fail properly.
			EditorApplication.Exit(1);
		}
	}
}
