using System;
using Mod.ModHelper;
using Mod.ModHelper.CommandMod.Chat;
using Mod.ModHelper.CommandMod.Hotkey;
using Mod.R;
using UnityEngine;

namespace Mod.Auto
{
	internal class AutoSendAttack : ThreadAction<AutoSendAttack>
	{
		internal override int Interval => 100;

		protected override void action()
		{
			if (Char.myCharz().meDead || Char.myCharz().cHP <= 0 || Char.myCharz().statusMe == 14 || Char.myCharz().statusMe == 5 || Char.myCharz().myskill.template.type == 3 || Char.myCharz().myskill.template.id == 10 || Char.myCharz().myskill.template.id == 11 || Char.myCharz().isWaitMonkey || Char.myCharz().isCharge || (Char.myCharz().myskill.paintCanNotUseSkill && !GameCanvas.panel.isShow))
			{
				return;
			}
			if (GameScr.gI().isMeCanAttackMob(Char.myCharz().mobFocus))
			{
				SendAttackToMobFocus();
				Char.myCharz().myskill.lastTimeUseThisSkill = mSystem.currentTimeMillis();
			}
			else if (Char.myCharz().charFocus != null && isMeCanAttackChar(Char.myCharz().charFocus) && System.Math.Abs(Char.myCharz().charFocus.cx - Char.myCharz().cx) < Char.myCharz().myskill.dx * 1.7)
			{
				Char.myCharz().myskill.lastTimeUseThisSkill = mSystem.currentTimeMillis();
				SendAttackToCharFocus();
				Char.myCharz().myskill.lastTimeUseThisSkill = mSystem.currentTimeMillis();
			}
		}

        [ChatCommand("ak"),HotkeyCommand('a')]
        internal static void toggleAutoAttack()
        {
            gI.toggle();
            GameScr.info1.addInfo(Strings.autoAttack + ": " + (gI.IsActing ? mResources.ON : mResources.OFF) + '!', 0);
        }
        
       public static void SendAttackToCharFocus()
        {
	        try
	        {
		        if (!Char.myCharz().isWaitMonkey)
		        {
			        MyVector myVector = new MyVector();
			        myVector.addElement(Char.myCharz().charFocus);
			        Service.gI().sendPlayerAttack(new MyVector(), myVector, 2);
		        }
	        }
	        catch(Exception ex)
	        { 
		        Debug.LogError("Failed to send attack to char focus. " + ex);
	        }
        }

        static void SendAttackToMobFocus()
        {
	        try
	        {
		        MyVector myVector = new MyVector();
		        myVector.addElement(Char.myCharz().mobFocus);
		        Service.gI().sendPlayerAttack(myVector, new MyVector(), -1);
	        }
	        catch(Exception ex)
	        {
		        Debug.LogError("Failed to send attack to mob focus. " + ex);
	        }
        }
        
        static bool isMeCanAttackChar(Char ch)
        {
	        if (TileMap.mapID == 113)
	        {
		        if (ch != null && Char.myCharz().myskill != null)
		        {
			        if (ch.cTypePk != 5)
			        {
				        return ch.cTypePk == 3;
			        }
			        return true;
		        }
		        return false;
	        }
	        if (ch != null && Char.myCharz().myskill != null)
	        {
		        if (ch.statusMe == 14 || ch.statusMe == 5 || Char.myCharz().myskill.template.type == 2 || ((Char.myCharz().cFlag != 8 || ch.cFlag == 0) && (Char.myCharz().cFlag == 0 || ch.cFlag != 8) && (Char.myCharz().cFlag == ch.cFlag || Char.myCharz().cFlag == 0 || ch.cFlag == 0) && (ch.cTypePk != 3 || Char.myCharz().cTypePk != 3) && Char.myCharz().cTypePk != 5 && ch.cTypePk != 5 && (Char.myCharz().cTypePk != 1 || ch.cTypePk != 1) && (Char.myCharz().cTypePk != 4 || ch.cTypePk != 4)))
		        {
			        if (Char.myCharz().myskill.template.type == 2)
			        {
				        return ch.cTypePk != 5;
			        }
			        return false;
		        }
		        return true;
	        }
	        return false;
        }
    }
}
