using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Mod.AutoTrain;
using Mod.Constants;
using Mod.ModHelper;
using Mod.ModHelper.CommandMod.Chat;
using Mod.Xmap;
using UnityEngine;

namespace Mod.PickMob
{
	internal class PickMobControllerV2 : CoroutineMainThreadAction<PickMobControllerV2>
	{
		const int ATTACK_RANGE_GROUND = 50;
		const int ATTACK_RANGE_FLY_X = 70;
		const int ATTACK_RANGE_GUI = 48;
		const int ITEM_PICK_RANGE = 60;
		const float PICK_ITEM_DELAY = 0.2f;
		const float ATTACK_DELAY = 0.2f;
		const int ID_ICON_ITEM_TDLT = 4387;
		const int MAX_STUCK_TICKS = 10;
		const int WAYPOINT_REACH_SQ = 24 * 24;

		static readonly sbyte[] IdSkillsMelee =
		{
			0, 9, 2, 17, 4
		};
		static readonly sbyte[] IdSkillsCanNotAttack =
		{
			10, 11, 14, 23, 7
		};
		int _lastMoveX;
		int _lastMoveY;
		List<int[]> _path;
		int _pathIdx;
		Mob _skippedMob;
		int _stuckCount;

		Mob _target;

		protected override float Interval => 0.1f;

		#region Main Loop
		protected override IEnumerator OnUpdate()
		{
			if (XmapController.gI.IsActing) yield break;

			Char myChar = Char.myCharz();
			if (myChar.statusMe == 14 || myChar.cHP <= 0 || myChar.meDead) yield break;

			bool isUseTDLT = ItemTime.isExistItem(ID_ICON_ITEM_TDLT);

			if (Pk9rPickMob.IsAutoPickItems && !(Pk9rPickMob.IsTanSat && isUseTDLT))
			{
				bool picked = false;
				yield return PickItems(myChar, isUseTDLT, v => picked = v);
				if (picked) yield break;
			}

			if (myChar.isCharge) yield break;

			myChar.clearFocus(0);

			if (_target != null && !IsMobAlive(_target))
				ClearTarget(myChar);

			if (_target == null)
				_target = FindNearestMob(myChar);

			if (_target == null)
			{
				if (!isUseTDLT)
				{
					Mob next = FindNextMobSpawn();
					if (next != null)
						yield return MoveTo(myChar, next.xFirst - 24, next.yFirst, false);
				}
				yield break;
			}

			Skill skill = GetBestSkill();
			if (skill == null || skill.paintCanNotUseSkill) yield break;

			_target.x = _target.xFirst;
			_target.y = _target.yFirst;

			bool isFly = _target.getTemplate().type == MonsterType.Fly;
			bool inRange = isFly
				? Math.abs(myChar.cx - _target.x) <= ATTACK_RANGE_FLY_X
				: Utils.Distance(myChar, _target) <= ATTACK_RANGE_GROUND;

			if (inRange)
			{
				yield return Attack(myChar, _target, skill, isFly);
			}
			else
			{
				int goalX = isFly ? _target.x : _target.xFirst;
				int goalY = isFly ? Utils.GetYGround(_target.x) : _target.yFirst;

				if (isUseTDLT)
				{
					myChar.cx = isFly ? goalX : goalX - 24;
					myChar.cy = goalY;
					Service.gI().charMove();
				}
				else
				{
					yield return MoveTo(myChar, goalX, goalY, false);
				}
			}
		}
		#endregion

		#region Movement
		IEnumerator MoveTo(Char myChar, int goalX, int goalY, bool isUseTDLT)
		{
			if (isUseTDLT)
			{
				myChar.cx = goalX;
				myChar.cy = goalY;
				Service.gI().charMove();
				yield break;
			}

			if (myChar.currentMovePoint != null)
			{
				if (myChar.cx == _lastMoveX && myChar.cy == _lastMoveY)
				{
					_stuckCount++;
					if (_stuckCount >= MAX_STUCK_TICKS)
					{
						myChar.currentMovePoint = null;
						_stuckCount = 0;
						if (_path != null)
						{
							_pathIdx++;
							if (_pathIdx >= _path.Count)
							{
								_path = null;
								_skippedMob = _target;
								_target = null;
							}
						}
						else
						{
							_skippedMob = _target;
							_target = null;
						}
					}
				}
				else
				{
					_stuckCount = 0;
				}
				_lastMoveX = myChar.cx;
				_lastMoveY = myChar.cy;
				yield break;
			}

			if (_path != null)
			{
				while (_pathIdx < _path.Count)
				{
					int[] wp = _path[_pathIdx];
					int dx = myChar.cx - wp[0];
					int dy = myChar.cy - wp[1];
					if (dx * dx + dy * dy > WAYPOINT_REACH_SQ) break;
					_pathIdx++;
				}
				if (_pathIdx >= _path.Count)
					_path = null;
			}

			if (_path == null)
			{
				_path = MapAStarPathfinder.FindPath(myChar.cx, myChar.cy, goalX, goalY);
				_pathIdx = 1;
			}

			int targetX, targetY;
			if (_path != null && _pathIdx < _path.Count)
			{
				targetX = _path[_pathIdx][0];
				targetY = _path[_pathIdx][1];
			}
			else
			{
				targetX = goalX;
				targetY = goalY;
				_path = null;
			}

			_stuckCount = 0;
			_lastMoveX = myChar.cx;
			_lastMoveY = myChar.cy;
			myChar.currentMovePoint = new MovePoint(targetX, targetY);
		}
		#endregion

		#region Combat
		IEnumerator Attack(Char myChar, Mob mob, Skill skill, bool isFly)
		{
			myChar.currentMovePoint = null;

			if (Pk9rPickMob.IsAttackMonsterBySendCommand)
				yield return AttackByCommand(myChar, mob, skill, isFly);
			else
				yield return AttackByGUI(myChar, mob, skill);
		}

		IEnumerator AttackByCommand(Char myChar, Mob mob, Skill skill, bool isFly)
		{
			if (myChar.myskill != skill)
			{
				Service.gI().selectSkill(skill.template.id);
				myChar.myskill = skill;
			}

			if (isFly)
			{
				myChar.currentMovePoint = null;
				myChar.cx = mob.x + Res.random(-5, 5);
				myChar.cy = mob.y + Res.random(-5, 5);
				Service.gI().charMove();
			}

			if (mSystem.currentTimeMillis() - skill.lastTimeUseThisSkill > skill.coolDown)
			{
				myChar.mobFocus = mob;
				skill.lastTimeUseThisSkill = mSystem.currentTimeMillis();
				MyVector vec = new MyVector();
				vec.addElement(mob);
				Service.gI().sendPlayerAttack(vec, new MyVector(), -1);
				ClearTarget(myChar);
			}

			yield return new WaitForSecondsRealtime(ATTACK_DELAY);
		}

		IEnumerator AttackByGUI(Char myChar, Mob mob, Skill skill)
		{
			if (Res.distance(mob.xFirst, mob.yFirst, myChar.cx, myChar.cy) > ATTACK_RANGE_GUI)
			{
				yield return MoveTo(myChar, mob.xFirst, mob.yFirst, false);
				yield break;
			}

			GameScr.gI().doSelectSkill(skill, true);
			myChar.focusManualTo(mob);
			Utils.DoDoubleClickToObj(mob);
			ClearTarget(myChar);
			yield return new WaitForSecondsRealtime(ATTACK_DELAY);
		}
		#endregion

		#region Item Picking
		IEnumerator PickItems(Char myChar, bool isUseTDLT, Action<bool> result)
		{
			if (TileMap.mapID == myChar.cgender + 21 && GameScr.vItemMap.size() > 0)
			{
				Service.gI().pickItem(((ItemMap)GameScr.vItemMap.elementAt(0)).itemMapID);
				result(true);
				yield break;
			}

			for (int i = 0; i < GameScr.vItemMap.size(); i++)
			{
				ItemMap item = (ItemMap)GameScr.vItemMap.elementAt(i);
				if (!CanPickItem(myChar, item)) continue;

				bool isNear = Res.abs(myChar.cx - item.xEnd) < ITEM_PICK_RANGE && Res.abs(myChar.cy - item.yEnd) < ITEM_PICK_RANGE;

				if (isNear)
				{
					Service.gI().charMove();
					Service.gI().pickItem(item.itemMapID);
					item.countAutoPick++;
					result(true);
					yield return new WaitForSecondsRealtime(PICK_ITEM_DELAY);
					yield break;
				}

				if (isUseTDLT)
				{
					myChar.cx = item.xEnd;
					myChar.cy = item.yEnd;
					Service.gI().charMove();
					Service.gI().pickItem(item.itemMapID);
					item.countAutoPick++;
					result(true);
					yield return new WaitForSecondsRealtime(PICK_ITEM_DELAY);
					yield break;
				}

				yield return MoveTo(myChar, item.xEnd, item.yEnd, false);
				myChar.mobFocus = null;
				result(true);
				yield break;
			}

			result(false);
		}

		static bool CanPickItem(Char myChar, ItemMap itemMap)
		{
			bool isMyItem = itemMap.playerId == myChar.charID || itemMap.playerId == -1;
			if (Pk9rPickMob.IsItemMe && !isMyItem) return false;
			if (Pk9rPickMob.IsLimitTimesPickItem && itemMap.countAutoPick > Pk9rPickMob.TimesAutoPickItemMax) return false;
			if (Pk9rPickMob.IsSkipPickEventItems && itemMap.template.description.Contains("Vật phẩm sự kiện")) return false;
			if (Pk9rPickMob.IdItemPicks.Count != 0 && !Pk9rPickMob.IdItemPicks.Contains(itemMap.template.id)) return false;
			if (Pk9rPickMob.IdItemBlocks.Count != 0 && Pk9rPickMob.IdItemBlocks.Contains(itemMap.template.id)) return false;
			if (Pk9rPickMob.TypeItemPicks.Count != 0 && !Pk9rPickMob.TypeItemPicks.Contains(itemMap.template.type)) return false;
			if (Pk9rPickMob.TypeItemBlocks.Count != 0 && Pk9rPickMob.TypeItemBlocks.Contains(itemMap.template.type)) return false;
			return true;
		}
		#endregion

		#region Mob Selection
		[CanBeNull]
		Mob FindNearestMob(Char myChar)
		{
			if (_skippedMob != null && !IsMobAlive(_skippedMob))
				_skippedMob = null;

			Mob best = null;
			int bestDist = int.MaxValue;
			for (int i = 0; i < GameScr.vMob.size(); i++)
			{
				Mob mob = (Mob)GameScr.vMob.elementAt(i);
				if (!IsMobAlive(mob) || mob == _skippedMob) continue;
				int d = (mob.xFirst - myChar.cx) * (mob.xFirst - myChar.cx) +
				        (mob.yFirst - myChar.cy) * (mob.yFirst - myChar.cy);
				if (d < bestDist)
				{
					bestDist = d;
					best = mob;
				}
			}
			return best;
		}

		[CanBeNull]
		static Mob FindNextMobSpawn()
		{
			Mob best = null;
			long bestTime = mSystem.currentTimeMillis();
			for (int i = 0; i < GameScr.vMob.size(); i++)
			{
				Mob mob = (Mob)GameScr.vMob.elementAt(i);
				if (mob.isMobMe || !FilterMob(mob)) continue;

				bool ne = Pk9rPickMob.IsNeSieuQuai && !ItemTime.isExistItem(ID_ICON_ITEM_TDLT);
				if (ne && mob.getTemplate().hp >= 3000)
				{
					if (mob.levelBoss != 0)
					{
						bool found = false;
						Mob sq = null;
						for (int j = 0; j < GameScr.vMob.size(); j++)
						{
							sq = (Mob)GameScr.vMob.elementAt(j);
							if (sq.countDie == 10 && (sq.status == 0 || sq.status == 1))
							{
								found = true;
								break;
							}
						}
						if (!found) continue;
						mob.lastTimeDie = sq.lastTimeDie;
					}
					else if (mob.countDie == 10 && (mob.status == 0 || mob.status == 1))
					{
						continue;
					}
				}

				if (mob.lastTimeDie < bestTime)
				{
					bestTime = mob.lastTimeDie;
					best = mob;
				}
			}
			return best;
		}

		static bool IsMobAlive(Mob mob)
		{
			if (mob.status == 0 || mob.status == 1 || mob.hp <= 0 || mob.isMobMe) return false;
			if (mob.levelBoss != 0 && Pk9rPickMob.IsNeSieuQuai && !ItemTime.isExistItem(ID_ICON_ITEM_TDLT)) return false;
			return FilterMob(mob);
		}

		static bool FilterMob(Mob mob)
		{
			if (Pk9rPickMob.IdMobsTanSat.Count != 0 && !Pk9rPickMob.IdMobsTanSat.Contains(mob.mobId)) return false;
			if (Pk9rPickMob.TypeMobsTanSat.Count != 0 && !Pk9rPickMob.TypeMobsTanSat.Contains(mob.getTemplate().mobTemplateId)) return false;
			return true;
		}
		#endregion

		#region Skill Selection
		[CanBeNull]
		static Skill GetBestSkill()
		{
			Skill best = null;
			SkillTemplate tmpl = new SkillTemplate();
			foreach (sbyte id in Pk9rPickMob.IdSkillsTanSat)
			{
				tmpl.id = id;
				Skill s = Char.myCharz().getSkill(tmpl);
				if (IsSkillBetter(s, best)) best = s;
			}
			return best;
		}

		static bool IsSkillBetter(Skill candidate, Skill current)
		{
			if (candidate == null || !CanUseSkill(candidate)) return false;
			if (current != null)
			{
				bool prioritized = candidate.template.id == 17 && current.template.id == 2 ||
				                   candidate.template.id == 9 && current.template.id == 0;
				if (current.coolDown >= candidate.coolDown && !prioritized) return false;
			}
			return true;
		}

		static bool CanUseSkill(Skill skill)
		{
			if (mSystem.currentTimeMillis() - skill.lastTimeUseThisSkill > skill.coolDown)
				skill.paintCanNotUseSkill = false;
			if (skill.paintCanNotUseSkill && !IdSkillsMelee.Contains(skill.template.id)) return false;
			if (IdSkillsCanNotAttack.Contains(skill.template.id)) return false;
			if (mSystem.currentTimeMillis() - skill.lastTimeUseThisSkill < skill.coolDown) return false;
			if (Char.myCharz().cMP < GetManaUse(skill)) return false;
			return true;
		}

		static int GetManaUse(Skill skill)
		{
			if (skill.template.manaUseType == 2) return 1;
			if (skill.template.manaUseType == 1)
				return (int)(skill.manaUse * Char.myCharz().cMPFull / 100);
			return skill.manaUse;
		}
		#endregion

		#region Lifecycle
		void ClearTarget(Char myChar)
		{
			_target = null;
			_path = null;
			myChar.currentMovePoint = null;
		}

		protected override void OnStart()
		{
			_target = null;
			_skippedMob = null;
			_path = null;
			_stuckCount = 0;
		}

		protected override void OnStop()
		{
			_target = null;
			_skippedMob = null;
			_path = null;
			_stuckCount = 0;
			Char.myCharz().currentMovePoint = null;
		}

		[ChatCommand("ts2")]
		internal static void TogglePickMobV2()
		{
			gI.Toggle();
			GameScr.info1.addInfo("PickMob V2: " + (gI.IsActing ? mResources.ON : mResources.OFF) + '!', 0);
		}
		#endregion
	}
}
