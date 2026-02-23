using System;
using System.Collections.Generic;
using Mod.ModHelper;
using Mod.R;
using UnityEngine;

namespace Mod.Xmap
{
	internal class XmapController : CoroutineMainThreadAction<XmapController>
	{

		static int mapEnd;
		static List<MapNext> way;
		static int indexWay;
		static bool isNextMapFailed;
		protected override float Interval => 1.5f;
		
		protected override void OnUpdate()
		{
			if (way == null)
			{
				if (!isNextMapFailed)
				{
					string mapName = TileMap.mapNames[mapEnd];
					GameScr.info1.addInfo(Strings.goTo + ": " + mapName, 0);
				}
				try
				{
					way = XmapAlgorithm.FindWayBFS(TileMap.mapID, mapEnd);
				}
				catch (Exception ex)
				{
					Debug.LogError($"[xmap][error] Lỗi tìm đường: {ex}");
					GameScr.info1.addInfo("Load map err" + '!', 0);
					finishXmap();
					return;
				}

				indexWay = 0;

				if (way == null)
				{
					GameScr.info1.addInfo(Strings.xmapCantFindWay + '!', 0);
					finishXmap();
					return;
				}

			}

			if (TileMap.mapID == way?[^1].to && !Char.myCharz().IsCharDead())
			{
				GameScr.info1.addInfo(Strings.xmapDestinationReached + '!', 0);
				finishXmap();
				return;
			}

			if (TileMap.mapID == way?[indexWay].mapStart)
			{
				if (Char.myCharz().IsCharDead())
				{
					Service.gI().returnTownFromDead();
					isNextMapFailed = true;
					way = null;
				}
				else if (Utils.CanNextMap())
				{
					Pk9rXmap.NextMap(way[indexWay]);
				}
			}
			else if (TileMap.mapID == way?[indexWay].to)
			{
				indexWay++;
			}
			else
			{
				isNextMapFailed = true;
				way = null;
			}
		}

		internal static void start(int mapId)
		{
			if (gI.IsActing)
			{
				finishXmap();
			}
			mapEnd = mapId;
			gI.Toggle(true);
		}

		internal static void finishXmap()
		{
			way = null;
			isNextMapFailed = false;
			gI.Toggle(false);
		}
	}
}
