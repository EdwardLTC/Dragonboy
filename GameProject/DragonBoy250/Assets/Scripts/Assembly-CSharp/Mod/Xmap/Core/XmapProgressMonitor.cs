using UnityEngine;

namespace Mod.Xmap
{
	internal sealed class XmapProgressMonitor
	{
		const float MaxStuckSeconds = 5f;
		const float MaxStuckInTransportScreenSeconds = 75f;

		int lastProgressMapId;
		float lastProgressRealtime;
		int lastProgressStepIndex;
		float? firstTimeInTransportScreen;

		internal void Reset(int mapId, int stepIndex)
		{
			lastProgressMapId = mapId;
			lastProgressStepIndex = stepIndex;
			lastProgressRealtime = Time.realtimeSinceStartup;
			firstTimeInTransportScreen = null;
		}

		internal void MarkProgress(int mapId, int stepIndex)
		{
			lastProgressRealtime = Time.realtimeSinceStartup;
			lastProgressMapId = mapId;
			lastProgressStepIndex = stepIndex;
			firstTimeInTransportScreen = null;
		}

		internal bool HasTimedOut(float now, int currentMapId, int stepIndex)
		{
			if (currentMapId != lastProgressMapId || stepIndex != lastProgressStepIndex)
			{
				MarkProgress(currentMapId, stepIndex);
				return false;
			}

			if (GameCanvas.currentScreen is TransportScr)
			{
				lastProgressRealtime = now;
				if (firstTimeInTransportScreen == null)
				{
					firstTimeInTransportScreen = now;
				}
				else if (now - firstTimeInTransportScreen >= MaxStuckInTransportScreenSeconds)
				{
					Service.gI().transportNow();
					firstTimeInTransportScreen = null;
				}

				return false;
			}

			return now - lastProgressRealtime >= MaxStuckSeconds;
		}
	}

}
