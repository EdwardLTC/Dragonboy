using JetBrains.Annotations;

namespace Mod.ModHelper.CommandMod.Hotkey
{
    [MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
    public class HotkeyCommandAttribute : BaseCommandAttribute
    {
        public char key;
        public string agrs = "";

        public HotkeyCommandAttribute(char key)
        {
            this.key = key;
        }
    }
}