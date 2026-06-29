using System;
using System.Collections.Generic;
using System.Linq;
using Mod.R;
using Newtonsoft.Json;

namespace Mod.AccountManager
{
	internal static class InGameAccountDAO
	{
		const string StorageKeyAccounts = "account_manager_accounts";
		const string StorageKeySelectedIndex = "account_manager_selected_account_index";

		internal static List<Account> Accounts { get; } = new List<Account>();

		internal static int SelectedAccountIndex { get; set; } = -1;

		internal static Account SelectedAccount =>
			SelectedAccountIndex < 0 || SelectedAccountIndex >= Accounts.Count ? null : Accounts[SelectedAccountIndex];

		internal static void Load()
		{
			List<Account> disk = ReadAccountsFromStorage();
			Accounts.Clear();
			Accounts.AddRange(disk);
			SelectedAccountIndex = (int)ModStorage.ReadLong(StorageKeySelectedIndex, -1);
			if (SelectedAccountIndex >= Accounts.Count)
			{
				SelectedAccountIndex = -1;
			}
		}

		internal static void Save()
		{
			if (Accounts == null || Accounts.Count == 0)
			{
				return;
			}
			ModStorage.WriteString(StorageKeyAccounts, JsonConvert.SerializeObject(Accounts));
			ModStorage.WriteLong(StorageKeySelectedIndex, SelectedAccountIndex);
		}

		static void RefreshAccountsFromDiskPreserveSelection()
		{
			int savedIdx = SelectedAccountIndex;
			Account savedAcc = SelectedAccount;
			List<Account> disk = ReadAccountsFromStorage();
			Accounts.Clear();
			Accounts.AddRange(disk);
			if (savedAcc != null)
			{
				int found = FindAccountIndexByIdentity(Accounts, savedAcc);
				SelectedAccountIndex = found >= 0 ? found : savedIdx < Accounts.Count ? savedIdx : -1;
			}
			else
			{
				SelectedAccountIndex = savedIdx < Accounts.Count ? savedIdx : -1;
			}
		}

		internal static void SaveMergedLoggedInAccount(Account live)
		{
			if (live == null)
			{
				return;
			}
			List<Account> disk = ReadAccountsFromStorage();
			int diskIdx = FindAccountIndexByIdentity(disk, live);
			Account snapshot = CloneAccount(live);
			if (diskIdx >= 0)
			{
				disk[diskIdx] = snapshot;
			}
			else
			{
				disk.Add(snapshot);
			}
			int newSelectedIdx = FindAccountIndexByIdentity(disk, snapshot);
			Accounts.Clear();
			Accounts.AddRange(disk);
			SelectedAccountIndex = newSelectedIdx;
			ModStorage.WriteString(StorageKeyAccounts, JsonConvert.SerializeObject(Accounts));
			ModStorage.WriteLong(StorageKeySelectedIndex, SelectedAccountIndex);
		}

		static List<Account> ReadAccountsFromStorage()
		{
			string jsonData = ModStorage.ReadString(StorageKeyAccounts, "[]");
			return JsonConvert.DeserializeObject<List<Account>>(jsonData) ?? new List<Account>();
		}

		static int FindAccountIndexByIdentity(List<Account> list, Account account)
		{
			if (list == null || account == null)
			{
				return -1;
			}
			for (int i = 0; i < list.Count; i++)
			{
				if (AccountsMatchIdentity(list[i], account))
				{
					return i;
				}
			}
			return -1;
		}

		static bool AccountsMatchIdentity(Account a, Account b)
		{
			if (a == null || b == null)
			{
				return false;
			}
			return a.Username == b.Username && a.Server == b.Server;
		}

		static Account CloneAccount(Account a)
		{
			return JsonConvert.DeserializeObject<Account>(JsonConvert.SerializeObject(a));
		}

		internal static void AddUserAoToAccountManager()
		{
			RefreshAccountsFromDiskPreserveSelection();
			string userAo = Rms.loadRMSString("userAo" + ServerListScreen.ipSelect);
			if (string.IsNullOrEmpty(userAo))
			{
				return;
			}
			if (Accounts.Any(acc => acc.Username == userAo))
			{
				GameCanvas.startOKDlg(Strings.inGameAccountManagerUnregisteredAccountAlreadyAdded + '!');
				return;
			}
			Account account = new Account
			{
				Username = userAo,
				Server = new Server(ServerListScreen.ipSelect),
				LastTimeLogin = DateTime.Now
			};
			Accounts.Add(account);
			SelectedAccountIndex = Accounts.Count - 1;
			Rms.DeleteStorage("userAo" + account.Server.index);
			Save();
			GameScr.info1.addInfo(Strings.inGameAccountManagerAccountAdded + '!', 0);
		}

		internal static void ResetSelectedAccountIndex()
		{
			SelectedAccountIndex = -1;
			ModStorage.WriteLong(StorageKeySelectedIndex, -1);
		}
	}
}
