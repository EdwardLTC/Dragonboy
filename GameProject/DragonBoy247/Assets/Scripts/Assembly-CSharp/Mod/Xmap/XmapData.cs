using System;
using System.Collections.Generic;

namespace Mod.Xmap
{
	internal static class XmapData
	{
		const int ID_MAP_SIEU_THI = 84;
		const int ID_MAP_TPVGT = 19;
		const int ID_MAP_TO_COLD = 109;
		internal static List<MapNext>[] links;
		internal static List<GroupMap> groups = new List<GroupMap>();

		static XmapData()
		{
			links = new List<MapNext>[TileMap.mapNames.Length];
			for (int i = 0; i < links.Length; i++)
				links[i] = new List<MapNext>();

			Load();
		}

		static void Load()
		{
			LoadLinksFromCode();
			LoadLinksAutoWaypointFromCode();
			AddLinksHome();
			LoadLinkSieuThi();
			LoadLinkToCold();
		}

		#region ================= MANUAL LINKS =================
		static void LoadLinksFromCode()
		{
			// ===== Trái Đất - Namec =====
			AddLink(24, 25, (TypeMapNext)1, 10, 0);
			AddLink(25, 24, (TypeMapNext)1, 11, 0);

			// ===== Trái Đất - Xayda =====
			AddLink(24, 26, (TypeMapNext)1, 10, 1);
			AddLink(26, 24, (TypeMapNext)1, 12, 0);

			// ===== Namec - Xayda =====
			AddLink(25, 26, (TypeMapNext)1, 11, 1);
			AddLink(26, 25, (TypeMapNext)1, 12, 1);

			// ===== Hành tinh -> Siêu thị =====
			AddLink(24, 84, (TypeMapNext)1, 10, 2);
			AddLink(25, 84, (TypeMapNext)1, 11, 2);
			AddLink(26, 84, (TypeMapNext)1, 12, 2);

			// ===== Tpvgt - Nappa =====
			AddLink(19, 68, (TypeMapNext)1, 12, 1);
			AddLink(68, 19, (TypeMapNext)1, 12, 0);

			// ===== Nappa -> Yadat =====
			AddLink(80, 131, (TypeMapNext)1, 60, 0);
			AddLink(131, 80, (TypeMapNext)1, 60, 1);

			// ===== Trái Đất - Tương lai =====
			AddLink(27, 102, (TypeMapNext)1, 38, 1);
			AddLink(28, 102, (TypeMapNext)1, 38, 1);
			AddLink(29, 102, (TypeMapNext)1, 38, 1);
			AddLink(102, 24, (TypeMapNext)1, 38, 1);

			// ===== Thành phố Vegeta - Thành phố Santa =====
			AddLink(19, 126, (TypeMapNext)1, 53, 0);
			AddLink(126, 19, (TypeMapNext)1, 53, 0);

			// ===== Trái Đất - Hành tinh Potaufeu =====
			AddLink(24, 139, (TypeMapNext)1, 63, 0);
			AddLink(139, 24, (TypeMapNext)1, 63, 0);

			// ===== Hành tinh Potaufeu - các hành tinh còn lại =====
			AddLink(139, 25, (TypeMapNext)1, 63, 1);
			AddLink(139, 26, (TypeMapNext)1, 63, 2);

			// ===== Rừng Bamboo - Tường thành 1 =====
			AddLink(27, 53, (TypeMapNext)1, 25, 0);

			// ===== Thần điện - Hành tinh Kaio =====
			AddLink(45, 48, (TypeMapNext)1, 19, 3);
			AddLink(48, 45, (TypeMapNext)1, 20, 3, 0);

			// ===== Thánh địa Kaio - Hành tinh Kaio =====
			AddLink(50, 48, (TypeMapNext)1, 44, 0);
			AddLink(48, 50, (TypeMapNext)1, 20, 3, 1);

			// ===== Trái Đất - Khí Gas =====
			AddLink(0, 149, (TypeMapNext)1, 67, 3, 0);

			// ===== Link đặc biệt type 3 =====
			AddLink(45, 46, (TypeMapNext)3, 576, 552);
			AddLink(46, 47, (TypeMapNext)3, 576, 552);
		}
		#endregion

		static void AddLink(int mapStart, int to, TypeMapNext type, params int[] info)
		{
			links[mapStart].Add(new MapNext(mapStart, to, type, info));
		}

		#region ================= GROUP MAPS =================
		internal static void LoadGroupMaps()
		{
			groups.Clear();
			LoadGroupMapsFromCode();
			RemoveMapsHomeInGroupMaps();
		}

		static void LoadGroupMapsFromCode()
		{
			groups.Clear();

			groups.Add(new GroupMap(
				new[]
				{
					"Xayda", "Saiya"
				},
				new List<int>
				{
					44,
					23,
					14,
					15,
					16,
					17,
					18,
					20,
					19,
					35,
					36,
					37,
					38,
					26,
					52,
					84
				}
			));

			groups.Add(new GroupMap(
				new[]
				{
					"Namec", "Namek"
				},
				new List<int>
				{
					43,
					22,
					7,
					8,
					9,
					11,
					12,
					13,
					10,
					31,
					32,
					33,
					34,
					25
				}
			));

			groups.Add(new GroupMap(
				new[]
				{
					"Trái đất", "Earth", "Bumi"
				},
				new List<int>
				{
					42,
					21,
					0,
					1,
					2,
					3,
					4,
					5,
					6,
					27,
					28,
					29,
					30,
					47,
					46,
					45,
					48,
					50,
					111,
					24
				}
			));

			groups.Add(new GroupMap(
				new[]
				{
					"Nappa"
				},
				new List<int>
				{
					68,
					69,
					70,
					71,
					72,
					64,
					65,
					63,
					66,
					67,
					73,
					74,
					75,
					76,
					77,
					81,
					82,
					83,
					79,
					80
				}
			));

			groups.Add(new GroupMap(
				new[]
				{
					"Yardrat"
				},
				new List<int>
				{
					131,
					132,
					133
				}
			));

			groups.Add(new GroupMap(
				new[]
				{
					"Tương lai", "Future"
				},
				new List<int>
				{
					102,
					92,
					93,
					94,
					96,
					97,
					98,
					99,
					100,
					103
				}
			));

			groups.Add(new GroupMap(
				new[]
				{
					"Cold"
				},
				new List<int>
				{
					109,
					108,
					107,
					110,
					106,
					105
				}
			));

			groups.Add(new GroupMap(
				new[]
				{
					"Potaufeu"
				},
				new List<int>
				{
					139,
					140
				}
			));

			groups.Add(new GroupMap(
				new[]
				{
					"Doanh trại", "Barracks"
				},
				new List<int>
				{
					53,
					58,
					59,
					60,
					61,
					62,
					55,
					56,
					54,
					57
				}
			));

			groups.Add(new GroupMap(
				new[]
				{
					"Khí Gas", "Gas"
				},
				new List<int>
				{
					149,
					147,
					152,
					151,
					148
				}
			));
		}

		static void RemoveMapsHomeInGroupMaps()
		{
			foreach (GroupMap groupMap in groups)
			{
				switch (Char.myCharz().cgender)
				{
				case 0:
					groupMap.maps.Remove(22);
					groupMap.maps.Remove(23);
					break;
				case 1:
					groupMap.maps.Remove(21);
					groupMap.maps.Remove(23);
					break;
				default:
					groupMap.maps.Remove(21);
					groupMap.maps.Remove(22);
					break;
				}
			}
		}
		#endregion

		#region ================= AUTO WAYPOINT =================
		static void LoadLinksAutoWaypointFromCode()
		{
			// ================= TRÁI ĐẤT =================
			AddAutoWaypointChain(42, 0, 1, 2, 3, 4, 5, 6);
			AddAutoWaypointChain(3, 27, 28, 29, 30);
			AddAutoWaypointChain(2, 24);
			AddAutoWaypointChain(1, 47);
			AddAutoWaypointChain(5, 29);
			AddAutoWaypointChain(47, 111);
			AddAutoWaypointChain(47, 46, 45);

			// ================= NAMEC =================
			AddAutoWaypointChain(43, 7, 8, 9, 11, 12, 13, 10);
			AddAutoWaypointChain(11, 31, 32, 33, 34);
			AddAutoWaypointChain(9, 25);
			AddAutoWaypointChain(13, 33);

			// ================= XAYDA =================
			AddAutoWaypointChain(52, 44, 14, 15, 16, 17, 18, 20, 19);
			AddAutoWaypointChain(17, 35, 36, 37, 38);
			AddAutoWaypointChain(16, 26);
			AddAutoWaypointChain(20, 37);

			// ================= NAPPA =================
			AddAutoWaypointChain(
				68, 69, 70, 71, 72,
				64, 65, 63, 66, 67,
				73, 74, 75, 76, 77,
				81, 82, 83, 79, 80
			);

			// ================= TƯƠNG LAI =================
			AddAutoWaypointChain(102, 92, 93, 94, 96, 97, 98, 99, 100, 103);

			// ================= COLD =================
			AddAutoWaypointChain(109, 108, 107, 110, 106);
			AddAutoWaypointChain(109, 105);
			AddAutoWaypointChain(109, 106);
			AddAutoWaypointChain(106, 107);
			AddAutoWaypointChain(108, 105);

			// ================= YARDAT =================
			AddAutoWaypointChain(131, 132, 133);

			// ================= NAPPA - COLD =================
			AddAutoWaypointChain(80, 105);

			// ================= POTA UFEU =================
			AddAutoWaypointChain(139, 140);

			// ================= DOANH TRẠI =================
			AddAutoWaypointChain(53, 58, 59, 60, 61, 62, 55, 56, 54, 57);
			AddAutoWaypointChain(53, 27);

			// ================= KHÍ GAS =================
			AddAutoWaypointChain(149, 147, 152, 151, 148);
		}

		static void AddAutoWaypointChain(params int[] maps)
		{
			for (int i = 0; i < maps.Length; i++)
			{
				int mapStart = maps[i];

				if (i != 0)
				{
					links[mapStart].Add(new MapNext(mapStart, maps[i - 1], TypeMapNext.AutoWaypoint, Array.Empty<int>()));
				}

				if (i != maps.Length - 1)
				{
					links[mapStart].Add(new MapNext(mapStart, maps[i + 1], TypeMapNext.AutoWaypoint, Array.Empty<int>()));
				}
			}
		}
		#endregion

		#region ================= SPECIAL LINKS =================
		static void AddLinksHome()
		{
			int cgender = Char.myCharz().cgender;
			int mapHome = XmapUtils.getIdMapHome(cgender);
			int mapLang = XmapUtils.getIdMapLang(cgender);

			AddLink(mapHome, mapLang, TypeMapNext.AutoWaypoint);
			AddLink(mapLang, mapHome, TypeMapNext.AutoWaypoint);
		}

		static void LoadLinkSieuThi()
		{
			const int npcId = 10;
			const int select = 0;

			int offset = Char.myCharz().cgender;
			int mapTTVT = XmapUtils.ID_MAP_TTVT_BASE + offset;

			AddLink(ID_MAP_SIEU_THI, mapTTVT, TypeMapNext.NpcMenu, npcId, select);
		}

		static void LoadLinkToCold()
		{
			if (Char.myCharz().taskMaint.taskId <= 30)
				return;

			const int npcId = 12;
			const int select = 0;

			AddLink(ID_MAP_TPVGT, ID_MAP_TO_COLD,
				TypeMapNext.NpcMenu,
				npcId, select);
		}
		#endregion
	}

}
