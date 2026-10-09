# THAI NGUYEN UNIVERSITY OF INFORMATION AND COMMUNICATION TECHNOLOGY
## INSTITUTE OF INTERNATIONAL TRAINING
### REPORT ON WEB APPLICATION DEVELOPMENT

---

**PROJECT TITLE:**  
# CARO ARENA: REAL-TIME MULTIPLAYER GOMOKU PLATFORM WITH INFINITE CANVAS ENGINE & MINIMAX AI

**Student ID:** `<Student ID>`  
**Student:** `<Student Full Name>`  
**Class:** `<Class Name>`  
**Advisor:** Dr. Vu Duc Quang  

*Thai Nguyen, October 2026*

---

## 1. TOOLS AND TECHNOLOGIES REQUIRED TO INSTALL AND RUN THE APPLICATION

### 1.1. Core Technologies and Development Frameworks
Caro Arena is an enterprise-grade real-time web and desktop gaming platform architected to demonstrate high-concurrency state synchronization, algorithmic game theory, and modern application distribution. The system strictly follows Clean Architecture principles, cleanly decoupling application logic, core business rules, persistence layers, and real-time distributed communication.

* **.NET 9.0 Long-Term Support (LTS):** The foundational runtime and SDK powering all projects. .NET 9 introduces significant performance gains in JIT compilation, high-throughput asynchronous networking, and memory efficiency.
* **ASP.NET Core 9 Minimal APIs:** Constructs the cloud backend micro-endpoints for account authentication, Elo leaderboards, security verification, and player profiles with negligible memory overhead compared to traditional MVC controllers.
* **Microsoft SignalR Core (WebSockets):** The real-time bidirectional communication engine enabling server-authoritative room state replication, turn countdown timers, in-game messaging, and sub-50ms move transmissions.
* **WPF (Windows Presentation Foundation) Client:** Utilizes the Model-View-ViewModel (MVVM) architecture with `CommunityToolkit.Mvvm`, custom Direct `DrawingContext` vector graphics for infinite canvas panning/zooming, and responsive data-binding.
* **Entity Framework Core 9 & SQLite:** Implements a robust dual-database model: a central cloud SQLite database (`caro_server.db`) hosted in persistent cloud storage, complemented by local client SQLite databases for offline replay persistence.
* **Microsoft Azure App Service:** Hosts the production backend server in Japan East on Linux container infrastructure, featuring TLS 1.3 encryption, automatic HTTPS redirection, and WebSocket streaming capabilities.

#### Table 1. Comprehensive technology stack and framework dependencies
| Layer / Subsystem | Framework / Library | Role & Configuration |
| :--- | :--- | :--- |
| **Presentation Layer** | WPF (.NET 9.0) + CommunityToolkit | MVVM Desktop GUI, InfiniteCaroCanvas, Vector Controls |
| **Server Backend** | ASP.NET Core 9 Minimal APIs | SignalR CaroHub, PBKDF2 Hasher, HMAC-SHA256 TokenService |
| **Persistence Layer** | EF Core 9 + Microsoft.Data.Sqlite | Cloud multiplayer DB and local offline replay repositories |
| **Cloud Hosting** | Microsoft Azure App Service (Linux) | Production URL: `https://carogame-d2bafxdybvg3grae.japaneast-01.azurewebsites.net` |
| **Packaging & Install** | Inno Setup 6.4 + Self-Contained | One-click standalone Windows installer `CaroGame_Setup_v1.0.exe` |

### 1.2. Deployment and Setup Procedures
The application provides two deployment mechanisms tailored to developers and non-technical end-users:

1. **Production End-User Installation (Zero-Prerequisite):**  
   Users download and execute `CaroGame_Setup_v1.0.exe` (49.4 MB). The installer includes the full .NET 9 self-contained runtime, gaming fonts (`Cecefontvn`), audio synthesizers, and icons. It installs the platform in under 10 seconds without requiring external dependencies.
2. **Developer CLI Execution:**  
   Developers with the .NET 9 SDK execute:
   ```bash
   dotnet run --project src/CaroGame.Server
   dotnet run --project src/CaroGame.Wpf
   ```
   Automated unit tests are verified via `dotnet test`.

---

## 2. MAIN APPLICATION INTERFACE

### 2.1. Navigation and Main Menu Architecture
Upon launching the client, the application verifies the local authentication token. If authenticated, the user enters `MenuView.xaml`, which acts as the operational hub displaying user credentials, Elo rank badges, matchmaking options, and settings controls.

### 2.2. Battle Arena Interface (GamePlayView)
The in-match battlefield view (`GamePlayView.xaml`) is structured into three coordinated panels:
* **Opponent and Status Card (Left):** Visualizes circular avatars, custom player titles (e.g., Grandmaster), live turn indicators, and synchronized chess countdown clocks with latency compensation.
* **Infinite Canvas Grid (Center):** Hosts `InfiniteCaroCanvas`, which renders Cartesian lines and stone markers using Direct `DrawingContext`. Players pan across infinite space by holding the right mouse button and zoom dynamically with the scroll wheel.
* **Match Drawer (Right):** Contains turn-by-turn move notation history and real-time room chat with localized timestamp bubbles.

---

## 3. DETAILED APPLICATION FUNCTIONALITIES

### 3.1. Authentication and Security Verification
The authentication system (`AuthView.xaml`) provides separate Login and Registration workflows modeled after modern platforms like Chess.com:
* **Real-Time Availability Checking:** Debounced queries immediately alert the user whether a username or email is already taken before submitting the registration form.
* **Masked Input with Eye Toggle:** Custom `PasswordInputControl` masks sensitive input with solid dots while allowing quick visibility toggling via vector eye buttons.
* **Guest Play Mode:** Enables instant local and offline access without requiring an account.

### 3.2. Infinite Coordinate Board Engine & Dual Rule Arbitration
Traditional digital Gomoku games constrain players to a 15x15 or 20x20 matrix. Caro Arena removes this constraint entirely by utilizing a dynamic sparse dictionary (`Dictionary<Coordinate, CellState>`). The board expands indefinitely along positive and negative axes while consuming minimal RAM.

The platform natively supports two official arbitration rule engines (`IRuleEngine`):
* **Vietnamese Rule (Two-End Blocked):** A row of 5 consecutive stones blocked at both ends by opponent stones is not counted as a win. Players must achieve an unblocked five or six consecutive stones to claim victory.
* **Free Gomoku Rule:** Any uninterrupted line of 5 or more consecutive stones immediately claims victory.

```
[ALGORITHM 1: DUAL-RULE VICTORY ARBITRATION ENGINE]
function CheckWin(board, lastCoord, player):
    directions = [ (1,0), (0,1), (1,1), (1,-1) ] // Horizontal, Vertical, Diagonals
    for each (dx, dy) in directions:
        count = 1
        forwardBlocked = false, backwardBlocked = false
        count += ScanLine(board, lastCoord, dx, dy, player, out forwardBlocked)
        count += ScanLine(board, lastCoord, -dx, -dy, player, out backwardBlocked)
        if Rule == Vietnamese:
            if count == 5 and not (forwardBlocked and backwardBlocked): return WIN
            if count >= 6: return WIN
        else if Rule == Free:
            if count >= 5: return WIN
    return ONGOING
```

### 3.3. Real-Time Online Multiplayer & Authoritative Synchronization
Multiplayer games communicate over bidirectional WebSocket connections managed by `CaroHub.cs` on Azure App Service. The architecture enforces server authority to eliminate client-side cheating:
* **Matchmaking Queue:** Automatically pairs players within comparable Elo rating brackets (+/- 150 points).
* **Custom Room System:** Allows users to generate 6-character room codes and optional passwords for private duels.
* **Authoritative Clocks:** Turn timers are decremented on the server; when a timer reaches zero, the server automatically forfeits the timed-out participant.

```
[ALGORITHM 2: AUTHORITATIVE SIGNALR STATE SYNCHRONIZATION]
client.SendMove(x, y):
    await HubConnection.InvokeAsync("SendMove", roomCode, x, y)

server.OnSendMove(roomCode, x, y):
    room = ActiveRooms[roomCode]
    if sender != room.ActivePlayer or not room.Board.IsEmpty(x, y): return Invalid
    room.Board.Set(x, y, room.ActivePlayerRole)
    winner = RuleEngine.CheckWin(room.Board, (x, y))
    if winner != None:
        EloCalculator.UpdateRatings(room.Player1, room.Player2, winner)
        Clients.Group(roomCode).SendAsync("MatchFinished", winnerDetails)
    else:
        room.SwitchTurn()
        Clients.Group(roomCode).SendAsync("MoveReceived", x, y, senderRole)
```

### 3.4. Minimax Artificial Intelligence with Threat Pattern Tables
For offline play, `MinimaxAiEngine.cs` calculates optimal moves across Easy, Medium, and Hard tiers. The AI executes Alpha-Beta pruning over candidate moves generated within the active board bounding box. Candidate moves are evaluated against `ThreatPatternTable.cs`, which assigns weighted heuristic scores to strategic formations (win-in-one, open four, blocked four, open three, split three).

### 3.5. Security Hardening and Cloud Infrastructure
The backend server is hardened against OWASP Top Ten vulnerabilities prior to internet deployment:
* **Transport Encryption:** Enforces TLS 1.3 HTTPS redirection and HTTP Strict Transport Security (HSTS).
* **Multi-Tier Rate Limiting:** Enforces 10 req/min for authentication, 3 req/min for OTP delivery, and 120 req/min globally to prevent brute-force attacks and email spam.
* **Cryptographic Password Storage:** Hashes passwords using PBKDF2 with HMAC-SHA256, 100,000 iterations, and unique 128-bit cryptographic salts.
* **Persistent Cloud Storage:** Configures SQLite to persist on Azure's dedicated `/home/data/caro_server.db` volume, ensuring zero data loss upon container restarts.

#### Table 2. Functional capabilities matrix of Caro Arena platform
| Category | Feature Name | Technical Implementation |
| :--- | :--- | :--- |
| **Authentication** | Real-Time Validation & Eye Toggle | Debounced endpoints, PasswordInputControl, Guest play |
| **Game Modes** | PvP Local, PvAI (3 Tiers), Online Ranked | Minimax Alpha-Beta, SignalR WebSocket Hub, Elo Calculator |
| **Game Rules** | Vietnamese & Free Gomoku Rules | Dynamic IRuleEngine arbitration with win-path highlight |
| **Social & Rank** | Global Leaderboard, Profiles & Friends | Podium 1-2-3 visuals, Title badges, online challenge popups |
| **Review & History** | Step-by-Step Replay & Undo/Redo | Dual stack MoveHistoryManager & timeline slider playback |
| **Security & Cloud** | OWASP Security, Rate Limiting, Cloud | Azure App Service, TLS 1.3, CSP/HSTS, Persistent SQLite |

---

## 4. CONCLUSION AND FUTURE DEVELOPMENT DIRECTIONS

### 4.1. Project Summary and Achievements
Through this project, a complete, highly optimized, and robust multiplayer web application platform has been successfully designed, implemented, and deployed. Key achievements accomplished throughout the development lifecycle include:
* **Modular Architectural Integrity:** Achieved clean separation of concerns across 5 modular projects with 0 circular dependency cycles and 33 passing automated unit tests.
* **Production Cloud Readiness:** Successfully hosted the backend server on Microsoft Azure App Service with public domain availability and TLS 1.3 encryption.
* **Streamlined Distribution:** Engineered a self-contained Inno Setup installer (49.4 MB) enabling instant one-click onboarding for new players without requiring pre-installed runtimes.
* **Enterprise Hardening:** Integrated multi-layer rate limiting, cryptographic session tokens, and persistent SQLite cloud storage.

### 4.2. Future Development Directions
To further expand Caro Arena into an enterprise-scale commercial gaming ecosystem, the following enhancements are planned for future iterations:
* **Cross-Platform Web Client (WebAssembly / Blazor):** Port the presentation tier to Blazor WebAssembly or React to enable native web-browser play alongside the desktop application.
* **Tournament & Bracket Engine:** Implement Swiss-system and Single-Elimination tournament managers with automated scheduling and prize tracking.
* **AI Deep Reinforcement Learning:** Upgrade the Minimax heuristic algorithm with Monte Carlo Tree Search (MCTS) and convolutional neural networks trained via self-play (similar to AlphaZero).
* **Live Spectator & Anti-Cheat System:** Add spectator broadcasting channels with move analysis engines and anomaly detection to identify third-party engine assistance during ranked play.

---

## 5. REFERENCES

* [1] Microsoft Corporation, ".NET 9 Documentation and Architecture Guides," Microsoft Learn, 2024. [Online]. Available: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9
* [2] Microsoft Corporation, "Real-time Web Applications with ASP.NET Core SignalR," Microsoft Learn, 2024. [Online]. Available: https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction
* [3] CommunityToolkit, "MVVM Toolkit Architecture and Source Generators," .NET Foundation, 2024. [Online]. Available: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/
* [4] OWASP Foundation, "OWASP Top 10: 2021 The Definitive Guide to Web Application Security," OWASP, 2021. [Online]. Available: https://owasp.org/Top10/
* [5] A. Elo, "The Rating of Chessplayers, Past and Present," Arco Publishing, New York, 1978.
* [6] S. Russell and P. Norvig, "Artificial Intelligence: A Modern Approach (4th Edition)," Pearson, 2020. [Minimax and Alpha-Beta Search].
* [7] Jordan Russell and Martijn Laan, "Inno Setup Documentation and Scripting Reference," JRSoftware, 2024. [Online]. Available: https://jrsoftware.org/isinfo.php
* [8] Microsoft Azure, "Azure App Service on Linux Architecture and Deployment Documentation," Microsoft, 2024. [Online]. Available: https://learn.microsoft.com/en-us/azure/app-service/
