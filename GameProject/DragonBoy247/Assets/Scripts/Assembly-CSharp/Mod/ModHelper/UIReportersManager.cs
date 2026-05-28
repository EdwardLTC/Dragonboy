using System.Collections.Generic;

namespace Mod.ModHelper
{
	public delegate int Reporter(int y, mGraphics g);

	internal static class UIReportersManager
	{
		const byte MinY = 100;
		const byte ItemGap = 5;
		static readonly List<Reporter> reporters = new List<Reporter>();

		public static void AddReporter(Reporter reporter)
		{
			reporters.Add(reporter);
		}

		public static void RemoveReporter(Reporter reporter)
		{
			reporters.Remove(reporter);
		}

		public static void ClearReporters()
		{
			reporters.Clear();
		}

		public static void handlePaintGameScr(mGraphics g)
		{
			int lastY = MinY;

			foreach (Reporter reporter in reporters)
			{
				lastY += reporter(lastY, g) + ItemGap;
			}
		}
	}
}
