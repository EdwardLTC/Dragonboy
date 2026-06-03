using System;
using System.Collections;
using Mod.ModHelper;
using Mod.PickMob;
using Mod.Xmap;

namespace Mod.Auto
{
	public class AutoKillBossInNappa : CoroutineMainThreadAction<AutoKillBossInNappa>
	{

		const int MapTransitionDelayMs = 1200;
		const int ZoneAttackDelayMs = 1000;
		static readonly int[] targetMapIds =
		{
			68, 69, 70, 71, 72, 64, 65, 63, 66, 67, 73, 74, 75, 76, 77, 81, 82, 83, 79, 80
		};
		int currentMapIndex = -1;

		Char currentTarget;
		int currentZoneIndex;
		bool isChangingMap;
		long lastTransitionTime;
		int nextMapIndex = -1;
		int observedMapId = -1;
		int observedZoneId = -1;

		protected override float Interval => 0.2f;

		bool IsTargetValid(Char target)
		{
			return target != null
			       && !target.meDead
			       && target.cTypePk == 5
			       && target.cx >= 0
			       && target.cy >= 0
			       && target.cx <= TileMap.pxw
			       && target.cy <= TileMap.pxh;
		}

		static bool IsTargetMap(int mapId)
		{
			return Array.IndexOf(targetMapIds, mapId) >= 0;
		}

		static int GetMapIndex(int mapId)
		{
			return Array.IndexOf(targetMapIds, mapId);
		}

		static int GetMapId(int mapIndex)
		{
			if (mapIndex < 0 || mapIndex >= targetMapIds.Length)
			{
				mapIndex = 0;
			}

			return targetMapIds[mapIndex];
		}

		static int GetNextMapIndex(int currentMapIndex)
		{
			if (currentMapIndex < 0)
			{
				return 0;
			}

			return (currentMapIndex + 1) % targetMapIds.Length;
		}

		void ClearFocus()
		{
			Char.myCharz().mobFocus = null;
			Char.myCharz().npcFocus = null;
			Char.myCharz().itemFocus = null;
		}

		void AttackTarget(Char target)
		{
			ClearFocus();
			Char.myCharz().charFocus = target;

			Skill skill = SkillPicker.GetSkillAttack();
			if (skill == null || skill.paintCanNotUseSkill)
			{
				return;
			}

			GameScr.gI().doSelectSkill(skill, true);

			bool inRange = Utils.Distance(Char.myCharz(), target) <= 50 || System.Math.Abs(Char.myCharz().cx - target.cx) <= 70;
			if (!inRange)
			{
				Utils.TeleportMyChar(target.cx - 30, Utils.GetYGround(target.cx));
				return;
			}

			skill.lastTimeUseThisSkill = mSystem.currentTimeMillis();
			MyVector targets = new MyVector();
			targets.addElement(target);
			Service.gI().sendPlayerAttack(new MyVector(), targets, 2);
		}

		void SyncState(long now)
		{
			if (observedMapId != TileMap.mapID)
			{
				observedMapId = TileMap.mapID;
				currentMapIndex = GetMapIndex(TileMap.mapID);
				currentZoneIndex = 0;
				currentTarget = null;
				lastTransitionTime = now;
			}

			if (observedZoneId != TileMap.zoneID)
			{
				observedZoneId = TileMap.zoneID;
				currentTarget = null;
				lastTransitionTime = now;
			}

			if (isChangingMap && currentMapIndex == nextMapIndex)
			{
				isChangingMap = false;
				currentZoneIndex = 0;
				currentTarget = null;
				lastTransitionTime = now;
			}
		}

		void StartMapTransition(int mapIndex, long now)
		{
			int mapId = GetMapId(mapIndex);
			if (TileMap.mapID == mapId)
			{
				isChangingMap = false;
				currentMapIndex = mapIndex;
				currentZoneIndex = 0;
				currentTarget = null;
				lastTransitionTime = now;
				return;
			}

			nextMapIndex = mapIndex;
			isChangingMap = true;
			currentTarget = null;
			lastTransitionTime = now;
			if (!XmapController.gI.IsActing)
			{
				XmapController.start(mapId);
			}
		}

		void MoveToNextZoneOrMap(long now, int zoneCount)
		{
			currentTarget = null;
			currentZoneIndex++;
			lastTransitionTime = now;

			if (currentZoneIndex < zoneCount)
			{
				return;
			}

			currentZoneIndex = 0;
			StartMapTransition(GetNextMapIndex(currentMapIndex), now);
		}

		protected override IEnumerator OnUpdate()
		{
			if (Char.myCharz().meDead)
			{
				yield break;
			}

			long now = mSystem.currentTimeMillis();
			SyncState(now);

			if (isChangingMap)
			{
				if (!XmapController.gI.IsActing && now - lastTransitionTime >= MapTransitionDelayMs)
				{
					StartMapTransition(nextMapIndex, now);
				}

				yield break;
			}

			if (!IsTargetMap(TileMap.mapID))
			{
				if (now - lastTransitionTime >= MapTransitionDelayMs)
				{
					StartMapTransition(0, now);
				}

				yield break;
			}

			if (XmapController.gI.IsActing)
			{
				yield break;
			}

			int[] zones = GameScr.gI().zones;
			if (zones == null || zones.Length == 0)
			{
				yield break;
			}

			if (currentZoneIndex < 0 || currentZoneIndex >= zones.Length)
			{
				currentZoneIndex = 0;
			}

			int targetZoneId = zones[currentZoneIndex];
			if (TileMap.zoneID != targetZoneId)
			{
				if (now - lastTransitionTime >= MapTransitionDelayMs)
				{
					currentTarget = null;
					lastTransitionTime = now;
					Service.gI().requestChangeZone(targetZoneId, 0);
				}

				yield break;
			}

			if (currentTarget != null)
			{
				if (!IsTargetValid(currentTarget))
				{
					currentTarget = null;
				}
				else
				{
					AttackTarget(currentTarget);
					yield break;
				}
			}

			if (now - lastTransitionTime < ZoneAttackDelayMs)
			{
				yield break;
			}

			for (int i = 0; i < GameScr.vCharInMap.size(); i++)
			{
				Char obj = (Char)GameScr.vCharInMap.elementAt(i);
				if (IsTargetValid(obj) && obj.cHP > 0)
				{
					currentTarget = obj;
					AttackTarget(obj);
					break;
				}
			}

			if (currentTarget == null)
			{
				MoveToNextZoneOrMap(now, zones.Length);
			}
		}
	}
}
