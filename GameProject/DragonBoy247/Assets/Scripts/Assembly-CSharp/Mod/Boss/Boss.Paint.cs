using System;
using Mod.Graphics;
using Mod.Xmap;
using UnityEngine;

namespace Mod
{
	public partial class Boss
	{
		static readonly int distanceBetweenLines = 8;
		static int offset;
		static readonly int x = 6;
		static int y;
		static int maxLength;
		static int lastBoss = -1;
		static bool isCollapsed;
		static readonly int MAX_BOSS_DISPLAY = 5;
		static readonly string LIST_BOSS = "Danh sách Boss";
		static int titleWidth;
		static int offsetX;

		static GUIStyle cachedRowStyle;
		static int cachedRowFontSize;
		static GUIStyle cachedTitleStyle;
		static int cachedTitleFontSize;

		static GUIStyle GetRowStyle()
		{
			int fontSize = 6 * mGraphics.zoomLevel;
			if (cachedRowStyle == null || cachedRowFontSize != fontSize)
			{
				cachedRowFontSize = fontSize;
				cachedRowStyle = new GUIStyle(GUI.skin.label)
				{
					alignment = TextAnchor.UpperRight,
					fontSize = fontSize,
					fontStyle = FontStyle.Bold,
					richText = true
				};
			}
			return cachedRowStyle;
		}

		static GUIStyle GetTitleStyle()
		{
			int fontSize = 7 * mGraphics.zoomLevel;
			if (cachedTitleStyle == null || cachedTitleFontSize != fontSize)
			{
				cachedTitleFontSize = fontSize;
				cachedTitleStyle = new GUIStyle(GUI.skin.label)
				{
					fontSize = fontSize,
					fontStyle = FontStyle.Bold,
					alignment = TextAnchor.UpperRight,
					richText = true
				};
				cachedTitleStyle.normal.textColor = Color.white;
			}
			return cachedTitleStyle;
		}

		static void ClampOffset()
		{
			int maxOffset = System.Math.Max(0, listBosses.Count - MAX_BOSS_DISPLAY);
			offset = Mathf.Clamp(offset, 0, maxOffset);
		}


		static int GetVisibleStartIndex()
		{
			return listBosses.Count > MAX_BOSS_DISPLAY ? listBosses.Count - MAX_BOSS_DISPLAY : 0;
		}

		static int GetPanelWidth(int scrollBarWidth)
		{
			return maxLength + 5 + (scrollBarWidth > 0 ? scrollBarWidth + 2 : 0);
		}

		static int GetPanelHeight()
		{
			return distanceBetweenLines * System.Math.Min(MAX_BOSS_DISPLAY, listBosses.Count) + 7;
		}

		static int GetScrollThumbY(int scrollBarHeight)
		{
			return y + 6 + Mathf.CeilToInt((float)scrollBarHeight / listBosses.Count * (listBosses.Count - offset - MAX_BOSS_DISPLAY));
		}

		string ToString(bool enableRichText)
		{
			if (!enableRichText)
				return ToString();

			TimeSpan timeSpan = DateTime.Now.Subtract(AppearTime);
			string colorName = "yellow";
			string colorMap = "yellow";
			if (TileMap.mapID == mapId)
			{
				colorName = "orange";
				colorMap = "red";
				if (Utils.FindCharInMap(name) != null)
					colorName = "red";
			}

			string result = $"<color={colorName}>{name}</color> - ";
			if (string.IsNullOrEmpty(map))
				result += "chưa biết";
			else
				result += $"<color={colorMap}>{map}</color> [<color={colorMap}>{mapId}</color>]";
			result += " - ";
			if (!isDied)
			{
				if (zoneId > -1)
				{
					if (TileMap.mapID == mapId)
					{
						if (TileMap.zoneID == zoneId)
							result += $"<color=yellow>khu</color> <color=red>{zoneId}</color> - ";
						else
							result += $"<color=yellow>khu {zoneId}</color> - ";
					}
					else
						result += $"khu <color=yellow>{zoneId}</color> - ";
				}
				int hours = (int)System.Math.Floor((decimal)timeSpan.TotalHours);
				if (hours > 0)
					result += $"<color=orange>{hours}</color>h";
				if (timeSpan.Minutes > 0)
					result += $"<color=orange>{timeSpan.Minutes}</color>m";
				result += $"<color=orange>{timeSpan.Seconds}</color>s";
			}
			else
			{
				if (!string.IsNullOrEmpty(killer))
					result += $"Bị <color=orange>{killer}</color> tiêu diệt";
				else
					result += "Đã chết";
			}
			return result;
		}

		public static int Paint(int _y, mGraphics g)
		{
			if (!isEnabled || listBosses.Count <= 0)
				return getSpaceOccupied();

			ClampOffset();
			maxLength = 0;
			y = _y;

			if (!isCollapsed)
			{
				PaintListBosses(g);
				PaintScroll(g);
			}

			PaintRect(g);
			return getSpaceOccupied();
		}

		static void PaintListBosses(mGraphics g)
		{
			int start = GetVisibleStartIndex();
			int end = listBosses.Count - offset;
			GUIStyle rowStyle = GetRowStyle();

			for (int i = start - offset; i < end; i++)
			{
				Boss boss = listBosses[i];
				int length = Utils.getWidth(rowStyle, $"{i + 1}. {boss}");
				maxLength = System.Math.Max(length, maxLength);
			}

			FillBackground(g);
			int xDraw = GameCanvas.w - (x + offsetX) - maxLength;
			for (int i = start - offset; i < end; i++)
			{
				int yDraw = y + distanceBetweenLines * (i - start + offset);
				Boss boss = listBosses[i];
				g.setColor(new Color(.2f, .2f, .2f, .4f));
				if (boss.isDied)
					g.setColor(new Color(.2f, .2f, .2f, .2f));
				if (GameCanvas.isMouseFocus(xDraw, yDraw, maxLength, 7))
					g.setColor(new Color(.2f, .2f, .2f, .7f));
				g.fillRect(xDraw, yDraw + 1, maxLength, 7);
				if (GameCanvas.isMouseFocus(xDraw, yDraw, maxLength, 7))
					CustomGraphics.fillRect(xDraw + 1, yDraw + 7, (maxLength - 2) * mGraphics.zoomLevel + 2, 1, Color.white);
				g.drawString($"{i + 1}. {boss.ToString(true)}", -(x + offsetX), mGraphics.zoomLevel - 3 + yDraw, rowStyle);
			}
		}

		static void PaintScroll(mGraphics g)
		{
			if (listBosses.Count <= MAX_BOSS_DISPLAY)
				return;

			getButtonUp(out int buttonUpX, out int buttonUpY);
			getButtonDown(out int buttonDownX, out int buttonDownY);
			getScrollBar(out int scrollBarWidth, out int scrollBarHeight, out int scrollBarThumbHeight);
			int thumbY = GetScrollThumbY(scrollBarHeight);

			g.setColor(new Color(.2f, .2f, .2f, .4f));
			g.fillRect(buttonUpX, buttonUpY, 9, scrollBarHeight + 6 * 2);
			g.drawRegion(Mob.imgHP, 0, offset < listBosses.Count - MAX_BOSS_DISPLAY ? 24 : 54, 9, 6, 1, buttonUpX, buttonUpY, 0);
			g.drawRegion(Mob.imgHP, 0, offset > 0 ? 24 : 54, 9, 6, 0, buttonDownX, buttonDownY, 0);
			g.setColor(new Color(.2f, .2f, .2f, .7f));
			g.fillRect(buttonUpX, thumbY, scrollBarWidth, scrollBarThumbHeight);
			g.setColor(new Color(.7f, .7f, 0f, 1f));
			g.drawRect(buttonUpX, thumbY, scrollBarWidth - 1, scrollBarThumbHeight - 1);
		}

		static void PaintRect(mGraphics g)
		{
			getScrollBar(out int scrollBarWidth, out _, out _);
			if (listBosses.Count <= MAX_BOSS_DISPLAY)
				scrollBarWidth = 0;

			int w = GetPanelWidth(scrollBarWidth);
			int h = GetPanelHeight();
			GUIStyle style = GetTitleStyle();
			titleWidth = Utils.getWidth(style, LIST_BOSS);

			int titleX = GameCanvas.w - (x + offsetX) - titleWidth + scrollBarWidth;
			g.setColor(new Color(.2f, .2f, .2f, .7f));
			g.fillRect(titleX, y - distanceBetweenLines, titleWidth, 8);
			if (GameCanvas.isMouseFocus(titleX, y - distanceBetweenLines, titleWidth, 8))
			{
				g.setColor(style.normal.textColor);
				g.fillRect(titleX, y - 1, titleWidth - 1, 1);
			}
			g.drawString(LIST_BOSS, -(x + offsetX) + scrollBarWidth, y - distanceBetweenLines - 2, style);
			getCollapseButton(out int collapseButtonX, out int collapseButtonY);
			g.drawRegion(Mob.imgHP, 0, 18, 9, 6, isCollapsed ? 5 : 4, collapseButtonX, collapseButtonY, 0);
			if (isCollapsed || listBosses.Count <= 0)
				return;

			g.setColor(Color.yellow);
			g.fillRect(GameCanvas.w - (x + offsetX) - maxLength - 3, y - 5, w - titleWidth - 9 - (scrollBarWidth > 0 ? 2 : 0), 1);
			g.fillRect(GameCanvas.w - (x + offsetX) + scrollBarWidth, y - 5, 3 + (scrollBarWidth > 0 ? 1 : 0), 1);
			g.fillRect(GameCanvas.w - (x + offsetX) - maxLength - 3, y - 5, 1, h);
			g.fillRect(GameCanvas.w - (x + offsetX) - maxLength - 3 + w, y - 5, 1, h + 1);
			g.fillRect(GameCanvas.w - (x + offsetX) - maxLength - 3, y - 5 + h, w + 1, 1);
		}

		static void FillBackground(mGraphics g)
		{
			if (!isCollapsed && listBosses.Count > 0)
			{
				g.setColor(new Color(0, 0, 0, .075f));
				getScrollBar(out int scrollBarWidth, out _, out _);
				if (listBosses.Count <= MAX_BOSS_DISPLAY)
					scrollBarWidth = 0;
				int w = GetPanelWidth(scrollBarWidth);
				int h = GetPanelHeight();
				g.fillRect(GameCanvas.w - (x + offsetX) - maxLength - 3, y - 5, w, h);
			}
		}

		public static void UpdateTouch()
		{
			if (lastBoss != -1 && mSystem.currentTimeMillis() - Utils.GetLastTimePress() > 200)
				lastBoss = -1;
			if (!isEnabled)
				return;
			if (!GameCanvas.isTouch || ChatTextField.gI().isShow || GameCanvas.menu.showMenu)
				return;

			ClampOffset();
			getCollapseButton(out int collapseButtonX, out int collapseButtonY);
			getScrollBar(out int scrollBarWidth, out int scrollBarHeight, out _);
			if (GameCanvas.isPointerHoldIn(collapseButtonX, collapseButtonY, 6, 9) || GameCanvas.isMouseFocus(GameCanvas.w - (x + offsetX) - titleWidth + scrollBarWidth, y - distanceBetweenLines, titleWidth, 8))
			{
				GameCanvas.isPointerJustDown = false;
				GameScr.gI().isPointerDowning = false;
				if (GameCanvas.isPointerClick)
					isCollapsed = !isCollapsed;
				GameCanvas.clearAllPointerEvent();
				return;
			}
			if (isCollapsed)
				return;

			int start = GetVisibleStartIndex();
			for (int i = start - offset; i < listBosses.Count - offset; i++)
			{
				if (GameCanvas.isPointerHoldIn(GameCanvas.w - (x + offsetX) - maxLength, y + 1 + distanceBetweenLines * (i - start + offset), maxLength, 7))
				{
					GameCanvas.isPointerJustDown = false;
					GameScr.gI().isPointerDowning = false;
					if (GameCanvas.isPointerClick)
					{
						if (listBosses[i].isDied)
							GameScr.info1.addInfo($"Boss đã {(string.IsNullOrEmpty(listBosses[i].killer) ? "chết" : $"bị {listBosses[i].killer} tiêu diệt")}!", 0);
						else
						{
							if (lastBoss == i && mSystem.currentTimeMillis() - Utils.GetLastTimePress() <= 200)
							{
								if (TileMap.mapID != listBosses[i].mapId)
								{
									if (XmapController.gI.IsActing)
										XmapController.finishXmap();
									XmapController.start(listBosses[i].mapId);
									lastBoss = -1;
									return;
								}
							}
							else
								lastBoss = i;
							if (TileMap.mapID == listBosses[i].mapId)
							{
								int j = 0;
								for (; j < GameScr.vCharInMap.size(); j++)
								{
									Char ch = GameScr.vCharInMap.elementAt(j) as Char;
									if (ch?.cName == listBosses[i].name)
									{
										Char.myCharz().deFocusNPC();
										Char.myCharz().itemFocus = null;
										Char.myCharz().mobFocus = null;
										if (Char.myCharz().charFocus != ch)
											Char.myCharz().charFocus = ch;
										else
											Utils.TeleportMyChar(ch);
										break;
									}
								}
								if (j == GameScr.vCharInMap.size())
								{
									if (listBosses[i].zoneId != -1 && TileMap.zoneID != listBosses[i].zoneId)
									{
										GameScr.info1.addInfo($"Vào khu {listBosses[i].zoneId}!", 0);
										Service.gI().requestChangeZone(listBosses[i].zoneId, 0);
										return;
									}
									GameScr.info1.addInfo("Boss không có trong khu!", 0);
								}
							}
						}
					}
					GameCanvas.clearAllPointerEvent();
					return;
				}
			}

			if (listBosses.Count > MAX_BOSS_DISPLAY)
			{
				getButtonUp(out int buttonUpX, out int buttonUpY);
				if (GameCanvas.isPointerMove && GameCanvas.isPointerDown && GameCanvas.isPointerHoldIn(buttonUpX, buttonUpY, scrollBarWidth, scrollBarHeight))
				{
					float increment = scrollBarHeight / (float)listBosses.Count;
					float newOffset = (GameCanvas.pyMouse - buttonUpY) / increment;
					if (float.IsNaN(newOffset))
						return;
					offset = Mathf.Clamp(listBosses.Count - Mathf.RoundToInt(newOffset), 0, listBosses.Count - MAX_BOSS_DISPLAY);
					return;
				}
				if (GameCanvas.isPointerHoldIn(buttonUpX, buttonUpY, 9, 6))
				{
					GameCanvas.isPointerJustDown = false;
					GameScr.gI().isPointerDowning = false;
					if (GameCanvas.isPointerClick)
					{
						if (offset + MAX_BOSS_DISPLAY <= listBosses.Count - MAX_BOSS_DISPLAY)
							offset += MAX_BOSS_DISPLAY;
						else if (offset < listBosses.Count - MAX_BOSS_DISPLAY)
							offset++;
					}
					GameCanvas.clearAllPointerEvent();
					return;
				}
				getButtonDown(out int buttonDownX, out int buttonDownY);
				if (GameCanvas.isPointerHoldIn(buttonDownX, buttonDownY, 9, 6))
				{
					GameCanvas.isPointerJustDown = false;
					GameScr.gI().isPointerDowning = false;
					if (GameCanvas.isPointerClick)
					{
						if (offset - MAX_BOSS_DISPLAY >= 0)
							offset -= MAX_BOSS_DISPLAY;
						else if (offset > 0)
							offset--;
					}
					GameCanvas.clearAllPointerEvent();
				}
			}
		}

		public static void Update()
		{
			ClampOffset();
			for (int i = listBosses.Count - 1; i >= 0; i--)
			{
				Boss boss = listBosses[i];
				if (boss.mapId == TileMap.mapID && !Char.isLoadingMap)
				{
					int j = 0;
					for (; j < GameScr.vCharInMap.size(); j++)
					{
						Char ch = GameScr.vCharInMap.elementAt(j) as Char;
						if (ch == null)
						{
							continue;
						}
						if (ch.cName == boss.name)
						{
							if (boss.zoneId == -1)
								boss.zoneId = TileMap.zoneID;
							if (ch.isDie || ch.cHP == 0)
								boss.isDied = true;
							break;
						}
					}
					if (boss.zoneId == TileMap.zoneID && j == GameScr.vCharInMap.size())
						boss.isDied = true;
				}
			}
			if (isEnabled && !isCollapsed && GameCanvas.isMouseFocus(GameCanvas.w - (x + offsetX) - maxLength, y + 1, maxLength, 8 * MAX_BOSS_DISPLAY))
			{
				if (GameCanvas.pXYScrollMouse > 0)
					if (offset < listBosses.Count - MAX_BOSS_DISPLAY)
						offset++;
				if (GameCanvas.pXYScrollMouse < 0)
					if (offset > 0)
						offset--;
			}
		}

		static void getButtonUp(out int buttonUpX, out int buttonUpY)
		{
			buttonUpX = GameCanvas.w - (x + offsetX) + 2;
			buttonUpY = y + 1;
		}

		static void getButtonDown(out int buttonDownX, out int buttonDownY)
		{
			buttonDownX = GameCanvas.w - (x + offsetX) + 2;
			buttonDownY = y + 2 + distanceBetweenLines * (MAX_BOSS_DISPLAY - 1);
		}

		static void getScrollBar(out int scrollBarWidth, out int scrollBarHeight, out int scrollBarThumbHeight)
		{
			scrollBarWidth = 9;
			scrollBarHeight = MAX_BOSS_DISPLAY * distanceBetweenLines - 1 - 6 * 2;
			scrollBarThumbHeight = Mathf.CeilToInt((float)MAX_BOSS_DISPLAY / listBosses.Count * scrollBarHeight);
		}

		static void getCollapseButton(out int collapseButtonX, out int collapseButtonY)
		{
			getScrollBar(out int scrollBarWidth, out _, out _);
			if (listBosses.Count <= MAX_BOSS_DISPLAY)
				scrollBarWidth = 0;
			collapseButtonX = GameCanvas.w - (x + offsetX) - titleWidth + scrollBarWidth - 8;
			collapseButtonY = y - distanceBetweenLines + 1;
		}

		public static void setState(bool value)
		{
			isEnabled = value;
		}

		static int getSpaceOccupied()
		{
			if (!isEnabled || listBosses.Count <= 0)
				return 0;

			byte border = 5;
			byte titleH = 7;

			return isCollapsed
				? distanceBetweenLines
				: titleH + border + distanceBetweenLines * Mathf.Clamp(listBosses.Count, 0, MAX_BOSS_DISPLAY);
		}
	}
}
