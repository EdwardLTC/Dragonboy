using System;

namespace Mod.TeleportMenu
{
	internal class TeleportChar
	{

		internal TeleportChar(int charId)
		{
			Name = "no name";
			ID = charId;
			LastTimeTeleportTo = mSystem.currentTimeMillis();
		}

		internal TeleportChar(Char ch)
		{
			Name = ch.GetNameWithoutClanTag();
			ID = ch.charID;
			LastTimeTeleportTo = mSystem.currentTimeMillis();
		}

		internal TeleportChar(string name, int charID, long lastTimeTeleportTo)
		{
			Name = name;
			ID = charID;
			LastTimeTeleportTo = lastTimeTeleportTo;
		}
		internal int ID { get; set; }

		internal string Name { get; set; }

		internal long LastTimeTeleportTo { get; set; }

		public override string ToString()
		{
			return $"{Name} [{ID}]";
		}

		public override bool Equals(object obj)
		{
			if (obj is TeleportChar teleportChar)
			{
				return teleportChar.Name == Name && teleportChar.ID == ID;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(ID, Name);
		}
	}
}
