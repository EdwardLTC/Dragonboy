# AccountManager contributor guide

## Project overview

`AccountManager` is a macOS SwiftUI application that stores Dragonboy account
credentials locally, launches a separate game app, and receives live game data
through a local Socket.IO/WebSocket server.

The Xcode project is `AccountManager.xcodeproj`; the app sources are in
`AccountManager/`.

## Code layout

- `AccountManagerApp.swift` owns the shared `AccountStore` and injects it into
  the root view.
- `Models/` contains the persisted account and character data types.
- `Stores/AccountStore.swift` is the source of truth for accounts. It persists
  `accounts.json` in Application Support and updates observable UI state.
- `Services/` contains game launching, process coordination, and the local
  Socket.IO/WebSocket server.
- `Views/` contains SwiftUI screens and reusable view components.
- `Assets.xcassets/` contains app assets.

## Development conventions

- Keep UI state mutations on the main thread. Service callbacks may arrive on
  background queues; dispatch back to the main queue before changing
  `@Published` properties or other UI-facing state.
- Preserve `Account`'s custom `CodingKeys`: `connectionStatus` is runtime-only
  and must not be persisted.
- Preserve account uniqueness via `Account.uniqueKey` (`username@server`,
  case-insensitive) when adding or importing accounts.
- Keep socket and process lifetime handling in `ProcessManager`/`SocketServer`;
  views should call store or service APIs rather than manage connections.
- Do not log, display, or commit account passwords. Treat persisted account data
  as sensitive.
- The game bundle defaults to `/Applications/Dragonboy250.app`; do not change
  that integration contract without confirming the desired game-side behavior.

## Build and verification

Open `AccountManager.xcodeproj` in Xcode, select the `AccountManager` scheme,
and build with Product > Build. The project currently targets macOS 26.2 and
uses Swift 5.

For command-line builds on a machine with full Xcode selected:

```sh
xcodebuild -project AccountManager.xcodeproj -scheme AccountManager -configuration Debug build
```

This workspace currently has only Command Line Tools selected, so `xcodebuild`
cannot run here until full Xcode is installed and selected with `xcode-select`.

When changing launch or socket behavior, manually verify both failure paths
(missing game bundle and connection loss) as well as a successful game launch.

## Repository hygiene

- Do not commit per-user Xcode workspace state, breakpoints, or IDE metadata.
- Keep changes scoped; do not reformat unrelated Swift files.
- Add tests alongside behavior changes when a test target is introduced. There
  is no test target in the current project.
