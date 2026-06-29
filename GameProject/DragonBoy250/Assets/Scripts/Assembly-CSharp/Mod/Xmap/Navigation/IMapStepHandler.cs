using System.Collections;

namespace Mod.Xmap
{
	internal interface IMapStepHandler
	{
		TypeMapNext StepType { get; }

		IEnumerator Execute(MapNext step);
	}

}
