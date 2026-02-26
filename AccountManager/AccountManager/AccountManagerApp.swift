//
//  AccountManagerApp.swift
//  AccountManager
//
//  Created by Thành Công Lê on 26/2/26.
//

import SwiftUI

@main
struct AccountManagerApp: App {
    @StateObject private var store = AccountStore()

    var body: some Scene {
        WindowGroup {
            AccountListView().environmentObject(store)
        }
    }
}
