using System.Collections.Generic;

namespace Mod.Xmap.Edward
{
	public class MainXmapCL : IActionListener
	{
		static bool findNpc29to27;

		static MainXmapCL _Instance;

		static float lastMapChangeTime;
		static float lastErrorTime;

		static bool isEatChicken = true;
		static bool isUseCapsule = true;
		public static readonly bool teleDirect = true;

		static bool isHarvestPeans;

		static float lastTimeOpenedPanel;

		static int[] wayPointMapLeft = new int[2];
		static int[] wayPointMapCenter = new int[2];
		static int[] wayPointMapRight = new int[2];

		static MainXmapCL Instance => _Instance ??= new MainXmapCL();

		public void perform(int idAction, object p)
		{
			switch (idAction)
			{
			case 1: ShowPlanetMenu(); break;
			case 2: ToggleSetting(ref isEatChicken, "Ăn Đùi Gà", "AutoMapIsEatChicken"); break;
			case 3: ToggleSetting(ref isHarvestPeans, "Thu Đậu", "AutoMapIsHarvestPean"); break;
			case 4: ToggleSetting(ref isUseCapsule, "Sử Dụng Capsule", "AutoMapIsUseCsb"); break;
			// case 5: SaveData(); break;
			case 6: ShowMapsMenu((int[])p); break;
			}
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

		static void ToggleSetting(ref bool setting, string name, string rmsKey)
		{
			setting = !setting;
			string status = setting ? "[STATUS: ON]" : "[STATUS: OFF]";
			GameScr.info1.addInfo($"{name}\n{status}", 0);
			ShowMenu();
		}

		public static void ShowMenu()
		{
			MyVector myVector = new MyVector();

			myVector.addElement(new Command("Load Map", Instance, 1, null));
			// myVector.addElement(new Command($"Delay: {customMapDelay * 1000f} mili giây", Instance, 9, null));
			myVector.addElement(new Command($"Loại: {(teleDirect ? "Tele" : "Chạy bộ")}", Instance, 11, null));
			myVector.addElement(new Command($"Ăn Đùi Gà\n{(isEatChicken ? "[ON]" : "[OFF]")}", Instance, 2, null));
			myVector.addElement(new Command($"Thu Đậu\n{(isHarvestPeans ? "[ON]" : "[OFF]")}", Instance, 3, null));
			myVector.addElement(new Command($"Dùng Capsule\n{(isUseCapsule ? "[ON]" : "[OFF]")}", Instance, 4, null));
			myVector.addElement(new Command("Lưu cài đặt", Instance, 5, null));

			GameCanvas.menu.startAt(myVector, 3);
		}

		// [HotkeyCommand('x')]
		static void ShowPlanetMenu()
		{
			MyVector myVector = new MyVector();
			foreach (KeyValuePair<string, int[]> item in DataXmap.planetDictionary)
			{
				myVector.addElement(new Command(item.Key, Instance, 6, item.Value));
			}
			GameCanvas.menu.startAt(myVector, 3);
		}

		static void ShowMapsMenu(int[] mapIDs)
		{
			List<int> availableMaps = new List<int>();
			int cgender = Char.myCharz().cgender;

			foreach (int mapID in mapIDs)
			{
				if (IsMapValidForGender(mapID, cgender))
				{
					availableMaps.Add(mapID);
				}
			}

			if (availableMaps.Count == 0)
			{
				return;
			}

			EdwardXmapPanel.Show(availableMaps);
		}

		static bool IsMapValidForGender(int mapID, int gender)
		{
			if (gender == 0 && (mapID == 22 || mapID == 23)) return false;
			if (gender == 1 && (mapID == 21 || mapID == 23)) return false;
			if (gender == 2 && (mapID == 21 || mapID == 22)) return false;
			return true;
		}
	}
}
