using System.Collections;
using System.Collections.Generic;
using Mod.ModHelper;
using UnityEngine;

namespace Mod.Xmap.Edward
{
	public class EdwardXmapController : CoroutineMainThreadAction<EdwardXmapController>
	{
		const float UPDATE_INTERVAL = 0.4f;
		const float ERROR_COOLDOWN = 1f;
		const float CHICKEN_PICKUP_DELAY = 0.6f;
		const float CAPSULE_PANEL_DELAY = 0.5f;
		const int CHICKEN_ITEM_ID = 74;
		const int CAPSULE_194_ID = 194;
		const int CAPSULE_193_ID = 193;
		const int TEMPORARY_MAP_999 = 999;
		const int MIN_PATH_LENGTH_FOR_CAPSULE = 4;
		static bool findNpc29to27;

		static int IdMapEnd;
		public static bool xmapErrr;
		static float lastWaitTime;

		static int lastProcessedMap = -1;
		static bool isProcessingMapChange;
		static float lastMapChangeTime;
		static float lastErrorTime;
		static float lastNpcIndexActionTime;

		static readonly bool isEatChicken = true;
		static readonly bool isUseCapsule = true;
		static readonly float customMapDelay = 0.5f;

		static bool isHarvestPeans;

		static bool isUsingCapsule;
		static bool isOpeningPanel;
		static float lastTimeOpenedPanel;

		static int[] wayPointMapLeft = new int[2];
		static int[] wayPointMapCenter = new int[2];
		static int[] wayPointMapRight = new int[2];

		protected override float Interval => 0.1f;

		protected override IEnumerator OnUpdate()
		{
			NextMap.UpdateConfirmNpc();

			Char me = Char.myCharz();
			float now = Time.realtimeSinceStartup;
			int currentMap = TileMap.mapID;

			if (HandleDeathState(me, now))
			{
				yield return null;
			}

			if (HandleDestinationReached(currentMap))
			{
				yield return null;
			}

			if (!ShouldContinueUpdate(now))
			{
				yield return null;
			}

			if (IsWaitingForNpcIndexDelay(now))
			{
				yield return null;
			}

			HandleMapChange(currentMap, now);

			if (isProcessingMapChange && now - lastMapChangeTime < customMapDelay)
			{
				yield return null;
			}

			if (!HandleFutureMapSpecialCase())
			{
				UpdateXmap(IdMapEnd);
			}
		}

		protected override void OnStart()
		{
			GameScr.info1.addInfo("Go to: " + TileMap.mapNames[IdMapEnd], 0);
			base.OnStart();
		}

		protected override void OnStop()
		{
			FinishXmap();
			base.OnStop();
		}

		public static void StartGoToMap(int mapID)
		{
			IdMapEnd = mapID;
			lastProcessedMap = -1;
			isProcessingMapChange = false;
			xmapErrr = false;

			if (gI == null)
				return;

			if (gI.IsActing)
				Finish();

			gI.Toggle(true);
		}

		static void Finish()
		{
			if (gI == null)
			{
				return;
			}

			gI.Toggle(false);
		}

		static void HandlePathNotFound(int mapID)
		{
			float now = Time.realtimeSinceStartup;
			if (now - lastErrorTime < ERROR_COOLDOWN)
				return;

			string msg = XmapPathfinder.GetInstance().GetPathErrorMessage(
				mapID,
				TileMap.mapID,
				Char.myCharz().cPower,
				Char.myCharz().taskMaint.taskId > 30
			);

			GameScr.info1.addInfo(msg, 0);
			lastErrorTime = now;
			xmapErrr = true;
		}

		static bool CheckClanRequirement(int[] path)
		{
			if (path == null || path.Length == 0) return true;
			if (TileMap.mapID != path[0]) return true;
			if (Char.ischangingMap || Controller.isStopReadMessage) return true;
			if (Char.myCharz().clan != null) return false;

			if (DataXmap.RequiresClan(IdMapEnd))
			{
				xmapErrr = true;
				return true;
			}

			return false;
		}

		static void LoadWaypointsInMap()
		{
			ResetSavedWaypoints();
			int count = TileMap.vGo.size();

			if (count != 2)
			{
				LoadMultipleWaypoints(count);
			}
			else
			{
				LoadTwoWaypoints();
			}
		}

		static void LoadMultipleWaypoints(int count)
		{
			for (int i = 0; i < count; i++)
			{
				Waypoint wp = (Waypoint)TileMap.vGo.elementAt(i);

				if (wp.maxX < 60)
				{
					wayPointMapLeft[0] = wp.minX + 15;
					wayPointMapLeft[1] = wp.maxY;
				}
				else if (wp.maxX > TileMap.pxw - 60)
				{
					wayPointMapRight[0] = wp.maxX - 15;
					wayPointMapRight[1] = wp.maxY;
				}
				else
				{
					wayPointMapCenter[0] = wp.minX + 15;
					wayPointMapCenter[1] = wp.maxY;
				}
			}
		}

		static void LoadTwoWaypoints()
		{
			Waypoint wp1 = (Waypoint)TileMap.vGo.elementAt(0);
			Waypoint wp2 = (Waypoint)TileMap.vGo.elementAt(1);

			bool bothLeft = wp1.maxX < 60 && wp2.maxX < 60;
			bool bothRight = wp1.minX > TileMap.pxw - 60 && wp2.minX > TileMap.pxw - 60;

			if (bothLeft || bothRight)
			{
				wayPointMapLeft[0] = wp1.minX + 15;
				wayPointMapLeft[1] = wp1.maxY;
				wayPointMapRight[0] = wp2.maxX - 15;
				wayPointMapRight[1] = wp2.maxY;
			}
			else if (wp1.maxX < wp2.maxX)
			{
				wayPointMapLeft[0] = wp1.minX + 15;
				wayPointMapLeft[1] = wp1.maxY;
				wayPointMapRight[0] = wp2.maxX - 15;
				wayPointMapRight[1] = wp2.maxY;
			}
			else
			{
				wayPointMapLeft[0] = wp2.minX + 15;
				wayPointMapLeft[1] = wp2.maxY;
				wayPointMapRight[0] = wp1.maxX - 15;
				wayPointMapRight[1] = wp1.maxY;
			}
		}

		static void ResetSavedWaypoints()
		{
			wayPointMapLeft = new int[2];
			wayPointMapCenter = new int[2];
			wayPointMapRight = new int[2];
		}

		static int GetYGround(int x)
		{
			int y = 50;
			int attempts = 0;

			while (attempts < 30)
			{
				attempts++;
				y += 24;

				if (TileMap.tileTypeAt(x, y, 2))
				{
					if (y % 24 != 0)
						y -= y % 24;
					break;
				}
			}

			return y;
		}

		static void TeleportTo(int x, int y)
		{
			Char me = Char.myCharz();
			me.cx = x;
			me.cy = y;
			Service.gI().charMove();

			if (!GameScr.canAutoPlay)
			{
				me.cy = y + 1;
				Service.gI().charMove();
				me.cy = y;
				Service.gI().charMove();
			}
		}

		public static void LoadMapLeft()
		{
			LoadMap(0);
		}

		public static void LoadMapCenter()
		{
			LoadMap(2);
		}

		public static void LoadMapRight()
		{
			LoadMap(1);
		}

		static void LoadMap(int position)
		{
			if (DataXmap.IsNRDMap(TileMap.mapID))
			{
				TeleportInNRDMap(position);
				return;
			}

			LoadWaypointsInMap();

			switch (position)
			{
			case 0:
				TeleportToPosition(wayPointMapLeft, 60);
				break;
			case 1:
				TeleportToPosition(wayPointMapRight, TileMap.pxw - 60);
				break;
			case 2:
				TeleportToPosition(wayPointMapCenter, TileMap.pxw / 2);
				break;
			}

			Service.gI().charMove();

			if (TileMap.mapID == 7 || TileMap.mapID == 14 || TileMap.mapID == 0)
				Service.gI().getMapOffline();
			else
				Service.gI().requestChangeMap();

			Char.ischangingMap = true;
		}

		static void TeleportToPosition(int[] waypoint, int defaultX)
		{
			if (waypoint[0] != 0 && waypoint[1] != 0)
				TeleportTo(waypoint[0], waypoint[1]);
			else
				TeleportTo(defaultX, GetYGround(defaultX));
		}


		static void TeleportInNRDMap(int position)
		{
			switch (position)
			{
			case 0:
				TeleportTo(60, GetYGround(60));
				break;
			case 1:
				TeleportTo(TileMap.pxw - 60, GetYGround(TileMap.pxw - 60));
				break;
			case 2:
				TeleportToNRDNpc();
				break;
			}
		}

		static void TeleportToNRDNpc()
		{
			for (int i = 0; i < GameScr.vNpc.size(); i++)
			{
				Npc npc = (Npc)GameScr.vNpc.elementAt(i);
				if (npc.template.npcTemplateId >= 30 && npc.template.npcTemplateId <= 36)
				{
					Char.myCharz().npcFocus = npc;
					TeleportTo(npc.cx, npc.cy - 3);
					break;
				}
			}
		}

		static bool HandleDeathState(Char me, float now)
		{
			if (!me.meDead) return false;

			lastWaitTime = now + 1f;
			if (gI.IsActing && GameCanvas.gameTick % 100 == 0)
			{
				Service.gI().returnTownFromDead();
			}

			return true;
		}

		static bool HandleDestinationReached(int currentMap)
		{
			if (currentMap != IdMapEnd) return false;
			FinishXmap();
			return true;
		}

		static bool ShouldContinueUpdate(float now)
		{
			if (TryEatChicken()) return false;
			if (!ShouldUpdateXmap(now)) return false;
			if (GameCanvas.isWait()) return false;
			return true;
		}

		static void HandleMapChange(int currentMap, float now)
		{
			if (currentMap != lastProcessedMap)
			{
				lastProcessedMap = currentMap;
				lastMapChangeTime = now;
				isProcessingMapChange = false;
			}
		}

		static bool ShouldUpdateXmap(float now)
		{
			if (!gI.IsActing) return false;
			if (now - lastWaitTime <= UPDATE_INTERVAL) return false;
			if (Char.ischangingMap || Controller.isStopReadMessage) return false;

			int mod = GameScr.canAutoPlay ? 15 : 35;
			return GameCanvas.gameTick % mod == 0;
		}

		static bool IsWaitingForNpcIndexDelay(float now)
		{
			if (lastNpcIndexActionTime > 0)
			{
				if (now - lastNpcIndexActionTime < customMapDelay + 1.2f)
				{
					return true;
				}
				// Reset sau khi delay đã qua
				lastNpcIndexActionTime = 0;
			}
			return false;
		}

		static bool TryEatChicken()
		{
			if (!ShouldTryEatChicken()) return false;

			float now = Time.realtimeSinceStartup;
			int size = GameScr.vItemMap.size();

			for (int i = 0; i < size; i++)
			{
				ItemMap itemMap = (ItemMap)GameScr.vItemMap.elementAt(i);
				if (IsChickenItem(itemMap))
				{
					PickupChicken(itemMap, now);
					return true;
				}
			}
			return false;
		}

		static bool ShouldTryEatChicken()
		{
			if (!isEatChicken) return false;
			int mapID = TileMap.mapID;
			return mapID == 21 || mapID == 22 || mapID == 23;
		}

		static bool IsChickenItem(ItemMap itemMap)
		{
			if (itemMap.template.id != CHICKEN_ITEM_ID) return false;
			int myCharID = Char.myCharz().charID;
			return itemMap.playerId == myCharID || itemMap.playerId == -1;
		}

		static void PickupChicken(ItemMap itemMap, float now)
		{
			Char.myCharz().itemFocus = itemMap;
			if (now - lastWaitTime > CHICKEN_PICKUP_DELAY)
			{
				lastWaitTime = now;
				Service.gI().pickItem(itemMap.itemMapID);
			}
		}

		static bool HandleFutureMapSpecialCase()
		{
			if (!DataXmap.IsFutureMap(IdMapEnd))
				return false;

			if (Char.myCharz().taskMaint.taskId <= 24)
			{
				xmapErrr = true;
				return true;
			}

			if (GameScr.findNPCInMap(38) != null)
			{
				findNpc29to27 = false;
				return false;
			}

			return ProcessFutureMapNavigation();
		}

		static bool ProcessFutureMapNavigation()
		{
			switch (TileMap.mapID)
			{
			case 27:
				UpdateXmap(28);
				findNpc29to27 = false;
				return true;

			case 28:
				UpdateXmap(findNpc29to27 ? 27 : 29);
				return true;

			case 29:
				findNpc29to27 = true;
				UpdateXmap(28);
				return true;

			default:
				return false;
			}
		}

		static void UpdateXmap(int mapID)
		{
			Char me = Char.myCharz();
			float now = Time.realtimeSinceStartup;

			SetupGenderPortal(me);

			int[] path = FindPathToDestination(me, mapID);

			if (path == null)
			{
				HandlePathNotFound(mapID);
				return;
			}

			if (TryUseCapsule(path)) return;
			if (CheckClanRequirement(path)) return;

			isProcessingMapChange = true;
			GotoNextMap(path[1]);
		}

		static void SetupGenderPortal(Char me)
		{
			if (!DataXmap.linkMaps.ContainsKey(TEMPORARY_MAP_999))
				DataXmap.linkMaps[TEMPORARY_MAP_999] = new List<NextMap>();

			List<NextMap> list = DataXmap.linkMaps[TEMPORARY_MAP_999];
			list.Clear();
			list.Add(new NextMap(24 + me.cgender, 10, "OK"));
		}

		static int[] FindPathToDestination(Char me, int mapID)
		{
			return XmapPathfinder.GetInstance().FindPath(
				mapID,
				TileMap.mapID,
				me.cPower,
				me.taskMaint.taskId > 30
			);
		}

		static void GotoNextMap(int nextMapID)
		{
			XmapPathfinder.GetInstance()
				.FindNextMapToGo(TileMap.mapID, nextMapID)
				?.GotoMap();
		}

		static bool TryUseCapsule(int[] path)
		{
			if (!isUseCapsule) return false;

			if (ShouldInitializeCapsule(path))
			{
				InitializeCapsuleUse();
				return true;
			}

			if (IsWaitingForPanel())
				return true;

			if (ShouldResetCapsuleState())
			{
				ResetCapsuleState();
				return true;
			}

			if (isUsingCapsule && !isOpeningPanel)
			{
				return TrySelectCapsuleDestination(path);
			}

			return false;
		}

		static bool ShouldInitializeCapsule(int[] path)
		{
			if (isUsingCapsule) return false;
			if (path.Length <= MIN_PATH_LENGTH_FOR_CAPSULE) return false;

			return Utils.getIndexItemBag(CAPSULE_194_ID, CAPSULE_193_ID) != -1;
		}

		static bool IsCapsuleItem(Item item)
		{
			return item.template.id == CAPSULE_194_ID ||
			       item.template.id == CAPSULE_193_ID && item.quantity > 1;
		}

		static void InitializeCapsuleUse()
		{
			Item capsule = FindCapsuleInBag();
			if (capsule == null) return;

			isUsingCapsule = true;
			isOpeningPanel = false;
			lastTimeOpenedPanel = Time.realtimeSinceStartup;
			GameCanvas.panel.mapNames = null;
			Service.gI().useItem(0, 1, -1, capsule.template.id);
		}

		static Item FindCapsuleInBag()
		{
			Item[] arrItemBag = Char.myCharz().arrItemBag;
			foreach (Item item in arrItemBag)
			{
				if (item != null && IsCapsuleItem(item))
					return item;
			}
			return null;
		}

		static bool IsWaitingForPanel()
		{
			return isUsingCapsule &&
			       !isOpeningPanel &&
			       Time.realtimeSinceStartup - lastTimeOpenedPanel < CAPSULE_PANEL_DELAY;
		}

		static bool ShouldResetCapsuleState()
		{
			return isUsingCapsule &&
			       !isOpeningPanel &&
			       GameCanvas.panel.mapNames == null;
		}

		static void ResetCapsuleState()
		{
			isUsingCapsule = false;
			isOpeningPanel = true;
		}

		static void FinishXmap()
		{
			isUsingCapsule = false;
			isOpeningPanel = false;
			xmapErrr = false;
			lastProcessedMap = -1;
			isProcessingMapChange = false;
			lastNpcIndexActionTime = 0;
		}

		public static void SetNpcIndexActionTime(float time)
		{
			lastNpcIndexActionTime = time;
		}

		static bool TrySelectCapsuleDestination(int[] path)
		{
			for (int i = path.Length - 1; i >= 1; i--)
			{
				string targetMapName = TileMap.mapNames[path[i]];

				for (int j = 0; j < GameCanvas.panel.mapNames.Length; j++)
				{
					if (GameCanvas.panel.mapNames[j].Contains(targetMapName))
					{
						isOpeningPanel = true;
						Service.gI().requestMapSelect(j);
						return true;
					}
				}
			}

			isOpeningPanel = true;
			return false;
		}
	}
}
