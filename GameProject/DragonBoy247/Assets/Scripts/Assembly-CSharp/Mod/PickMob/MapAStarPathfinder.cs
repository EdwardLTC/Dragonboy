using System.Collections.Generic;

namespace Mod.AutoTrain
{
	internal static class MapAStarPathfinder
	{
		const int GRID_STEP = 12;
		const int MAX_STEP_PX = 20;
		const int MAX_VERTICAL_PX = 200;
		const int MAX_ITERATIONS = 5000;
		const int GROUND_SCAN_STEP = 24;

		static long Key(int x, int y)
		{
			return (long)x << 32 | (uint)y;
		}

		static float Dist(int x1, int y1, int x2, int y2)
		{
			float dx = x1 - x2, dy = y1 - y2;
			return (float)System.Math.Sqrt(dx * dx + dy * dy);
		}

		static void CollectGroundYs(int x, List<int> buf)
		{
			buf.Clear();
			if (x < 0 || x >= TileMap.pxw) return;
			for (int y = GROUND_SCAN_STEP; y < TileMap.pxh; y += GROUND_SCAN_STEP)
				if (TileMap.tileTypeAt(x, y, 2))
					buf.Add(y);
		}

		static int NearestGroundY(int x, int refY)
		{
			int bestY = -1, bestD = int.MaxValue;
			for (int y = GROUND_SCAN_STEP; y < TileMap.pxh; y += GROUND_SCAN_STEP)
			{
				if (!TileMap.tileTypeAt(x, y, 2)) continue;
				int d = System.Math.Abs(y - refY);
				if (d < bestD)
				{
					bestD = d;
					bestY = y;
				}
			}
			return bestY;
		}

		/// <summary>
		///     Finds a terrain-aware path from (sx,sy) to (gx,gy) where each step &lt;= 20px.
		///     Returns null when no valid ground path exists.
		/// </summary>
		internal static List<int[]> FindPath(int sx, int sy, int gx, int gy)
		{
			sx = SnapToGrid(sx);
			gx = SnapToGrid(gx);

			if (!TileMap.tileTypeAt(sx, sy, 2))
			{
				int ny = NearestGroundY(sx, sy);
				if (ny < 0) return null;
				sy = ny;
			}
			if (!TileMap.tileTypeAt(gx, gy, 2))
			{
				int ny = NearestGroundY(gx, gy);
				if (ny < 0) return null;
				gy = ny;
			}

			if (Dist(sx, sy, gx, gy) <= MAX_STEP_PX)
				return new List<int[]>
				{
					new[]
					{
						sx, sy
					},
					new[]
					{
						gx, gy
					}
				};

			long startKey = Key(sx, sy);
			long goalKey = Key(gx, gy);

			SortedSet<(float f, int seq, long key)> open = new SortedSet<(float f, int seq, long key)>();
			Dictionary<long, float> gScore = new Dictionary<long, float>();
			Dictionary<long, long> parent = new Dictionary<long, long>();
			Dictionary<long, int[]> pos = new Dictionary<long, int[]>();
			HashSet<long> closed = new HashSet<long>();
			List<int> groundBuf = new List<int>();
			int seq = 0;

			gScore[startKey] = 0;
			pos[startKey] = new[]
			{
				sx, sy
			};
			pos[goalKey] = new[]
			{
				gx, gy
			};
			open.Add((Dist(sx, sy, gx, gy), seq++, startKey));

			int iter = 0;
			while (open.Count > 0 && iter++ < MAX_ITERATIONS)
			{
				(float f, int seq, long key) cur = open.Min;
				open.Remove(cur);
				long ck = cur.key;

				if (closed.Contains(ck)) continue;
				closed.Add(ck);

				if (ck == goalKey)
					return Reconstruct(parent, goalKey, pos);

				int[] cp = pos[ck];
				int cx = cp[0], cy = cp[1];

				float dGoal = Dist(cx, cy, gx, gy);
				if (dGoal <= MAX_STEP_PX)
				{
					float tg = gScore[ck] + dGoal;
					if (!gScore.ContainsKey(goalKey) || tg < gScore[goalKey])
					{
						gScore[goalKey] = tg;
						parent[goalKey] = ck;
						return Reconstruct(parent, goalKey, pos);
					}
				}

				for (int dxMul = -1; dxMul <= 1; dxMul++)
				{
					int nx = cx + dxMul * GRID_STEP;
					if (nx < 0 || nx >= TileMap.pxw) continue;

					CollectGroundYs(nx, groundBuf);
					foreach (int ny in groundBuf)
					{
						if (nx == cx && ny == cy) continue;

						float d = Dist(cx, cy, nx, ny);
						if (d < 0.5f) continue;
						if (dxMul != 0 ? d > MAX_VERTICAL_PX : d > MAX_STEP_PX) continue;

						long nk = Key(nx, ny);
						if (closed.Contains(nk)) continue;

						float tg = gScore[ck] + d;
						if (gScore.ContainsKey(nk) && tg >= gScore[nk]) continue;

						gScore[nk] = tg;
						parent[nk] = ck;
						pos[nk] = new[]
						{
							nx, ny
						};
						open.Add((tg + Dist(nx, ny, gx, gy), seq++, nk));
					}
				}
			}

			return null;
		}

		static List<int[]> Reconstruct(Dictionary<long, long> parent, long cur, Dictionary<long, int[]> pos)
		{
			List<int[]> path = new List<int[]>();
			while (parent.ContainsKey(cur))
			{
				path.Add(pos[cur]);
				cur = parent[cur];
			}
			path.Add(pos[cur]);
			path.Reverse();
			return path;
		}

		static int SnapToGrid(int v)
		{
			return v / GRID_STEP * GRID_STEP;
		}
	}
}
