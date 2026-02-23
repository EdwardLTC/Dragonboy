using System.Threading;

namespace Mod.ModHelper
{
	public abstract class ThreadAction<T> where T : ThreadAction<T>, new()
	{
		Thread threadAction;
		volatile bool isActing;

		public static T gI { get; } = new T();

		public bool IsActing => isActing;

		protected virtual bool UseUpdateLoop => false;

		internal virtual int Interval => 0;

		protected virtual void update()
		{
		}

		protected abstract void action();

		public void performAction()
		{
			onStart();
		}

		internal void toggle(bool? value = null)
		{
			if ((value == null || value == false) && isActing)
			{
				onStop();
			}
			else if ((value == null || value == true) && !isActing)
			{
				onStart();
			}
		}

		internal void toggle(bool value)
		{
			toggle((bool?)value);
		}

		protected virtual void onStop()
		{
			isActing = false;
			threadAction?.Interrupt();
		}

		protected virtual void onStart()
		{
			if (threadAction?.IsAlive == true)
				return;

			isActing = true;
			threadAction = new Thread(executeAction)
			{
				IsBackground = true
			};
			threadAction.Start();
		}

		void executeAction()
		{
			try
			{
				if (UseUpdateLoop)
				{
					while (isActing)
					{
						update();
						try
						{
							Thread.Sleep(Interval);
						}
						catch (ThreadInterruptedException)
						{
							// Interrupted to exit sleep early when stopping.
						}
					}
					return;
				}

				action();
			}
			finally
			{
				isActing = false;
			}
		}
	}
}
