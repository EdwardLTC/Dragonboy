using Mod.PickMob.Pathfinding;

namespace Mod.PickMob
{
	public static class MobPicker
	{
		const int ID_ICON_ITEM_TDLT = 4387;

		public static bool AreAllTanSatMobsRespawning()
		{
			bool anyMatch = false;
			for (int i = 0; i < GameScr.vMob.size(); i++)
			{
				Mob mob = (Mob)GameScr.vMob.elementAt(i);
				if (mob.isMobMe || !FilterMobTanSat(mob))
					continue;

				anyMatch = true;
				if (IsMobTanSat(mob))
					return false;
			}

			return anyMatch;
		}

		public static bool HasAttackableMob()
		{
			for (int i = 0; i < GameScr.vMob.size(); i++)
			{
				Mob mob = (Mob)GameScr.vMob.elementAt(i);
				if (IsMobTanSat(mob))
					return true;
			}

			return false;
		}

		public static Mob GetMobTanSat()
		{
			Mob closest = null;
			int minDist = int.MaxValue;
			Char myChar = Char.myCharz();
			for (int i = 0; i < GameScr.vMob.size(); i++)
			{
				Mob mob = (Mob)GameScr.vMob.elementAt(i);
				int dx = mob.xFirst - myChar.cx;
				int dy = mob.yFirst - myChar.cy;
				int dist = dx * dx + dy * dy;
				if (IsMobTanSat(mob) && dist < minDist)
				{
					closest = mob;
					minDist = dist;
				}
			}

			return closest;
		}

		public static Mob GetMobNext()
		{
			Char myChar = Char.myCharz();
			Mob best = null;
			int bestPathCost = int.MaxValue;
			long bestLastDie = long.MaxValue;
			for (int i = 0; i < GameScr.vMob.size(); i++)
			{
				Mob mob = (Mob)GameScr.vMob.elementAt(i);
				if (!IsMobNext(mob))
					continue;

				int pathCost = GetPathCostToMobForPickNext(myChar, mob);
				if (pathCost < bestPathCost || pathCost == bestPathCost && mob.lastTimeDie < bestLastDie)
				{
					best = mob;
					bestPathCost = pathCost;
					bestLastDie = mob.lastTimeDie;
				}
			}

			return best;
		}

		public static bool IsMobTanSat(Mob mob)
		{
			if (mob.status == 0 || mob.status == 1 || mob.hp <= 0 || mob.isMobMe)
			{
				return false;
			}

			if (mob.levelBoss != 0 && PickMobConfig.IsNeSieuQuai && !ItemTime.isExistItem(ID_ICON_ITEM_TDLT))
			{
				return false;
			}

			if (mob.x < 0 || mob.x > TileMap.pxw || mob.y < 0 || mob.y > TileMap.pxh)
			{
				return false;
			}

			return FilterMobTanSat(mob);
		}

		static bool IsMobNext(Mob mob)
		{
			if (mob.isMobMe || !FilterMobTanSat(mob))
				return false;

			if (!PickMobConfig.IsNeSieuQuai || ItemTime.isExistItem(ID_ICON_ITEM_TDLT) || mob.getTemplate().hp < 3000)
				return true;

			if (mob.levelBoss != 0)
			{
				Mob mobNextSieuQuai = null;
				bool found = false;
				for (int i = 0; i < GameScr.vMob.size(); i++)
				{
					mobNextSieuQuai = (Mob)GameScr.vMob.elementAt(i);
					if (mobNextSieuQuai.countDie == 10 &&
					    (mobNextSieuQuai.status == 0 || mobNextSieuQuai.status == 1))
					{
						found = true;
						break;
					}
				}

				if (!found) return false;
				mob.lastTimeDie = mobNextSieuQuai.lastTimeDie;
			}
			else if (mob.countDie == 10 && (mob.status == 0 || mob.status == 1))
			{
				return false;
			}

			return true;
		}

		static bool FilterMobTanSat(Mob mob)
		{
			if (PickMobConfig.IdMobsTanSat.Count != 0 && !PickMobConfig.IdMobsTanSat.Contains(mob.mobId))
				return false;

			if (PickMobConfig.TypeMobsTanSat.Count != 0 && !PickMobConfig.TypeMobsTanSat.Contains(mob.getTemplate().mobTemplateId))
				return false;

			return true;
		}

		static int GetPathCostToMobForPickNext(Char myChar, Mob mob)
		{
			int goalX = mob.xFirst - 24;
			int goalY = mob.yFirst;

			int astar = AStarPathFinder.ComputeDistance(myChar.cx, myChar.cy, goalX, goalY);

			if (astar >= int.MaxValue / 8)
			{
				return Res.abs(myChar.cx - goalX) + Res.abs(myChar.cy - goalY) + 1_000_000;
			}

			return astar;
		}
	}
}
