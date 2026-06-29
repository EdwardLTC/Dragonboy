using System.Collections;
using System.Collections.Generic;

namespace Mod.Xmap
{
	internal sealed class MapStepExecutor
	{
		readonly Dictionary<TypeMapNext, IMapStepHandler> handlers;

		internal MapStepExecutor(IEnumerable<IMapStepHandler> stepHandlers)
		{
			handlers = new Dictionary<TypeMapNext, IMapStepHandler>();
			foreach (IMapStepHandler handler in stepHandlers)
			{
				handlers[handler.StepType] = handler;
			}
		}

		internal IEnumerator Execute(MapNext step)
		{
			if (step != null && handlers.TryGetValue(step.Type, out IMapStepHandler handler))
			{
				yield return handler.Execute(step);
			}
		}
	}

}
