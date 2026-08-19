# Game Launcher Pro: Zero-Bloat Windows Game Mode

Game Launcher Pro is an ultra-lightweight, native Windows utility designed to squeeze maximum performance out of your hardware during gaming sessions. 

Instead of relying on heavy third-party optimization software, this project uses native Windows Batch scripts, PowerShell commands, and a custom-built, zero-dependency C# GUI to aggressively free up system resources. It temporarily kills non-essential background applications, suspends Windows Explorer, and disables the internet to ensure 100% of your CPU and RAM are dedicated to your game.

---

## 🗂️ Project Structure

| File | Type | Description |
| :--- | :--- | :--- |
| `Enable.bat` | Batch / PowerShell | The aggressive initiator. Saves running background apps to a log, forcefully closes them, kills Windows Explorer, disables the physical internet adapter, and boots the launcher. |
| `Disable.bat` | Batch / PowerShell | The restoration script. Re-enables the internet adapter, restarts Windows Explorer, reads the log file to relaunch your previous background apps (minimized), and cleans up the log. |we
---

## ✨ Key Features

* **True Zero-Bloat Optimization:** Kills Windows Explorer (`explorer.exe`) and user-level background apps entirely, rather than just lowering their CPU priority.
* **Smart App Restoration:** Logs the exact file paths of closed applications and automatically restores them in a minimized state when you exit Game Mode.
* **0% Idle CPU Usage GUI:** The launcher features a custom asynchronous hardware monitoring thread. When a game is launched and the UI loses focus, the data collector automatically sleeps, dropping the launcher's background resource usage to absolute zero.
* **Low-Level Hardware Polling:** Uses direct P/Invoke calls to the Windows Kernel (`GlobalMemoryStatusEx`) for microsecond-fast RAM usage reading without relying on heavy WMI queries.
* **High-Fidelity Native Rendering:** Utilizes Windows GDI+ with `HighQualityBicubic` interpolation for scaling icons cleanly, and `ClearTypeGridFit` for razor-sharp text rendering without needing modern, heavy UI frameworks like Electron or WPF.

---

## 🛠️ How It Works (Under the Hood)

When you trigger `StartGameMode.bat`, the script requests Administrator privileges and uses an embedded PowerShell command to safely map all running processes (ignoring critical `Session 0` system services to prevent Blue Screens). It logs the executable paths to `log.txt`, terminates them, disables physical network adapters, and launches the custom UI.

The C# GUI reads any game shortcuts (`.lnk` files) placed in a local `Games` folder. It extracts the native icons, scales them smoothly, and displays them in a strict 2D layout. Once you finish gaming, clicking **Disable GameMode** triggers the reverse batch file, bringing Windows back to its normal state seamlessly.

---
## ⚙️ Compilation & Installation

This project requires **zero third-party dependencies** or IDEs (like Visual Studio or MinGW) to compile. It utilizes the C# compiler (`csc.exe`) secretly built into every modern Windows installation.

**To compile the launcher:**
1. Clone or download this repository.
2. Open Command Prompt inside the downloaded folder.
3. Run the following command to natively compile the executable:

`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:GameLauncherPro.exe GameLauncherPro.cs`

*Note: The `/target:winexe` flag ensures the app runs silently as a native Windows GUI without opening a command console in the background.*

---

## 🚀 Usage

1. Create a folder named `Games` in the same directory as the scripts and executable.
2. Place the shortcuts (`.lnk` files) of the games you want to play inside the `Games` folder. 
3. Double-click `StartGameMode.bat`. 
4. Your desktop will disappear, background apps will close, and the Game Launcher Pro UI will appear. 
5. Click any game to play.
6. When finished, click **Disable GameMode** to restore your PC to its standard state.

*(Tip: You can modify your game shortcuts to include custom launch arguments. For example, modifying a GTA V shortcut to target `PlayGTAV.exe -nobattleye -fullscreen` will ensure it boots without anti-cheat and strictly in exclusive fullscreen mode).*
