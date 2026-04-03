using JetBrains.Annotations;

namespace Mod.ModHelper.CommandMod.Chat
{
    [MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
    public class ChatCommandAttribute : BaseCommandAttribute
    {
        public string command;

        public ChatCommandAttribute(string command)
        {
            this.command = command;
        }
    }
}