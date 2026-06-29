using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mod.PickMob
{
	internal struct AStarHeapItem
	{
		public int F;
		public int G;
		public int Idx;
	}

	public static class MobPicker
	{
		const int ID_ICON_ITEM_TDLT = 4387;

		const int ASTAR_STEP_COST = 10;
		const int ASTAR_MAX_EXPANSIONS = 6000;

		static int[] astarBestG;
		static int[] astarVisitStamp;
		static int astarStampCounter = 1;
		static readonly List<AStarHeapItem> AstarHeap = new List<AStarHeapItem>(256);

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

			if (mob.levelBoss != 0 && Pk9rPickMob.IsNeSieuQuai && !ItemTime.isExistItem(ID_ICON_ITEM_TDLT))
			{
				return false;
			}

			return FilterMobTanSat(mob);
		}

		static bool IsMobNext(Mob mob)
		{
			if (mob.isMobMe || !FilterMobTanSat(mob))
				return false;

			if (!Pk9rPickMob.IsNeSieuQuai || ItemTime.isExistItem(ID_ICON_ITEM_TDLT) || mob.getTemplate().hp < 3000)
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
			if (Pk9rPickMob.IdMobsTanSat.Count != 0 && !Pk9rPickMob.IdMobsTanSat.Contains(mob.mobId))
				return false;

			if (Pk9rPickMob.TypeMobsTanSat.Count != 0 &&
			    !Pk9rPickMob.TypeMobsTanSat.Contains(mob.getTemplate().mobTemplateId))
				return false;

			return true;
		}

		static void EnsureAStarBuffers()
		{
			int n = TileMap.tmw * TileMap.tmh;
			if (n <= 0)
			{
				return;
			}

			if (astarBestG == null || astarBestG.Length < n)
			{
				astarBestG = new int[n];
				astarVisitStamp = new int[n];
			}
		}

		static int NextAStarStamp()
		{
			astarStampCounter++;
			if (astarStampCounter >= int.MaxValue - 1)
			{
				if (astarVisitStamp != null)
				{
					Array.Clear(astarVisitStamp, 0, astarVisitStamp.Length);
				}

				astarStampCounter = 1;
			}

			return astarStampCounter;
		}

		static int TileIndex(int tx, int ty)
		{
			return ty * TileMap.tmw + tx;
		}

		static bool IsWalkableTile(int tx, int ty)
		{
			if (tx < 1 || ty < 1 || tx >= TileMap.tmw - 1 || ty >= TileMap.tmh - 1)
			{
				return false;
			}

			return TileMap.tileTypeAt(tx * TileMap.size, ty * TileMap.size, 2);
		}

		static bool FindNearestWalkableTile(int tx, int ty, out int otx, out int oty, int maxRadius = 16)
		{
			otx = 0;
			oty = 0;
			if (TileMap.tmw <= 2 || TileMap.tmh <= 2)
				return false;

			tx = Mathf.Clamp(tx, 1, TileMap.tmw - 2);
			ty = Mathf.Clamp(ty, 1, TileMap.tmh - 2);
			if (IsWalkableTile(tx, ty))
			{
				otx = tx;
				oty = ty;
				return true;
			}

			for (int r = 1; r <= maxRadius; r++)
			{
				for (int dx = -r; dx <= r; dx++)
				{
					int[] dys =
					{
						-r, r
					};
					foreach (int dy in dys)
					{
						int nx = tx + dx;
						int ny = ty + dy;
						if (IsWalkableTile(nx, ny))
						{
							otx = nx;
							oty = ny;
							return true;
						}
					}
				}

				for (int dy = -r + 1; dy <= r - 1; dy++)
				{
					int[] xxs =
					{
						-r, r
					};
					foreach (int dx in xxs)
					{
						int nx = tx + dx;
						int ny = ty + dy;
						if (IsWalkableTile(nx, ny))
						{
							otx = nx;
							oty = ny;
							return true;
						}
					}
				}
			}

			return false;
		}

		static int HeuristicTiles(int tx, int ty, int gx, int gy)
		{
			return (Res.abs(tx - gx) + Res.abs(ty - gy)) * ASTAR_STEP_COST;
		}

		static void AstarHeapPush(int f, int g, int idx)
		{
			AStarHeapItem item = new AStarHeapItem
			{
				F = f,
				G = g,
				Idx = idx
			};
			AstarHeap.Add(item);
			int i = AstarHeap.Count - 1;
			while (i > 0)
			{
				int p = (i - 1) / 2;
				if (AstarHeap[p].F <= item.F)
					break;

				AstarHeap[i] = AstarHeap[p];
				i = p;
			}

			AstarHeap[i] = item;
		}

		static bool AstarHeapPop(out AStarHeapItem top)
		{
			if (AstarHeap.Count == 0)
			{
				top = default;
				return false;
			}

			top = AstarHeap[0];
			int last = AstarHeap.Count - 1;
			AStarHeapItem move = AstarHeap[last];
			AstarHeap.RemoveAt(last);
			if (AstarHeap.Count == 0)
				return true;

			AstarHeap[0] = move;
			int i = 0;
			while (true)
			{
				int l = i * 2 + 1;
				int r = l + 1;
				int smallest = i;
				if (l < AstarHeap.Count && AstarHeap[l].F < AstarHeap[smallest].F)
					smallest = l;

				if (r < AstarHeap.Count && AstarHeap[r].F < AstarHeap[smallest].F)
					smallest = r;

				if (smallest == i)
					break;

				(AstarHeap[i], AstarHeap[smallest]) = (AstarHeap[smallest], AstarHeap[i]);

				i = smallest;
			}

			return true;
		}

		static int GetBestG(int idx, int stamp)
		{
			return astarVisitStamp[idx] == stamp ? astarBestG[idx] : int.MaxValue;
		}

		static void SetBestG(int idx, int g, int stamp)
		{
			astarVisitStamp[idx] = stamp;
			astarBestG[idx] = g;
		}

		static int ComputeAStarTileDistance(int fromPx, int fromPy, int toPx, int toPy)
		{
			if (TileMap.tmw <= 2 || TileMap.tmh <= 2)
			{
				return int.MaxValue / 4;
			}

			EnsureAStarBuffers();

			if (astarBestG == null || astarBestG.Length < TileMap.tmw * TileMap.tmh)
			{
				return int.MaxValue / 4;
			}

			int stamp = NextAStarStamp();
			int stx = fromPx / TileMap.size;
			int sty = fromPy / TileMap.size;
			int gtx = toPx / TileMap.size;
			int gty = toPy / TileMap.size;

			if (!FindNearestWalkableTile(stx, sty, out stx, out sty))
			{
				return int.MaxValue / 4;
			}

			if (!FindNearestWalkableTile(gtx, gty, out gtx, out gty))
			{
				return int.MaxValue / 4;
			}

			int startIdx = TileIndex(stx, sty);
			int goalIdx = TileIndex(gtx, gty);
			if (startIdx == goalIdx)
			{
				return 0;
			}

			AstarHeap.Clear();
			SetBestG(startIdx, 0, stamp);
			AstarHeapPush(HeuristicTiles(stx, sty, gtx, gty), 0, startIdx);

			int expansions = 0;
			while (AstarHeap.Count > 0 && expansions < ASTAR_MAX_EXPANSIONS)
			{
				if (!AstarHeapPop(out AStarHeapItem cur))
					break;

				expansions++;
				if (cur.G > GetBestG(cur.Idx, stamp))
					continue;

				int ctx = cur.Idx % TileMap.tmw;
				int cty = cur.Idx / TileMap.tmw;
				if (cur.Idx == goalIdx)
					return cur.G;

				for (int k = 0; k < 4; k++)
				{
					int nx = ctx;
					int ny = cty;
					switch (k)
					{
					case 0:
						nx--;
						break;
					case 1:
						nx++;
						break;
					case 2:
						ny--;
						break;
					default:
						ny++;
						break;
					}

					if (!IsWalkableTile(nx, ny))
						continue;

					int nIdx = TileIndex(nx, ny);
					int tentative = cur.G + ASTAR_STEP_COST;
					if (tentative >= GetBestG(nIdx, stamp))
						continue;

					SetBestG(nIdx, tentative, stamp);
					int f = tentative + HeuristicTiles(nx, ny, gtx, gty);
					AstarHeapPush(f, tentative, nIdx);
				}
			}

			return int.MaxValue / 4;
		}

		static int GetPathCostToMobForPickNext(Char myChar, Mob mob)
		{
			int goalX = mob.xFirst - 24;
			int goalY = mob.yFirst;
			if (!Pk9rPickMob.IsVuotDiaHinh)
			{
				return Res.abs(myChar.cx - goalX) + Res.abs(myChar.cy - goalY);
			}

			int astar = ComputeAStarTileDistance(myChar.cx, myChar.cy, goalX, goalY);
			if (astar >= int.MaxValue / 8)
			{
				return Res.abs(myChar.cx - goalX) + Res.abs(myChar.cy - goalY) + 1_000_000;
			}

			return astar;
		}
	}
}
