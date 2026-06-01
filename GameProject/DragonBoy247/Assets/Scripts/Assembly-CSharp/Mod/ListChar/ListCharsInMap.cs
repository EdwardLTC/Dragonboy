using System;
using System.Collections.Generic;
using Mod.R;
using UnityEngine;

namespace Mod.ListChar
{
	internal static class ListCharsInMap
	{
		static readonly List<Char> listChars = new List<Char>();
		public static bool isEnabled;
		static bool isShowPet;

		static readonly int x = 15 - 9;
		static int y;

		static int maxLength;
		static readonly int MAX_CHAR = 6;
		static readonly int distanceBetweenLines = 8;
		static readonly int frameInset = 2;
		static int offset;
		static bool isCollapsed;
		static int titleWidth;
		static int offsetX;

		static readonly Color panelBackground = new Color(0.07f, 0.08f, 0.1f, 0.82f);
		static readonly Color headerBackground = new Color(0.12f, 0.14f, 0.17f, 0.92f);
		static readonly Color borderColor = new Color(0.58f, 0.62f, 0.68f, 0.22f);
		static readonly Color hoverRowColor = new Color(0.19f, 0.22f, 0.27f, 0.86f);
		static readonly Color focusRowColor = new Color(0.95f, 0.67f, 0.25f, 0.72f);
		static readonly Color idleRowColor = new Color(0.13f, 0.15f, 0.18f, 0.5f);

		internal static void Update()
		{
			if (!isEnabled)
			{
				return;
			}
			ClampOffset();
			listChars.Clear();
			for (int i = 0; i < GameScr.vCharInMap.size(); i++)
			{
				Char ch = (Char)GameScr.vCharInMap.elementAt(i);
				if (ch.IsNormalChar(true))
				{
					listChars.Add(ch);
					if (isShowPet && ch.charID > 0)
					{
						Char chPet = GameScr.findCharInMap(-ch.charID);
						if (chPet != null)
						{
							listChars.Add(chPet);
						}
					}
				}
			}
			if (isShowPet)
			{
				for (int i = 0; i < GameScr.vCharInMap.size(); i++)
				{
					Char ch = (Char)GameScr.vCharInMap.elementAt(i);
					if (ch.IsNormalChar(false, true) && !listChars.Contains(ch))
					{
						listChars.Add(ch);
					}
				}
			}
			ClampOffset();
			if (GameCanvas.isMouseFocus(GameCanvas.w - (x + offsetX) - maxLength, y + 1, maxLength, 8 * MAX_CHAR))
			{
				if (GameCanvas.pXYScrollMouse > 0)
					if (offset < listChars.Count - MAX_CHAR)
						offset++;
				if (GameCanvas.pXYScrollMouse < 0)
					if (offset > 0)
						offset--;
			}
			getScrollBar(out int scrollBarWidth, out _);
			if (listChars.Count > MAX_CHAR)
				offsetX = scrollBarWidth;
			else
				offsetX = 0;
		}

		internal static int Paint(int _y, mGraphics g)
		{
			if (!isEnabled)
			{
				return getSpaceOccupied();
			}
			ClampOffset();
			y = _y;
			maxLength = 0;

			if (!isCollapsed)
			{
				PaintListChars(g);
				PaintScroll(g);
			}
			PaintRect(g);
			return getSpaceOccupied();
		}

		static string formatHP(Char ch)
		{
			long hp = ch.cHP;
			long hpFull = ch.cHPFull;
			float ratio = hpFull > 0 ? hp / (float)hpFull : 0f;
			Color color = new Color(Mathf.Clamp(2 - ratio * 2, 0, 1), Mathf.Clamp(ratio * 2, 0, 1), 0);
			string hexColor = $"#{(int)(color.r * 255):x2}{(int)(color.g * 255):x2}{(int)(color.b * 255):x2}{(int)(color.a * 255):x2}";
			if (hp == 0)
			{
				hexColor = "black";
			}
			return $"<color=white>[<color={hexColor}>{NinjaUtil.getMoneys(ch.cHP)}</color>/<color=lime>{NinjaUtil.getMoneys(ch.cHPFull)}</color>]</color>";
		}

		static void ClampOffset()
		{
			int maxOffset = Mathf.Max(0, listChars.Count - MAX_CHAR);
			offset = Mathf.Clamp(offset, 0, maxOffset);
		}

		static int GetVisibleStartIndex()
		{
			return listChars.Count > MAX_CHAR ? listChars.Count - MAX_CHAR : 0;
		}

		static int GetVisibleCount()
		{
			return Mathf.Min(MAX_CHAR, listChars.Count);
		}

		static int GetPanelContentWidth()
		{
			return Mathf.Max(maxLength, titleWidth);
		}

		static string BuildTitleText()
		{
			int maxPlayers = GameScr.gI().maxPlayer[TileMap.zoneID];
			int players = GameScr.gI().numPlayer[TileMap.zoneID];
			return $"{TileMap.mapName} • {Strings.zone} {TileMap.mapID} [{players}/{maxPlayers}]";
		}

		static Color GetRowColor(Char ch, bool isHovered)
		{
			if (Char.myCharz().charFocus == ch)
				return focusRowColor;
			if (isHovered)
				return hoverRowColor;
			if (ch.cHP <= 0)
				return new Color(idleRowColor.r, idleRowColor.g, idleRowColor.b, 0.35f);
			return idleRowColor;
		}

		static Color GetRowAccent(Char ch)
		{
			if (ch.IsPet())
				return new Color(0.25f, 0.8f, 0.95f, 0.95f);
			if (ch.IsBoss() || ch.charEffectTime.hasBlackStarDragonBall)
				return new Color(0.96f, 0.34f, 0.35f, 0.95f);
			if (Char.myCharz().charFocus == ch)
				return new Color(0.98f, 0.78f, 0.26f, 0.95f);
			return borderColor;
		}

		static GUIStyle CreateTitleStyle()
		{
			GUIStyle style = new GUIStyle(GUI.skin.label)
			{
				fontSize = 7 * mGraphics.zoomLevel,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.UpperRight,
				richText = true
			};
			style.normal.textColor = Color.white;
			return style;
		}

		static GUIStyle CreateRowStyle()
		{
			return new GUIStyle(GUI.skin.label)
			{
				fontSize = 6 * mGraphics.zoomLevel,
				fontStyle = FontStyle.Bold,
				alignment = TextAnchor.UpperRight,
				richText = true
			};
		}

		static void PaintListChars(mGraphics g)
		{
			int skippedCharCount = 0;
			List<KeyValuePair<string, GUIStyle>> charDescriptions = new List<KeyValuePair<string, GUIStyle>>();
			int start = GetVisibleStartIndex();
			for (int i = start - offset; i < listChars.Count - offset; i++)
			{
				GUIStyle style = CreateRowStyle();
				Char ch = listChars[i];
				string charDesc = $"<color=#F2B965>{ch.GetClanTag()}</color>{ch.GetNameWithoutClanTag(true)} {formatHP(ch)}";
				if (ch.IsPet())
				{
					charDesc += $" <color=#AAB4C0>•</color> {ch.GetGender(true)}";
					Char chMaster = GameScr.findCharInMap(-ch.charID);
					if (chMaster != null)
						charDesc += $" <color=#AAB4C0>•</color> {string.Format(Strings.someonePet, chMaster.GetNameWithoutClanTag(true))}";
					else
						charDesc += $" <color=#AAB4C0>•</color> {Strings.petLostMaster}";
					skippedCharCount++;
				}
				else if (!ch.IsBoss())
					charDesc = $"{i + 1 - skippedCharCount}. {charDesc}";
				else
					skippedCharCount++;
				if (ch.charEffectTime.hasBlackStarDragonBall || ch.IsBoss())
					charDesc = $"<color=#FF6B6B>{charDesc}</color>";
				else if (ch.IsPet())
					charDesc = $"<color=#5ED8F0>{charDesc}</color>";
				if (ch.cHP <= 0)
					charDesc = $"<color=#525252>{charDesc}</color>";

				charDescriptions.Add(new KeyValuePair<string, GUIStyle>(charDesc, style));
				maxLength = System.Math.Max(maxLength, Utils.getWidth(style, charDesc) + (ch.cFlag != 0 ? distanceBetweenLines + 1 : 0));
			}
			FillBackground(g);
			for (int i = start - offset; i < listChars.Count - offset; i++)
			{
				int offsetPaint = 0;
				Char ch = listChars[i];
				int rowIndex = i - start + offset;
				int rowY = y + 1 + distanceBetweenLines * rowIndex;
				int rowX = GameCanvas.w - (x + offsetX) - maxLength;
				bool isHovered = GameCanvas.isMouseFocus(rowX - (ch.cFlag != 0 ? distanceBetweenLines + 1 : 0), rowY, maxLength, distanceBetweenLines - 1);
				if (ch.cFlag != 0)
				{
					offsetPaint = distanceBetweenLines + 1;
					if (ch.cFlag == 9 || ch.cFlag == 10)
					{
						GUIStyle flagStyle = new GUIStyle(GUI.skin.label)
						{
							alignment = TextAnchor.UpperCenter,
							fontSize = 6 * mGraphics.zoomLevel
						};
						flagStyle.normal.textColor = Color.white;
						if (ch.cFlag == 9)
							g.drawString("K", -(x + offsetX), mGraphics.zoomLevel - 3 + y + distanceBetweenLines * (i - start + offset), flagStyle);
						if (ch.cFlag == 10)
							g.drawString("M", -(x + offsetX), mGraphics.zoomLevel - 3 + y + distanceBetweenLines * (i - start + offset), flagStyle);
					}
					g.setColor(ch.GetFlagColor());
					g.fillRect(GameCanvas.w - (x + offsetX) - distanceBetweenLines + 1, y + 1 + distanceBetweenLines * (i - start + offset), distanceBetweenLines - 1, distanceBetweenLines - 1);
				}
				g.setColor(GetRowColor(ch, isHovered));
				g.fillRect(rowX, rowY, maxLength - offsetPaint, distanceBetweenLines - 1);
				g.setColor(GetRowAccent(ch));
				g.fillRect(rowX, rowY, 2, distanceBetweenLines - 1);
				if (isHovered)
				{
					g.setColor(new Color(1f, 1f, 1f, 0.12f));
					g.fillRect(rowX, rowY + 1, maxLength, 1);
					g.setColor(borderColor);
					g.drawRect(rowX, rowY + 1, maxLength - 1, 6);
				}
				g.drawString(charDescriptions[rowIndex].Key, -(x + offsetX) - offsetPaint, mGraphics.zoomLevel - 3 + rowY + (ch.IsBoss() ? -1 : 0), charDescriptions[rowIndex].Value);
			}
		}

		static void FillBackground(mGraphics g)
		{
			if (!isCollapsed && listChars.Count > 0)
			{
				g.setColor(panelBackground);
				getScrollBar(out int scrollBarWidth, out _);
				if (listChars.Count <= MAX_CHAR)
				{
					scrollBarWidth = 0;
				}
				int w = GetPanelContentWidth() + 5 + (scrollBarWidth > 0 ? scrollBarWidth + 2 : 0);
				int h = distanceBetweenLines * GetVisibleCount() + 7;
				g.fillRect(GameCanvas.w - (x + offsetX) - GetPanelContentWidth() - 3 + frameInset, y - 5, w - frameInset, h);
			}
		}

		static void PaintScroll(mGraphics g)
		{
			if (listChars.Count > MAX_CHAR)
			{
				getButtonUp(out int buttonUpX, out int buttonUpY);
				getButtonDown(out int buttonDownX, out int buttonDownY);
				getScrollBar(out int scrollBarWidth, out int scrollBarHeight);
				g.setColor(new Color(.2f, .2f, .2f, .4f));
				g.fillRect(buttonUpX, buttonUpY, 9, scrollBarHeight + 6 * 2);
				g.drawRegion(Mob.imgHP, 0, offset < listChars.Count - MAX_CHAR ? 18 : 54, 9, 6, 1, buttonUpX, buttonUpY, 0);
				g.drawRegion(Mob.imgHP, 0, offset > 0 ? 18 : 54, 9, 6, 0, buttonDownX, buttonDownY, 0);
				int thumbY = GetScrollThumbY(buttonUpY, scrollBarHeight);
				int thumbHeight = Mathf.Max(1, Mathf.CeilToInt((float)MAX_CHAR / listChars.Count * scrollBarHeight));
				g.setColor(new Color(.18f, .2f, .23f, .82f));
				g.fillRect(buttonUpX, thumbY, scrollBarWidth, thumbHeight);
				g.setColor(new Color(.88f, .92f, .98f, .55f));
				g.drawRect(buttonUpX, thumbY, scrollBarWidth - 1, thumbHeight - 1);
			}
		}

		static void PaintRect(mGraphics g)
		{
			getScrollBar(out int scrollBarWidth, out _);
			if (listChars.Count <= MAX_CHAR)
			{
				scrollBarWidth = 0;
			}
			string str = BuildTitleText();
			GUIStyle style = CreateTitleStyle();
			titleWidth = Utils.getWidth(style, str);
			int panelWidth = GetPanelContentWidth();
			int titleX = GameCanvas.w - (x + offsetX) - titleWidth + scrollBarWidth;
			g.setColor(headerBackground);
			g.fillRect(titleX, y - distanceBetweenLines, titleWidth, 8);
			if (GameCanvas.isMouseFocus(titleX, y - distanceBetweenLines, titleWidth, 8))
			{
				g.setColor(new Color(1f, 1f, 1f, 0.12f));
				g.fillRect(titleX, y - 1, titleWidth - 1, 1);
			}
			g.drawString(str, -(x + offsetX) + scrollBarWidth, y - distanceBetweenLines - 2, style);
			getCollapseButton(out int collapseButtonX, out int collapseButtonY);
			g.drawRegion(Mob.imgHP, 0, 18, 9, 6, isCollapsed ? 5 : 4, collapseButtonX, collapseButtonY, 0);
			if (isCollapsed || listChars.Count <= 0)
			{
				return;
			}
			int w = panelWidth + 5 + (scrollBarWidth > 0 ? scrollBarWidth + 2 : 0);
			int h = distanceBetweenLines * GetVisibleCount() + 7;
			int left = GameCanvas.w - (x + offsetX) - panelWidth - 3 + frameInset;
			int outerWidth = w - frameInset;
			g.setColor(borderColor);
			int titleGapStart = Mathf.Max(left, titleX - 2);
			int titleGapEnd = Mathf.Min(left + outerWidth, titleX + titleWidth + 2);
			if (titleGapStart > left)
				g.fillRect(left, y - 5, titleGapStart - left, 1);
			if (titleGapEnd < left + outerWidth)
				g.fillRect(titleGapEnd, y - 5, left + outerWidth - titleGapEnd, 1);
			g.fillRect(left, y - 5, 1, h);
			g.fillRect(left + outerWidth, y - 5, 1, h + 1);
			g.fillRect(left, y - 5 + h, outerWidth + 1, 1);
		}

		internal static void updateTouch()
		{
			if (!isEnabled)
				return;
			try
			{
				if (!GameCanvas.isTouch || ChatTextField.gI().isShow || GameCanvas.menu.showMenu)
					return;
				getScrollBar(out int scrollBarWidth, out int scrollBarHeight);
				getCollapseButton(out int collapseButtonX, out int collapseButtonY);
				if (GameCanvas.isPointerHoldIn(collapseButtonX, collapseButtonY, 9, 6) || GameCanvas.isPointerHoldIn(GameCanvas.w - (x + offsetX) - titleWidth + scrollBarWidth, y - distanceBetweenLines, titleWidth, 8))
				{
					GameCanvas.isPointerJustDown = false;
					GameScr.gI().isPointerDowning = false;
					if (GameCanvas.isPointerClick)
						isCollapsed = !isCollapsed;
					GameCanvas.clearAllPointerEvent();
					return;
				}
				int start = 0;
				if (listChars.Count > MAX_CHAR)
					start = listChars.Count - MAX_CHAR;
				for (int i = start - offset; i < listChars.Count - offset; i++)
				{
					if (GameCanvas.isPointerHoldIn(GameCanvas.w - (x + offsetX) - maxLength - (listChars[i].cFlag != 0 ? distanceBetweenLines + 1 : 0), y + 1 + distanceBetweenLines * (i - start + offset), maxLength, distanceBetweenLines - 1))
					{
						GameCanvas.isPointerJustDown = false;
						GameScr.gI().isPointerDowning = false;
						if (GameCanvas.isPointerClick)
						{
							Char.myCharz().mobFocus = null;
							Char.myCharz().npcFocus = null;
							Char.myCharz().itemFocus = null;
							if (Char.myCharz().charFocus != listChars[i])
								Char.myCharz().charFocus = listChars[i];
							else
								Utils.TeleportMyChar(listChars[i]);
						}
						Char.myCharz().currentMovePoint = null;
						GameCanvas.clearAllPointerEvent();
						return;
					}
				}
				if (listChars.Count > MAX_CHAR)
				{
					getButtonUp(out int buttonUpX, out int buttonUpY);
					if (GameCanvas.isPointerMove && GameCanvas.isPointerDown && GameCanvas.isPointerHoldIn(buttonUpX, buttonUpY, scrollBarWidth, scrollBarHeight))
					{
						float increment = scrollBarHeight / (float)listChars.Count;
						float newOffset = (GameCanvas.pyMouse - buttonUpY) / increment;
						if (float.IsNaN(newOffset))
							return;
						offset = Mathf.Clamp(listChars.Count - Mathf.RoundToInt(newOffset), 0, listChars.Count - MAX_CHAR);
						return;
					}
					if (GameCanvas.isPointerHoldIn(buttonUpX, buttonUpY, 9, 6))
					{
						GameCanvas.isPointerJustDown = false;
						GameScr.gI().isPointerDowning = false;
						if (GameCanvas.isPointerClick)
						{
							if (offset < listChars.Count - MAX_CHAR)
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
							if (offset > 0)
								offset--;
						}
						GameCanvas.clearAllPointerEvent();
					}
				}
			}
			catch (Exception)
			{
				// ignored
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
			buttonDownY = y + 2 + distanceBetweenLines * (MAX_CHAR - 1);
		}

		static void getScrollBar(out int scrollBarWidth, out int scrollBarHeight)
		{
			scrollBarWidth = 9;
			scrollBarHeight = MAX_CHAR * distanceBetweenLines - 1 - 6 * 2;
		}

		static int GetScrollThumbY(int buttonUpY, int scrollBarHeight)
		{
			if (listChars.Count <= 0)
				return buttonUpY + 6;
			return buttonUpY + 6 + Mathf.CeilToInt((float)scrollBarHeight / listChars.Count * (listChars.Count - offset - MAX_CHAR));
		}

		static void getCollapseButton(out int collapseButtonX, out int collapseButtonY)
		{
			getScrollBar(out int scrollBarWidth, out _);
			if (listChars.Count <= MAX_CHAR)
				scrollBarWidth = 0;
			collapseButtonX = GameCanvas.w - (x + offsetX) - titleWidth + scrollBarWidth - 8;
			collapseButtonY = y - distanceBetweenLines + 1;
		}

		public static void setState(bool value)
		{
			isEnabled = value;
		}

		internal static void setStatePet(bool value)
		{
			isShowPet = value;
		}

		static int getSpaceOccupied()
		{
			if (!isEnabled)
				return 0;
			byte border = 5;
			byte titleH = 7;
			if (listChars.Count <= 0)
				return titleH + border + 3;
			if (isCollapsed)
				return distanceBetweenLines;
			return titleH + border + distanceBetweenLines * Mathf.Clamp(listChars.Count, 0, MAX_CHAR);
		}
	}
}
