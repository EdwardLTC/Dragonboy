namespace Mod.Xmap
{
	internal static class XmapTiming
	{
		internal static float ServiceCallDelaySeconds => Utils.isUsingTDLT() ? 0.1f : 0.2f;
	}

}
