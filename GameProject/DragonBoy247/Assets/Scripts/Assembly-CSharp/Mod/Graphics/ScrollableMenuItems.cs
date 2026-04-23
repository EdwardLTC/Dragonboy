using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Mod.Graphics
{
	internal class ScrollableMenuItems<T>
	{

		int currentOffsetTo;
		int currentStepScroll;
		bool isPressKey;

		int lastMouseY = -1;
		int lastOffsetTo;
		int pointerGrabY = -1;

		internal ScrollableMenuItems(List<T> values)
		{
			Items = values;
			currentStepScroll = StepScroll;
		}

		internal Action<mGraphics, int, int, int, int, int> PaintItemAction { get; set; }
		internal int X { get; set; }
		internal int Y { get; set; }
		internal int Width { get; set; }
		internal int Height { get; set; }
		internal int ItemHeight { get; set; } = 40;
		internal int CurrentItemIndex { get; set; } = -1;
		internal int StepScroll { get; set; } = 70;
		internal int CurrentOffset { get; private set; }
		List<T> Items { get; }
		internal bool AllowSelectNone { get; set; }
		internal Action ItemSelected { get; set; }

		internal void Reset()
		{
			if (AllowSelectNone || Items.Count == 0)
				CurrentItemIndex = -1;
			else
				CurrentItemIndex = 0;
			CurrentOffset = currentOffsetTo = 0;
			currentStepScroll = StepScroll;
			isPressKey = false;
		}

		internal void Update()
		{
			if (CurrentOffset != currentOffsetTo)
			{
				if (CurrentOffset < currentOffsetTo)
				{
					if (CurrentOffset + currentStepScroll * 2 > currentOffsetTo)
						currentStepScroll /= 3;
					if (CurrentOffset > currentOffsetTo || currentStepScroll == 0)
					{
						CurrentOffset = currentOffsetTo;
						currentStepScroll = StepScroll;
					}
					else
						CurrentOffset += currentStepScroll;
				}
				else if (CurrentOffset > currentOffsetTo)
				{
					if (CurrentOffset - currentStepScroll * 2 < currentOffsetTo)
						currentStepScroll /= 3;
					if (CurrentOffset < currentOffsetTo || currentStepScroll == 0)
					{
						CurrentOffset = currentOffsetTo;
						currentStepScroll = StepScroll;
					}
					else
						CurrentOffset -= currentStepScroll;
				}
			}
		}

		internal void UpdateKey()
		{
			if (Items.Count > (float)Height / ItemHeight)
			{
				if (GameCanvas.pXYScrollMouse != 0)
				{
					if (IsPointerIn(X, Y, Width, Height))
					{
						currentStepScroll = StepScroll;
						if (GameCanvas.pXYScrollMouse < 0)
							currentOffsetTo += StepScroll;
						else if (GameCanvas.pXYScrollMouse > 0)
							currentOffsetTo -= StepScroll;
						isPressKey = false;
					}
				}
				else
				{
					if (GameCanvas.isPointerJustDown && lastMouseY == -1 && IsPointerIn(X, Y, Width, Height))
					{
						lastMouseY = GameCanvas.pyMouse;
						pointerGrabY = GameCanvas.pyMouse;
						currentStepScroll = StepScroll;
						lastOffsetTo = currentOffsetTo = CurrentOffset;
						isPressKey = false;
					}
					if (lastMouseY != -1 && GameCanvas.isPointerDown)
					{
						currentOffsetTo = CurrentOffset = lastOffsetTo - (GameCanvas.pyMouse - lastMouseY);
						isPressKey = false;
					}
				}
				if (currentOffsetTo > Items.Count * ItemHeight - Height)
					currentOffsetTo = Items.Count * ItemHeight - Height;
				if (currentOffsetTo < 0)
					currentOffsetTo = 0;
			}
			if (GameCanvas.isPointerHoldIn(X, Y, Width, Height))
			{
				if (GameCanvas.isPointerSelect && !GameCanvas.isPointerMove)
				{
					int releaseY = pointerGrabY >= 0 ? pointerGrabY : GameCanvas.pyMouse;
					bool treatAsTap = Res.abs(GameCanvas.pyMouse - releaseY) < 12;
					if (treatAsTap)
					{
						GameCanvas.isPointerJustDown = false;
						GameScr.gI().isPointerDowning = false;
						isPressKey = false;
						int selectedIndex = (GameCanvas.pyMouse - Y + CurrentOffset) / ItemHeight;
						if (selectedIndex >= 0 && selectedIndex < Items.Count)
						{
							if (selectedIndex != CurrentItemIndex)
								CurrentItemIndex = selectedIndex;
							else
							{
								if (AllowSelectNone || Items.Count == 0)
									CurrentItemIndex = -1;
							}
							if (AllowSelectNone || CurrentItemIndex != -1)
								new Thread(() =>
								{
									Thread.Sleep(50);
									ItemSelected?.Invoke();
								}).Start();
						}
					}
				}
			}
			if (GameCanvas.isPointerJustRelease)
			{
				lastMouseY = -1;
				pointerGrabY = -1;
			}
			if (GameCanvas.keyPressed[!Main.isPC ? 2 : 21] || GameCanvas.keyPressed[!Main.isPC ? 8 : 22])
			{
				int minVisibleIndex = currentOffsetTo;
				int maxVisibleIndex = currentOffsetTo + Height / ItemHeight;
				int upperOffset = ItemHeight * (CurrentItemIndex - 2);
				int lowerOffset = ItemHeight * (CurrentItemIndex - Height / ItemHeight + 2);
				bool isCheckNewOffset = true;
				if (GameCanvas.keyPressed[!Main.isPC ? 2 : 21])
				{
					GameCanvas.keyPressed[!Main.isPC ? 2 : 21] = false;
					if (CurrentItemIndex == -1)
					{
						CurrentItemIndex = Items.Count - 1;
						if (Items.Count > (float)Height / ItemHeight)
							currentOffsetTo = CurrentOffset = ItemHeight * (CurrentItemIndex - 1);
						isCheckNewOffset = false;
					}
					else
					{
						if (CurrentItemIndex - 1 < 0)
						{
							CurrentItemIndex = Items.Count - 1;
							currentOffsetTo = CurrentOffset = ItemHeight * (CurrentItemIndex - 1);
							isCheckNewOffset = false;
						}
						else
							CurrentItemIndex--;
					}
					if (isCheckNewOffset && Items.Count > (float)Height / ItemHeight)
					{
						if (!isPressKey)
						{
							if (currentOffsetTo < lowerOffset)
								currentOffsetTo = CurrentOffset = lowerOffset - ItemHeight;
							else if (currentOffsetTo > upperOffset)
								currentOffsetTo = CurrentOffset = upperOffset;
						}
						else if (upperOffset < minVisibleIndex)
							currentOffsetTo = CurrentOffset = upperOffset;
					}
				}
				else if (GameCanvas.keyPressed[!Main.isPC ? 8 : 22])
				{
					GameCanvas.keyPressed[!Main.isPC ? 8 : 22] = false;
					if (CurrentItemIndex == -1)
					{
						CurrentItemIndex = 0;
						if (Items.Count > (float)Height / ItemHeight)
							currentOffsetTo = CurrentOffset = 0;
						isCheckNewOffset = false;
					}
					else
					{
						if (CurrentItemIndex + 1 >= Items.Count)
						{
							CurrentItemIndex = 0;
							currentOffsetTo = CurrentOffset = 0;
							isCheckNewOffset = false;
						}
						else
							CurrentItemIndex++;
					}
					if (isCheckNewOffset && Items.Count > (float)Height / ItemHeight)
					{
						if (!isPressKey)
						{
							if (currentOffsetTo < lowerOffset)
								currentOffsetTo = CurrentOffset = lowerOffset;
							else if (currentOffsetTo > upperOffset)
								currentOffsetTo = CurrentOffset = upperOffset + ItemHeight * 2;
						}
						else if (lowerOffset > maxVisibleIndex)
							currentOffsetTo = CurrentOffset = lowerOffset;
					}
				}
				if (currentOffsetTo > Items.Count * ItemHeight - Height)
					currentOffsetTo = CurrentOffset = Items.Count * ItemHeight - Height;
				if (currentOffsetTo < 0)
					currentOffsetTo = CurrentOffset = 0;
				isPressKey = true;
			}
			if (GameCanvas.keyPressed[13])
			{
				GameCanvas.keyPressed[13] = false;
				if (AllowSelectNone || Items.Count == 0)
					CurrentItemIndex = -1;
				isPressKey = false;
			}
			if (GameCanvas.keyPressed[!Main.isPC ? 5 : 25])
			{
				if (AllowSelectNone || CurrentItemIndex != -1)
					new Thread(() =>
					{
						ItemSelected?.Invoke();
					}).Start();
			}
		}

		internal void Paint(mGraphics g)
		{
			g.setColor(0xD3A46F);
			g.fillRect(X, Y, Width, Height);
			g.setColor(Color.black);
			g.drawRect(X - 1, Y - 1, Width + 1, Height + 1);
			g.setClip(X, Y, Width, Height);
			for (int i = Math.min((CurrentOffset + Height) / ItemHeight, Items.Count - 1); i >= Math.max(CurrentOffset / ItemHeight, 0); i--)
			{
				g.setColor(Color.white);
				if (i == CurrentItemIndex)
					g.setColor(0xFFF9BD);
				g.fillRect(X, Y + i * ItemHeight - CurrentOffset, Width, ItemHeight);
				g.setColor(new Color(0, 0, 0, .3f));
				g.fillRect(X, Y + (i + 1) * ItemHeight - CurrentOffset, Width, 1);
				PaintItemAction?.Invoke(g, i, X, Y + i * ItemHeight - CurrentOffset, Width, ItemHeight);
			}
			if (CurrentOffset <= 0)
			{
				g.setColor(new Color(0, 0, 0, .3f));
				g.fillRect(X, Y - CurrentOffset, Width, 1);
			}
		}

		static bool IsPointerIn(int x, int y, int w, int h)
		{
			return GameCanvas.pxMouse >= x && GameCanvas.pxMouse <= x + w && GameCanvas.pyMouse >= y && GameCanvas.pyMouse <= y + h;
		}
	}
}
