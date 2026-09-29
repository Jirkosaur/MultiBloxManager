# MultiBloxManager

<div align="center">

![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20(x64)-blue?style=for-the-badge&logo=windows)
![.NET Version](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?style=for-the-badge&logo=dotnet)
![License](https://img.shields.io/badge/License-MIT-4EC9B0?style=for-the-badge)
![Architecture](https://img.shields.io/badge/Architecture-Pure%20Native%20Win32-informational?style=for-the-badge)

**Ultra-lightweight, standalone Windows utility for concurrent Roblox client instances powered by pure native NT/Win32 APIs.**

[Key Features](#key-features) • [Installation & Usage](#installation--usage) • [How It Works](#how-it-works-technical-deep-dive) • [Antivirus Transparency](#antivirus--false-positive-transparency) • [Building](#building-from-source) • [Disclaimer](#disclaimer)

</div>

---

## ⚡ Key Features

- **Pure Native NT/Win32 Implementation:** Zero dependency on external tools like Sysinternals `handle64.exe`. All handle queries and closures operate directly through Windows NT syscalls and Win32 P/Invoke.
- **64-Bit Extended Handle Safety:** Employs `SystemExtendedHandleInformation` (class `0x40`) to reliably process full 64-bit Process IDs (PIDs) exceeding `65535`, avoiding the truncation bugs present in legacy 16-bit tools.
- **Deadlock-Free Kernel Querying:** Eliminates known kernel hangs on synchronous pipes or locked files by querying `ObjectTypeInformation` prior to name resolution.
- **Automatic Priority Optimization:** Elevates newly launched client instances to `ProcessPriorityClass.AboveNormal` to mitigate Windows background thread throttling and network packet lag.
- **Minimal Resource Footprint:** Idles at **< 20 MB RAM** with **0.0% standby CPU** utilization.
- **Analytical Dark HUD Interface:** Implements immersive Windows 11/10 DWM dark title bars, obsidian container styling, and live status badges.
- **System Tray Integration:** Minimizes cleanly to the notification area with a contextual quick-action menu (Open, Pause/Resume, GitHub, Exit).
- **Single-Instance Protection:** Prevents duplicate utility instances via a secure system mutex (`MultiBloxManager_SingleInstance_Mutex`).

---

## 🚀 Installation & Usage

### 1. Download
Grab the latest single-file executable `MultiBloxManager.exe` from the [Releases](https://github.com/Jirkosaur/MultiBloxManager/releases) page.

### 2. Run as Administrator
Right-click `MultiBloxManager.exe` and select **Run as administrator** (or approve the automatic UAC prompt). Elevated privileges are strictly required by Windows to inspect cross-process handle tables.

### 3. Launch Clients
Keep MultiBloxManager running in the background or minimized in your System Tray:
1. Launch your first Roblox account normally.
2. MultiBloxManager detects `RobloxPlayerBeta.exe`, waits 2.5 seconds for engine mutex creation, and closes the singleton event.
3. Launch your second (or subsequent) account. Both sessions will now run concurrently without closing each other.

---

## 🔬 How It Works (Technical Deep Dive)

### The Single-Instance Mechanism
By default, the Roblox desktop client (`RobloxPlayerBeta.exe`) ensures only one copy of itself runs at any given time. During startup, the engine creates a named Windows synchronization event:
```text
\BaseNamedObjects\ROBLOX_singletonEvent
```
If a second instance is launched, it checks whether an event named `ROBLOX_singletonEvent` already exists in the session. If detected, the second instance passes focus to the existing window and immediately terminates.

### Native Handle Closure Workflow
MultiBloxManager circumvents this limitation without modifying game files, injecting DLLs, or hooking game memory:

```
[ Roblox Launch ] ──> Creates "\BaseNamedObjects\ROBLOX_singletonEvent"
                             │
                             ▼
[ MultiBloxManager ] ──> Detects RobloxPlayerBeta.exe PID
                             │
                             ▼ (2.5s Delay for Mutex Initialization)
                         NtQuerySystemInformation(SystemExtendedHandleInformation)
                             │
                             ▼
                         Filters handles belonging to target PID
                             │
                             ▼
                         DuplicateHandle(hRoblox, hSource, hManager, ...)
                             │
                             ▼
                         NtQueryObject(ObjectTypeInformation) ──> Verify "Event"
                             │
                             ▼
                         NtQueryObject(ObjectNameInformation) ──> Matches "ROBLOX_singletonEvent"
                             │
                             ▼
                         DuplicateHandle(hRoblox, hSource, NULL, DUPLICATE_CLOSE_SOURCE)
                             │
                             ▼
[ Mutex Released ] ──> Subsequent Roblox accounts launch freely!
```

1. **System Handle Table Scan:** `NtQuerySystemInformation` queries the active handle table. Handles matching the target PID are isolated.
2. **Safe Duplication:** Handles are cloned into the manager process with `DUPLICATE_SAME_ACCESS`.
3. **Deadlock Guard:** Before resolving the object name, the manager checks `ObjectTypeInformation`. Synchronous pipes and files are skipped, preventing kernel deadlocks.
4. **Mutex Closure:** Once `ROBLOX_singletonEvent` is verified, `DuplicateHandle` is called with the flag `DUPLICATE_CLOSE_SOURCE` and `hTargetProcessHandle = IntPtr.Zero`. This signals the Windows kernel to invalidate and destroy the mutex handle inside the Roblox process.
5. **Priority Boost:** The client's process priority is set to `AboveNormal` using `SetPriorityClass` to prevent background stutter.

---

## 🛡️ Antivirus & False Positive Transparency

Because MultiBloxManager operates directly on process handle tables, heuristic scanners may occasionally flag it:

| Question | Detail |
|---|---|
| **Does it inject code or DLLs?** | **NO.** It performs zero memory injection (`WriteProcessMemory`, `CreateRemoteThread`, or DLL injection). |
| **Does it touch game memory?** | **NO.** No game memory, addresses, or assets are read or modified. |
| **Why is Administrator needed?** | Windows NT security forbids unelevated processes from inspecting handle tables of other processes. |
| **Is it open source?** | **YES.** The entire source code is hosted here under the permissive MIT license. You can inspect every line and build it yourself. |

---

## 🛠️ Building from Source

### Prerequisites
- Windows 10 or 11 (64-bit)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build Commands

```powershell
# Clone the repository
git clone https://github.com/Jirkosaur/MultiBloxManager.git
cd MultiBloxManager

# Compile and publish single-file self-contained executable
dotnet publish MultiBloxManager.csproj -c Release -r win-x64 --self-contained true
```

The published standalone binary will be located at:
`bin\Release\net8.0-windows\win-x64\publish\MultiBloxManager.exe`

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

## ⚠️ Disclaimer

MultiBloxManager is an independent open-source utility developed for educational, testing, and multi-account convenience. It is **not** affiliated with, endorsed by, or sponsored by Roblox Corporation. "Roblox" and related marks are registered trademarks of Roblox Corporation. Use responsibly in compliance with all relevant Terms of Service.
