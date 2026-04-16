using System.Collections;
using System.Linq;
using JetBrains.Annotations;
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

		static readonly sbyte[] IdSkillsMelee =
		{
			0, 9, 2, 17, 4
		};
		static readonly sbyte[] IdSkillsCanNotAttack =
		{
			10, 11, 14, 23, 7
		};

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

			if (myChar.mobFocus != null && !IsMobTanSat(myChar.mobFocus))
				myChar.mobFocus = null;

			if (myChar.mobFocus == null)
			{
				myChar.mobFocus = GetMobTanSat();
				if (isUseTDLT && myChar.mobFocus != null)
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
					Skill skill = GetSkillAttack();
					if (skill != null && !skill.paintCanNotUseSkill)
						AttackMob(myChar, skill);
				}
			}
			else if (!isUseTDLT)
			{
				Mob mob = GetMobNext();
				if (mob != null)
					Move(mob.xFirst - 24, mob.yFirst);
			}

			yield return new WaitForSecondsRealtime(ATTACK_DELAY);
		}

		#region Attack
		static void AttackMob(Char myChar, Skill skill)
		{
			Mob mobFocus = myChar.mobFocus;
			mobFocus.x = mobFocus.xFirst;
			mobFocus.y = mobFocus.yFirst;

			if (Pk9rPickMob.IsAttackMonsterBySendCommand)
				AttackMobBySendCommand(myChar, skill, mobFocus);
			else
				AttackMobByFocus(myChar, skill, mobFocus);
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
		#endregion

		#region Movement
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
		#endregion

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

		#region Mob selection helpers
		static Mob GetMobTanSat()
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

		static Mob GetMobNext()
		{
			Mob earliest = null;
			long earliestTime = mSystem.currentTimeMillis();
			for (int i = 0; i < GameScr.vMob.size(); i++)
			{
				Mob mob = (Mob)GameScr.vMob.elementAt(i);
				if (IsMobNext(mob) && mob.lastTimeDie < earliestTime)
				{
					earliest = mob;
					earliestTime = mob.lastTimeDie;
				}
			}

			return earliest;
		}

		static bool IsMobTanSat(Mob mob)
		{
			if (mob.status == 0 || mob.status == 1 || mob.hp <= 0 || mob.isMobMe)
				return false;

			if (mob.levelBoss != 0 && Pk9rPickMob.IsNeSieuQuai && !ItemTime.isExistItem(ID_ICON_ITEM_TDLT))
				return false;

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
		#endregion

		#region Skill helpers
		[CanBeNull]
		public static Skill GetSkillAttack()
		{
			Skill best = null;
			SkillTemplate template = new SkillTemplate();
			foreach (sbyte id in Pk9rPickMob.IdSkillsTanSat)
			{
				template.id = id;
				Skill candidate = Char.myCharz().getSkill(template);
				if (IsSkillBetter(candidate, best))
					best = candidate;
			}

			return best;
		}

		static bool IsSkillBetter(Skill candidate, Skill current)
		{
			if (candidate == null || !CanUseSkill(candidate))
				return false;

			if (current == null)
				return true;

			bool isPrioritize = candidate.template.id == 17 && current.template.id == 2 ||
			                    candidate.template.id == 9 && current.template.id == 0;

			return current.coolDown < candidate.coolDown || isPrioritize;
		}

		static bool CanUseSkill(Skill skill)
		{
			if (mSystem.currentTimeMillis() - skill.lastTimeUseThisSkill > skill.coolDown)
				skill.paintCanNotUseSkill = false;

			if (skill.paintCanNotUseSkill && !IdSkillsMelee.Contains(skill.template.id))
				return false;

			if (IdSkillsCanNotAttack.Contains(skill.template.id))
				return false;

			if (mSystem.currentTimeMillis() - skill.lastTimeUseThisSkill < skill.coolDown)
				return false;

			if (Char.myCharz().cMP < GetManaUseSkill(skill))
				return false;

			return true;
		}

		static int GetManaUseSkill(Skill skill)
		{
			if (skill.template.manaUseType == 2)
				return 1;
			if (skill.template.manaUseType == 1)
				return (int)(skill.manaUse * Char.myCharz().cMPFull / 100);
			return skill.manaUse;
		}
		#endregion
	}
}
