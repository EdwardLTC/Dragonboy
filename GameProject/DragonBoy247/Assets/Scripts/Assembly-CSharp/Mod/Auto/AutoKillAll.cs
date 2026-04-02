using System.Collections;
using Mod.ModHelper;

namespace Mod.Auto
{
	public class AutoKillAll :CoroutineMainThreadAction<AutoKillAll>
	{
		protected override float Interval => 100;

		protected override IEnumerator OnUpdate()
		{
			if (Char.myCharz().cFlag == 0)
			{
				Service.gI().getFlag(1, 8);
				yield return null;
			}
			
			for (int i = 0; i < GameScr.vCharInMap.size(); i++)
			{
				Char obj = (Char)GameScr.vCharInMap.elementAt(i);
				if (!string.IsNullOrEmpty(obj.cName) && !obj.isPet && !obj.isMiniPet && !obj.cName.StartsWith("#") && !obj.cName.StartsWith("$") && obj.cName != "Trọng tài" && obj.cFlag != 0 && !char.IsUpper(char.Parse(obj.cName.Substring(0, 1))) && obj.cHP > 0)
				{
					Char.myCharz().mobFocus = null;
					Char.myCharz().npcFocus = null;
					Char.myCharz().itemFocus = null;
					
					if (!obj.meDead && obj.cHP > 0 && obj.cFlag != 0 && obj.charID > 0 && Res.distance(Char.myCharz().cx, Char.myCharz().cy, Char.myCharz().charFocus.cx, Char.myCharz().charFocus.cy) > 50)
					{
						Utils.TeleportMyChar(obj.cx, obj.cy);
						Char.myCharz().charFocus = obj;
						AutoSendAttack.SendAttackToCharFocus();
					}
				}
			}
		}

		protected override void OnStart()
		{
			if (Char.myCharz().cFlag == 0)
			{
				Service.gI().getFlag(1, 8);
				return;
			}
			base.OnStart(); 
		}
		
	}
}
