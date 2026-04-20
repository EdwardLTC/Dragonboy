using System.Collections;
using Mod.Constants;
using Mod.ModHelper;
using Mod.Xmap;
using UnityEngine;

namespace Mod.PickMob
{
	public class PickMobControllerV2 : CoroutineMainThreadAction<PickMobControllerV2>
	{
		const float PICK_ITEM_DELAY = 0.2f;
		const float ATTACK_DELAY = 0.1f;
		const int ID_ICON_ITEM_TDLT = 4387;

		protected override float Interval => 0f;

		protected override IEnumerator OnUpdate()
		{
			if (XmapController.gI.IsActing)
			{
				yield break;
			}

			Char myChar = Char.myCharz();

			if (myChar.statusMe == 14 || myChar.cHP <= 0)
			{
				yield break;
			}

			bool isUseTDLT = ItemTime.isExistItem(ID_ICON_ITEM_TDLT);
			bool isTanSatTDLT = Pk9rPickMob.IsTanSat && isUseTDLT;

			if (Pk9rPickMob.IsAutoPickItems && !isTanSatTDLT)
			{
				if (TileMap.mapID == myChar.cgender + 21 && GameScr.vItemMap.size() > 0)
				{
					Service.gI().pickItem(((ItemMap)GameScr.vItemMap.elementAt(0)).itemMapID);
					yield break;
				}

				bool pickedAny = false;
				for (int i = 0; i < GameScr.vItemMap.size(); i++)
				{
					ItemMap itemMap = (ItemMap)GameScr.vItemMap.elementAt(i);
					TypePickItem type = GetTypePickItem(itemMap);
					if (type == TypePickItem.CanNotPickItem)
					{
						continue;
					}

					pickedAny = true;
					switch (type)
					{
					case TypePickItem.PickItemTDLT:
						myChar.cx = itemMap.xEnd;
						myChar.cy = itemMap.yEnd;
						Service.gI().charMove();
						Service.gI().pickItem(itemMap.itemMapID);
						itemMap.countAutoPick++;
						yield return new WaitForSecondsRealtime(PICK_ITEM_DELAY);
						break;
					case TypePickItem.PickItemTanSat:
						Move(itemMap.xEnd, itemMap.yEnd);
						myChar.mobFocus = null;
						yield return new WaitForSecondsRealtime(PICK_ITEM_DELAY);
						break;
					case TypePickItem.PickItemNormal:
						Service.gI().charMove();
						Service.gI().pickItem(itemMap.itemMapID);
						itemMap.countAutoPick++;
						yield return new WaitForSecondsRealtime(PICK_ITEM_DELAY);
						break;
					}
				}

				if (pickedAny)
				{
					yield break;
				}
			}

			if (Pk9rPickMob.IsTanSat)
			{
				yield return DoSlaughter(myChar, isUseTDLT);
			}
		}

		static IEnumerator DoSlaughter(Char myChar, bool isUseTDLT)
		{
			if (myChar.isCharge)
			{
				yield return new WaitForSecondsRealtime(ATTACK_DELAY);
				yield break;
			}

			myChar.clearFocus(0);

			if (myChar.mobFocus != null && !MobPicker.IsMobTanSat(myChar.mobFocus))
			{
				myChar.mobFocus = null;
			}

			if (myChar.mobFocus == null)
			{
				myChar.mobFocus = MobPicker.GetMobTanSat();
				if (myChar.mobFocus != null && isUseTDLT)
				{
					myChar.cx = myChar.mobFocus.xFirst - 24;
					myChar.cy = myChar.mobFocus.yFirst;
					Service.gI().charMove();
				}
			}

			if (myChar.mobFocus != null)
			{
				if (myChar.skillInfoPaint() == null)
				{
					Skill skill = SkillPicker.GetSkillAttack();
					if (skill != null && !skill.paintCanNotUseSkill)
					{
						AttackMob(myChar, skill);
					}
				}
			}
			else if (!isUseTDLT)
			{
				Mob mob = MobPicker.GetMobNext();
				if (mob != null)
				{
					Move(mob.xFirst - 24, mob.yFirst);
				}
			}

			yield return new WaitForSecondsRealtime(ATTACK_DELAY);
		}

		static void AttackMob(Char myChar, Skill skill)
		{
			Mob mobFocus = myChar.mobFocus;
			mobFocus.x = mobFocus.xFirst;
			mobFocus.y = mobFocus.yFirst;

			if (Pk9rPickMob.IsAttackMonsterBySendCommand)
			{
				AttackMobBySendCommand(myChar, skill, mobFocus);
			}
			else
			{
				AttackMobByFocus(myChar, skill, mobFocus);
			}
		}

		static void AttackMobBySendCommand(Char myChar, Skill skill, Mob mobFocus)
		{
			if (myChar.myskill != skill)
			{
				Service.gI().selectSkill(skill.template.id);
				myChar.myskill = skill;
			}

			if (mobFocus.getTemplate().type == MonsterType.Fly)
			{
				if (Math.abs(myChar.cx - mobFocus.x) > 70)
				{
					Move(mobFocus.x, Utils.GetYGround(mobFocus.x));
				}
				else
				{
					myChar.currentMovePoint = null;
					myChar.cx = mobFocus.x + Res.random(-5, 5);
					myChar.cy = mobFocus.y + Res.random(-5, 5);
					Service.gI().charMove();
				}
			}
			else
			{
				Move(mobFocus.xFirst, mobFocus.yFirst);
			}

			bool inRange = Utils.Distance(myChar, mobFocus) <= 50 ||
			               mobFocus.getTemplate().type == MonsterType.Fly &&
			               Math.abs(myChar.cx - mobFocus.x) <= 70;

			if (inRange && mSystem.currentTimeMillis() - skill.lastTimeUseThisSkill > skill.coolDown + 100L)
			{
				myChar.mobFocus = mobFocus;
				skill.lastTimeUseThisSkill = mSystem.currentTimeMillis();
				MyVector targets = new MyVector();
				targets.addElement(mobFocus);
				Service.gI().sendPlayerAttack(targets, new MyVector(), -1);
			}
		}

		static void AttackMobByFocus(Char myChar, Skill skill, Mob mobFocus)
		{
			GameScr.gI().doSelectSkill(skill, true);
			if (Res.distance(mobFocus.xFirst, mobFocus.yFirst, myChar.cx, myChar.cy) <= 48)
			{
				myChar.focusManualTo(mobFocus);
				Utils.DoDoubleClickToObj(mobFocus);
			}
			else
			{
				Move(mobFocus.xFirst, mobFocus.yFirst);
			}
		}

		static void Move(int x, int y)
		{
			Char myChar = Char.myCharz();
			if (!Pk9rPickMob.IsVuotDiaHinh)
			{
				myChar.currentMovePoint = new MovePoint(x, y);
				return;
			}

			int[] vs = GetPointYsdMax(myChar.cx, x);
			if (vs[1] >= y || vs[1] >= myChar.cy && (myChar.statusMe == 2 || myChar.statusMe == 1))
			{
				vs[0] = x;
				vs[1] = y;
			}

			myChar.currentMovePoint = new MovePoint(vs[0], vs[1]);
		}

		static int GetYsd(int xsd)
		{
			int dmin = TileMap.pxh;
			int ysdBest = -1;
			int myCharY = Char.myCharz().cy;
			for (int i = 24; i < TileMap.pxh; i += 24)
			{
				if (!TileMap.tileTypeAt(xsd, i, 2))
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

		#region Item picking helpers
		static TypePickItem GetTypePickItem(ItemMap itemMap)
		{
			Char myChar = Char.myCharz();
			if (Pk9rPickMob.IsItemMe && itemMap.playerId != myChar.charID && itemMap.playerId != -1)
				return TypePickItem.CanNotPickItem;

			if (Pk9rPickMob.IsLimitTimesPickItem && itemMap.countAutoPick > Pk9rPickMob.TimesAutoPickItemMax)
				return TypePickItem.CanNotPickItem;

			if (!FilterItemPick(itemMap))
				return TypePickItem.CanNotPickItem;

			if (Pk9rPickMob.IsSkipPickEventItems && itemMap.template.description.Contains("Vật phẩm sự kiện"))
				return TypePickItem.CanNotPickItem;

			if (Res.abs(myChar.cx - itemMap.xEnd) < 60 && Res.abs(myChar.cy - itemMap.yEnd) < 60)
				return TypePickItem.PickItemNormal;

			if (ItemTime.isExistItem(ID_ICON_ITEM_TDLT))
				return TypePickItem.PickItemTDLT;

			if (Pk9rPickMob.IsTanSat)
				return TypePickItem.PickItemTanSat;

			return TypePickItem.CanNotPickItem;
		}

		static bool FilterItemPick(ItemMap itemMap)
		{
			if (Pk9rPickMob.IdItemPicks.Count != 0 && !Pk9rPickMob.IdItemPicks.Contains(itemMap.template.id))
				return false;

			if (Pk9rPickMob.IdItemBlocks.Count != 0 && Pk9rPickMob.IdItemBlocks.Contains(itemMap.template.id))
				return false;

			if (Pk9rPickMob.TypeItemPicks.Count != 0 && !Pk9rPickMob.TypeItemPicks.Contains(itemMap.template.type))
				return false;

			if (Pk9rPickMob.TypeItemBlocks.Count != 0 && Pk9rPickMob.TypeItemBlocks.Contains(itemMap.template.type))
				return false;

			return true;
		}

		enum TypePickItem
		{
			CanNotPickItem,
			PickItemNormal,
			PickItemTDLT,
			PickItemTanSat
		}
		#endregion
	}
}
