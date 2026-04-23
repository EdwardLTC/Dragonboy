using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Editor
{
	public static class XcodePostProcess
	{
		[PostProcessBuild]
		public static void OnPostprocessBuild(BuildTarget target, string path)
		{
			if (target != BuildTarget.iOS) return;

			string projectPath = PBXProject.GetPBXProjectPath(path);
			PBXProject project = new PBXProject();
			project.ReadFromFile(projectPath);

			string targetGuid = project.GetUnityMainTargetGuid();

			project.SetBuildProperty(targetGuid, "CODE_SIGN_STYLE", "Automatic");

			project.WriteToFile(projectPath);
		}
	}
}
