# MultiBloxManager

Small Windows utility (C#, .NET 8, WinForms) that lets several Roblox clients run at once by closing
Roblox's single-instance handle through native NT/Win32 calls.

- `Program.cs`: entry. `MainForm.cs` / `MainForm.Designer.cs`: UI. `NativeMethods.cs`: P/Invoke.
- Build: `dotnet build MultiBloxManager.csproj`.
- This repo is public: never commit anything private.

## Keeping this file current
At the end of every chat that changes this project, update this file before finishing:
add decisions, gotchas and current status under "Notes & decisions" (newest first, dated),
fix anything above that is no longer true, then commit and push just this file
(`git add CLAUDE.md && git commit -m "Update CLAUDE.md notes" && git push`).
Never write passwords, tokens, IPs of private servers or other secrets here: this file is on GitHub.

## Notes & decisions
- 2026-11-20: CLAUDE.md added. Note: this repo is PUBLIC.
