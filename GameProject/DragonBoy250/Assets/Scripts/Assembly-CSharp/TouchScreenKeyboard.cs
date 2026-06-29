using System;

public class TouchScreenKeyboard
{
    private UnityEngine.TouchScreenKeyboard _keyboard;

    private TouchScreenKeyboard(UnityEngine.TouchScreenKeyboard keyboard)
    {
        _keyboard = keyboard;
    }

    private bool IsAlive()
    {
        return _keyboard != null;
    }

    private void Invalidate()
    {
        _keyboard = null;
    }

    public static bool hideInput
    {
        get => UnityEngine.TouchScreenKeyboard.hideInput;
        set => UnityEngine.TouchScreenKeyboard.hideInput = value;
    }

    public static bool visible => UnityEngine.TouchScreenKeyboard.visible;

    public bool done
    {
        get
        {
            if (!IsAlive())
                return true;

            try
            {
                return _keyboard.status == UnityEngine.TouchScreenKeyboard.Status.Done;
            }
            catch (NullReferenceException)
            {
                Invalidate();
                return true;
            }
        }
    }

    public UnityEngine.TouchScreenKeyboard.Status status
    {
        get
        {
            if (!IsAlive())
                return UnityEngine.TouchScreenKeyboard.Status.Canceled;

            try
            {
                return _keyboard.status;
            }
            catch (NullReferenceException)
            {
                Invalidate();
                return UnityEngine.TouchScreenKeyboard.Status.Canceled;
            }
        }
    }

    public bool active
    {
        get
        {
            if (!IsAlive())
                return false;

            try
            {
                return _keyboard.active;
            }
            catch (NullReferenceException)
            {
                Invalidate();
                return false;
            }
        }

        set
        {
            if (!IsAlive())
                return;

            try
            {
                _keyboard.active = value;
            }
            catch (NullReferenceException)
            {
                Invalidate();
            }
        }
    }

    public string text
    {
        get
        {
            if (!IsAlive())
                return string.Empty;

            try
            {
                return _keyboard.text ?? "";
            }
            catch (NullReferenceException)
            {
                Invalidate();
                return "";
            }
        }

        set
        {
            if (!IsAlive())
                return;

            try
            {
                _keyboard.text = value ?? "";
            }
            catch (NullReferenceException)
            {
                Invalidate();
            }
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
}