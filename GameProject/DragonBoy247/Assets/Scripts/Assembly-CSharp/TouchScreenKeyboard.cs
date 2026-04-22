public class TouchScreenKeyboard
{
	readonly UnityEngine.TouchScreenKeyboard _keyboard;

	TouchScreenKeyboard(UnityEngine.TouchScreenKeyboard keyboard)
	{
		_keyboard = keyboard;
	}

	public static bool hideInput
	{
		get => UnityEngine.TouchScreenKeyboard.hideInput;
		set => UnityEngine.TouchScreenKeyboard.hideInput = value;
	}

	public static bool visible => UnityEngine.TouchScreenKeyboard.visible;

	public bool done => _keyboard != null && _keyboard.done;

	public bool active
	{
		get => _keyboard != null && _keyboard.active;
		set
		{
			if (_keyboard != null)
				_keyboard.active = value;
		}
	}

	public string text
	{
		get => _keyboard?.text ?? string.Empty;
		set
		{
			if (_keyboard != null)
				_keyboard.text = value ?? string.Empty;
		}
	}

	public static TouchScreenKeyboard Open(string text, TouchScreenKeyboardType t, bool b1, bool b2, bool type, bool b3,
		string caption)
	{
		UnityEngine.TouchScreenKeyboardType unityType = t switch
		{
			TouchScreenKeyboardType.ASCIICapable => UnityEngine.TouchScreenKeyboardType.ASCIICapable,
			TouchScreenKeyboardType.NumberPad => UnityEngine.TouchScreenKeyboardType.NumberPad,
			_ => UnityEngine.TouchScreenKeyboardType.Default
		};

		UnityEngine.TouchScreenKeyboard keyboard =
			UnityEngine.TouchScreenKeyboard.Open(text ?? string.Empty, unityType, b1, b2, type, b3, caption ?? string.Empty);

		return keyboard != null ? new TouchScreenKeyboard(keyboard) : null;
	}

	public static void Clear()
	{
		// Unity doesn't expose a global "clear current keyboard buffer" API.
		// Callers already clear via their own stored instance (e.g. `kb.text = ""`).
	}
}
