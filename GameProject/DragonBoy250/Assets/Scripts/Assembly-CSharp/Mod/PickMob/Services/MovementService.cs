namespace Mod.PickMob.Services
{
	public static class MovementService
	{
		const int ID_ICON_ITEM_TDLT = 4387;

		static int[] _groundYCache;
		static int _groundYCacheMapId = -1;

		public static void Move(Char myChar, int x, int y)
		{
			int[] vs = GetPointYsdMax(myChar.cx, x);
			if (vs[1] >= y || vs[1] >= myChar.cy && (myChar.statusMe == 2 || myChar.statusMe == 1))
			{
				vs[0] = x;
				vs[1] = y;
			}

			if (ItemTime.isExistItem(ID_ICON_ITEM_TDLT))
			{
				Utils.TeleportMyChar(vs[0], vs[1]);
				return;
			}

			myChar.currentMovePoint = new MovePoint(vs[0], vs[1]);
		}

		public static void EnsureGroundCache()
		{
			if (_groundYCache != null && _groundYCacheMapId == TileMap.mapID)
			{
				return;
			}

			int cols = TileMap.pxw / 24 + 2;
			_groundYCache = new int[cols];

			for (int xi = 0; xi < cols; xi++)
			{
				int x = xi * 24;
				_groundYCache[xi] = ComputeGroundY(x);
			}

			_groundYCacheMapId = TileMap.mapID;
		}

		static int ComputeGroundY(int x)
		{
			int dmin = TileMap.pxh;
			int ysdBest = -1;
			int myCharY = Char.myCharz().cy;

			for (int i = 24; i < TileMap.pxh; i += 24)
			{
				if (!TileMap.tileTypeAt(x, i, 2))
					continue;

				int d = Res.abs(i - myCharY);
				if (d < dmin)
				{
					dmin = d;
					ysdBest = i;
				}
			}

			return ysdBest;
		}

		static int GetYsd(int xsd)
		{
			int xi = xsd / 24;
			if (_groundYCache == null || xi < 0 || xi >= _groundYCache.Length)
			{
				return ComputeGroundY(xsd);
			}

			return _groundYCache[xi];
		}

		static int[] GetPointYsdMax(int xStart, int xEnd)
		{
			int ysdMin = TileMap.pxh;
			int x = -1;

			if (xStart > xEnd)
			{
				for (int i = xEnd; i < xStart; i += 24)
				{
					int ysd = GetYsd(i);
					if (ysd < ysdMin)
					{
						ysdMin = ysd;
						x = i;
					}
				}
			}
			else
			{
				for (int i = xEnd; i > xStart; i -= 24)
				{
					int ysd = GetYsd(i);
					if (ysd < ysdMin)
					{
						ysdMin = ysd;
						x = i;
					}
				}
			}

			return new[]
			{
				x, ysdMin
			};
		}
	}
}
