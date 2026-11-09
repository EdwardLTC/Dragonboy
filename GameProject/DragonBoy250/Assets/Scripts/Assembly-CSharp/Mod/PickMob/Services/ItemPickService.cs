using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mod.PickMob.Services
{
	public enum TypePickItem
	{
		CanNotPickItem,
		PickItemNormal,
		PickItemTDLT,
		PickItemTanSat
	}

	public static class ItemPickService
	{
		const float PICK_ITEM_DELAY = 0.2f;
		const int ID_ICON_ITEM_TDLT = 4387;

		static int _lockedItemMapId = -1;
		static long _lockedUntilMs;

		static readonly Dictionary<int, bool> _eventItemCache = new Dictionary<int, bool>();

		public static IEnumerator TryPickItem(Char myChar)
		{
			if (TileMap.mapID == myChar.cgender + 21 && GameScr.vItemMap.size() > 0)
			{
				Service.gI().pickItem(((ItemMap)GameScr.vItemMap.elementAt(0)).itemMapID);
				return new WaitForSecondsRealtime(PICK_ITEM_DELAY);
			}

			ItemMap target = FindBestPickTarget(myChar);
			if (target != null)
			{
				TypePickItem type = GetTypePickItem(myChar, target);
				switch (type)
				{
				case TypePickItem.PickItemTDLT:
					LockTarget(target, 1200L);
					myChar.cx = target.xEnd;
					myChar.cy = target.yEnd;
					Service.gI().charMove();
					Service.gI().pickItem(target.itemMapID);
					target.countAutoPick++;
					return new WaitForSecondsRealtime(PICK_ITEM_DELAY);

				case TypePickItem.PickItemNormal:
					LockTarget(target, 1200L);
					MovementService.Move(myChar, target.xEnd, target.yEnd);
					Service.gI().pickItem(target.itemMapID);
					target.countAutoPick++;
					return new WaitForSecondsRealtime(PICK_ITEM_DELAY);

				case TypePickItem.PickItemTanSat:
					LockTarget(target, 2000L);
					MovementService.Move(myChar, target.xEnd, target.yEnd);
					myChar.mobFocus = null;
					return new WaitForSecondsRealtime(PICK_ITEM_DELAY);
				}
			}

			return null;
		}

		static void LockTarget(ItemMap target, long durationMs)
		{
			_lockedItemMapId = target.itemMapID;
			_lockedUntilMs = mSystem.currentTimeMillis() + durationMs;
		}

		static ItemMap FindBestPickTarget(Char myChar)
		{
			if (GameScr.vItemMap == null || GameScr.vItemMap.size() <= 0)
			{
				_lockedItemMapId = -1;
				return null;
			}

			long now = mSystem.currentTimeMillis();

			if (_lockedItemMapId != -1 && now < _lockedUntilMs)
			{
				for (int i = 0; i < GameScr.vItemMap.size(); i++)
				{
					ItemMap it = (ItemMap)GameScr.vItemMap.elementAt(i);
					if (it != null && it.itemMapID == _lockedItemMapId && GetTypePickItem(myChar, it) != TypePickItem.CanNotPickItem)
					{
						return it;
					}
				}
			}

			_lockedItemMapId = -1;

			ItemMap best = null;
			int bestScore = int.MaxValue;

			for (int i = 0; i < GameScr.vItemMap.size(); i++)
			{
				ItemMap it = (ItemMap)GameScr.vItemMap.elementAt(i);
				if (it == null)
				{
					continue;
				}

				TypePickItem type = GetTypePickItem(myChar, it);
				if (type == TypePickItem.CanNotPickItem)
				{
					continue;
				}

				int dx = Res.abs(myChar.cx - it.xEnd);
				int dy = Res.abs(myChar.cy - it.yEnd);
				int dist = dx + dy;

				int typeBias = type == TypePickItem.PickItemNormal ? -100000 : 0;
				int score = dist + typeBias;

				if (score < bestScore)
				{
					bestScore = score;
					best = it;
				}
			}

			if (best != null)
			{
				_lockedItemMapId = best.itemMapID;
				_lockedUntilMs = now + 1200L;
			}

			return best;
		}

		static TypePickItem GetTypePickItem(Char myChar, ItemMap itemMap)
		{
			if (PickMobConfig.IsItemMe && itemMap.playerId != myChar.charID && itemMap.playerId != -1)
				return TypePickItem.CanNotPickItem;

			if (PickMobConfig.IsLimitTimesPickItem && itemMap.countAutoPick > PickMobConfig.TimesAutoPickItemMax)
				return TypePickItem.CanNotPickItem;

			if (!FilterItemPick(itemMap))
				return TypePickItem.CanNotPickItem;

			if (PickMobConfig.IsSkipPickEventItems && IsEventItem(itemMap.template.id, itemMap.template.description))
				return TypePickItem.CanNotPickItem;

			if (Res.abs(myChar.cx - itemMap.xEnd) < 60 && Res.abs(myChar.cy - itemMap.yEnd) < 60)
				return TypePickItem.PickItemNormal;

			if (ItemTime.isExistItem(ID_ICON_ITEM_TDLT))
				return TypePickItem.PickItemTDLT;

			if (PickMobConfig.IsTanSat)
				return TypePickItem.PickItemTanSat;

			return TypePickItem.CanNotPickItem;
		}

		static bool IsEventItem(int templateId, string description)
		{
			if (!_eventItemCache.TryGetValue(templateId, out bool isEvent))
			{
				isEvent = description.Contains("Vật phẩm sự kiện");
				_eventItemCache[templateId] = isEvent;
			}

			return isEvent;
		}

		static bool FilterItemPick(ItemMap itemMap)
		{
			if (PickMobConfig.IdItemPicks.Count != 0 && !PickMobConfig.IdItemPicks.Contains(itemMap.template.id))
				return false;

			if (PickMobConfig.IdItemBlocks.Count != 0 && PickMobConfig.IdItemBlocks.Contains(itemMap.template.id))
				return false;

			if (PickMobConfig.TypeItemPicks.Count != 0 && !PickMobConfig.TypeItemPicks.Contains(itemMap.template.type))
				return false;

			if (PickMobConfig.TypeItemBlocks.Count != 0 && PickMobConfig.TypeItemBlocks.Contains(itemMap.template.type))
				return false;

			return true;
		}
	}
}
