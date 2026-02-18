using Mod.ModHelper;
using Mod.ModHelper.CommandMod.Chat;
using Mod.R;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Mod.Xmap
{
	internal class XmapController : ThreadActionUpdate<XmapController>
	{
		internal override int Interval => 100;

		static int mapEnd;
		static List<MapNext> way;
		static int indexWay;
		static bool isNextMapFailed;

		protected override void update()
		{
			if (way == null)
			{
				if (!isNextMapFailed)
				{
					string mapName = TileMap.mapNames[mapEnd];
					MainThreadDispatcher.Dispatch(() => GameScr.info1.addInfo(Strings.goTo + ": " + mapName, 0));
				}

				XmapAlgorithm.xmapData = new XmapData();
				MainThreadDispatcher.Dispatch(XmapAlgorithm.xmapData.Load);
				while (!XmapAlgorithm.xmapData.isLoaded)
				{
					Thread.Sleep(100);
					XmapAlgorithm.xmapData.LoadLinkMapCapsule();
					try
					{
						way = XmapAlgorithm.findWay(TileMap.mapID, mapEnd);
					}
					catch (Exception ex)
					{
						Debug.LogError($"[xmap][error] Lỗi tìm đường: {ex}");
						MainThreadDispatcher.Dispatch(() => GameScr.info1.addInfo("Load map err" + '!', 0));
						finishXmap();
						return;
					}
					indexWay = 0;

					if (way == null)
					{
						MainThreadDispatcher.Dispatch(() => GameScr.info1.addInfo(Strings.xmapCantFindWay + '!', 0));
						finishXmap();
						return;
					}
				}
			}

			if (TileMap.mapID == way[way.Count - 1].to && !Char.myCharz().IsCharDead())
			{
				MainThreadDispatcher.Dispatch(() => GameScr.info1.addInfo(Strings.xmapDestinationReached + '!', 0));
				finishXmap();
				return;
			}

			if (TileMap.mapID == way[indexWay].mapStart)
			{
				if (Char.myCharz().IsCharDead())
				{
					Service.gI().returnTownFromDead();
					isNextMapFailed = true;
					way = null;
				}
				else if (Utils.CanNextMap())
				{
					MainThreadDispatcher.Dispatch(() => Pk9rXmap.NextMap(way[indexWay]));
				}
				Thread.Sleep(1000);
			}
			else if (TileMap.mapID == way[indexWay].to)
			{
				indexWay++;
			}
			else
			{
				isNextMapFailed = true;
				way = null;
			}
		}

		[ChatCommand("xmp")]
		internal static void start(int mapId)
		{
			if (gI.IsActing)
			{
				finishXmap();
				Debug.Log($"[xmap][info] Hủy xmap tới {TileMap.mapNames[mapEnd]} để thực hiện xmap mới");
			}
			mapEnd = mapId;
			gI.toggle(true);
			Debug.Log($"[xmap][info] Bắt đầu xmap tới {TileMap.mapNames[mapEnd]}");
		}

		internal static void finishXmap()
		{
			Debug.Log("[xmap][info] Kết thúc xmap");
			way = null;
			isNextMapFailed = false;
			gI.toggle(false);
		}
	}
}
