#import <Foundation/Foundation.h>

// Stubs for Unity P/Invoke from iOSPlugins.cs ([DllImport("__Internal")]).
// The original game shipped native Obj-C for SMS / rotation / IAP; this project
// had no plugin, so the iOS linker could not resolve these symbols.

extern "C" {

// C identifiers here use a single leading underscore; Darwin exports them with an extra
// linker prefix, matching IL2CPP references like "__SMSsend" from C# `_SMSsend`.

void _SMSsend(const char* tophone, const char* withtext, int n)
{
	(void)tophone;
	(void)withtext;
	(void)n;
}

int _unpause(void)
{
	return 0;
}

int _checkRotation(void)
{
	return 0;
}

int _back(void)
{
	return 0;
}

int _Send(void)
{
	return 0;
}

void _purchaseItem(const char* itemID, const char* userName, const char* gameID)
{
	(void)itemID;
	(void)userName;
	(void)gameID;
}

}
