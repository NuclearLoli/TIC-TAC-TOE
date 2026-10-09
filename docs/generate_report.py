import os
import docx
from docx.shared import Inches, Pt, RGBColor, Cm
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml import OxmlElement, parse_xml
from docx.oxml.ns import nsdecls, qn

def set_cell_border(cell, **kwargs):
    tcPr = cell._tc.get_or_add_tcPr()
    tcBorders = parse_xml(r'<w:tcBorders xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"/>')
    for border_name, border_props in kwargs.items():
        border_el = parse_xml(f'<w:{border_name} xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" '
                              f'w:val="{border_props.get("val", "single")}" '
                              f'w:sz="{border_props.get("sz", "4")}" '
                              f'w:space="{border_props.get("space", "0")}" '
                              f'w:color="{border_props.get("color", "auto")}"/>')
        tcBorders.append(border_el)
    tcPr.append(tcBorders)

def add_callout_box(doc, text, title="PSEUDO-CODE"):
    table = doc.add_table(rows=1, cols=1)
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    
    cell = table.cell(0, 0)
    cell.width = Cm(16.0)
    
    # Shading (light gray background)
    shading = parse_xml(r'<w:shd xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" w:fill="F4F6F9"/>')
    cell._tc.get_or_add_tcPr().append(shading)
    
    # Border
    set_cell_border(cell, 
                    left=dict(val='single', sz=24, color='0055A5'),
                    top=dict(val='single', sz=4, color='D0D7DE'),
                    bottom=dict(val='single', sz=4, color='D0D7DE'),
                    right=dict(val='single', sz=4, color='D0D7DE'))
    
    p = cell.paragraphs[0]
    p.paragraph_format.space_before = Pt(4)
    p.paragraph_format.space_after = Pt(2)
    p.paragraph_format.line_spacing = 1.15
    run_title = p.add_run(f"[{title}]\n")
    run_title.font.name = 'Consolas'
    run_title.font.size = Pt(9.5)
    run_title.font.bold = True
    run_title.font.color.rgb = RGBColor(0, 85, 165)
    
    run_text = p.add_run(text)
    run_text.font.name = 'Consolas'
    run_text.font.size = Pt(9.5)
    run_text.font.color.rgb = RGBColor(30, 30, 30)

def set_page_number_footer(section):
    footer = section.footer
    p = footer.paragraphs[0]
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(6)
    
    run = p.add_run()
    run.font.name = 'Times New Roman'
    run.font.size = Pt(11)
    
    fldSimple = parse_xml(r'<w:fldSimple xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" w:instr="PAGE"/>')
    p._p.append(fldSimple)

def generate_academic_report():
    os.makedirs("docs", exist_ok=True)
    doc = docx.Document()

    # SECTION 1: COVER PAGE
    sec_cover = doc.sections[0]
    sec_cover.top_margin = Cm(2.0)
    sec_cover.bottom_margin = Cm(2.0)
    sec_cover.left_margin = Cm(3.0)
    sec_cover.right_margin = Cm(2.0)
    sec_cover.different_first_page_header_footer = True

    # Outer Border Box for Cover Page (as per standard ICTU template)
    cover_table = doc.add_table(rows=1, cols=1)
    cover_table.alignment = WD_TABLE_ALIGNMENT.CENTER
    cover_table.autofit = False
    c_cell = cover_table.cell(0, 0)
    c_cell.width = Cm(16.0)
    set_cell_border(c_cell,
                    top=dict(val='double', sz=18, color='002060'),
                    bottom=dict(val='double', sz=18, color='002060'),
                    left=dict(val='double', sz=18, color='002060'),
                    right=dict(val='double', sz=18, color='002060'))

    # Inside Cover Page Content
    p_uni = c_cell.paragraphs[0]
    p_uni.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_uni.paragraph_format.space_before = Pt(12)
    p_uni.paragraph_format.space_after = Pt(4)
    r1 = p_uni.add_run("THAI NGUYEN UNIVERSITY OF INFORMATION AND COMMUNICATION TECHNOLOGY\n")
    r1.font.name = "Times New Roman"
    r1.font.size = Pt(12.5)
    r1.font.bold = True
    r1.font.color.rgb = RGBColor(0, 32, 96)

    r2 = p_uni.add_run("INSTITUTE OF INTERNATIONAL TRAINING\n")
    r2.font.name = "Times New Roman"
    r2.font.size = Pt(12)
    r2.font.bold = True
    r2.font.color.rgb = RGBColor(150, 0, 0)

    p_star = c_cell.add_paragraph()
    p_star.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_star.paragraph_format.space_after = Pt(28)
    r_star = p_star.add_run("--------------------***--------------------")
    r_star.font.name = "Times New Roman"
    r_star.font.size = Pt(11)

    p_logo = c_cell.add_paragraph()
    p_logo.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_logo.paragraph_format.space_after = Pt(36)
    r_logo = p_logo.add_run("< UNIVERSITY LOGO >\n")
    r_logo.font.name = "Times New Roman"
    r_logo.font.size = Pt(13)
    r_logo.font.bold = True
    r_logo.font.color.rgb = RGBColor(120, 120, 120)

    p_subject = c_cell.add_paragraph()
    p_subject.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_subject.paragraph_format.space_after = Pt(16)
    r_sub = p_subject.add_run("REPORT ON WEB APPLICATION DEVELOPMENT\n")
    r_sub.font.name = "Times New Roman"
    r_sub.font.size = Pt(16)
    r_sub.font.bold = True
    r_sub.font.color.rgb = RGBColor(0, 51, 102)

    p_topic = c_cell.add_paragraph()
    p_topic.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_topic.paragraph_format.space_after = Pt(48)
    r_tp_lbl = p_topic.add_run("PROJECT TITLE:\n")
    r_tp_lbl.font.name = "Times New Roman"
    r_tp_lbl.font.size = Pt(13)
    r_tp_lbl.font.bold = True
    r_proj = p_topic.add_run("CARO ARENA: REAL-TIME MULTIPLAYER GOMOKU PLATFORM\nWITH INFINITE CANVAS ENGINE & MINIMAX AI")
    r_proj.font.name = "Times New Roman"
    r_proj.font.size = Pt(15.5)
    r_proj.font.bold = True
    r_proj.font.color.rgb = RGBColor(180, 0, 0)

    p_info = c_cell.add_paragraph()
    p_info.alignment = WD_ALIGN_PARAGRAPH.LEFT
    p_info.paragraph_format.left_indent = Cm(3.2)
    p_info.paragraph_format.space_after = Pt(54)
    p_info.paragraph_format.line_spacing = 1.3

    meta_items = [
        ("Student ID: ", True), ("<Student ID>\n", False),
        ("Student:    ", True), ("<Student Full Name>\n", False),
        ("Class:      ", True), ("<Class Name>\n", False),
        ("Advisor:    ", True), ("Dr. Vu Duc Quang\n", True),
    ]
    for lbl, is_bold in meta_items:
        r = p_info.add_run(lbl)
        r.font.name = "Times New Roman"
        r.font.size = Pt(13)
        r.font.bold = is_bold

    p_foot = c_cell.add_paragraph()
    p_foot.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p_foot.paragraph_format.space_after = Pt(12)
    r_ft = p_foot.add_run("Thai Nguyen, October 2026")
    r_ft.font.name = "Times New Roman"
    r_ft.font.size = Pt(12)
    r_ft.font.italic = True

    # PAGE BREAK TO REPORT BODY
    doc.add_page_break()
    set_page_number_footer(sec_cover)

    # HELPER FORMATTING FUNCTIONS
    def add_h1(text):
        p = doc.add_paragraph()
        p.paragraph_format.space_before = Pt(14)
        p.paragraph_format.space_after = Pt(6)
        p.paragraph_format.keep_with_next = True
        r = p.add_run(text)
        r.font.name = "Times New Roman"
        r.font.size = Pt(14.5)
        r.font.bold = True
        r.font.color.rgb = RGBColor(0, 51, 102)
        return p

    def add_h2(text):
        p = doc.add_paragraph()
        p.paragraph_format.space_before = Pt(10)
        p.paragraph_format.space_after = Pt(4)
        p.paragraph_format.keep_with_next = True
        r = p.add_run(text)
        r.font.name = "Times New Roman"
        r.font.size = Pt(13.5)
        r.font.bold = True
        r.font.color.rgb = RGBColor(30, 30, 30)
        return p

    def add_p(text):
        p = doc.add_paragraph()
        p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
        p.paragraph_format.space_before = Pt(0)
        p.paragraph_format.space_after = Pt(5)
        p.paragraph_format.line_spacing = 1.2
        r = p.add_run(text)
        r.font.name = "Times New Roman"
        r.font.size = Pt(13)
        return p

    def add_bullet(bold_prefix, text):
        p = doc.add_paragraph(style='List Bullet')
        p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
        p.paragraph_format.space_before = Pt(0)
        p.paragraph_format.space_after = Pt(3)
        p.paragraph_format.line_spacing = 1.15
        r_bold = p.add_run(bold_prefix)
        r_bold.font.name = "Times New Roman"
        r_bold.font.size = Pt(13)
        r_bold.font.bold = True
        r = p.add_run(text)
        r.font.name = "Times New Roman"
        r.font.size = Pt(13)
        return p

    def add_caption(text):
        p = doc.add_paragraph()
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p.paragraph_format.space_before = Pt(2)
        p.paragraph_format.space_after = Pt(8)
        r = p.add_run(text)
        r.font.name = "Times New Roman"
        r.font.size = Pt(11)
        r.font.italic = True
        r.font.color.rgb = RGBColor(80, 80, 80)
        return p

    def add_image_if_exists(img_path, width_in_cm, caption_text):
        if os.path.exists(img_path):
            p = doc.add_paragraph()
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER
            p.paragraph_format.space_before = Pt(6)
            p.paragraph_format.space_after = Pt(2)
            run = p.add_run()
            run.add_picture(img_path, width=Cm(width_in_cm))
            add_caption(caption_text)

    # -------------------------------------------------------------
    # SECTION 1: TOOLS AND PLATFORMS
    # -------------------------------------------------------------
    add_h1("1. TOOLS AND TECHNOLOGIES REQUIRED TO INSTALL AND RUN THE APPLICATION")
    add_p(
        "Caro Arena is an enterprise-grade real-time web and desktop gaming platform architected to demonstrate "
        "high-concurrency state synchronization, algorithmic game theory, and modern application distribution. "
        "The system strictly follows Clean Architecture principles, cleanly decoupling application logic, core business "
        "rules, persistence layers, and real-time distributed communication."
    )

    add_h2("1.1. Core Technologies and Development Frameworks")
    add_bullet(".NET 9.0 Long-Term Support (LTS): ", "The foundational runtime and SDK powering all projects. .NET 9 introduces significant performance gains in JIT compilation, high-throughput asynchronous networking, and memory efficiency.")
    add_bullet("ASP.NET Core 9 Minimal APIs: ", "Constructs the cloud backend micro-endpoints for account authentication, Elo leaderboards, security verification, and player profiles with negligible memory overhead compared to traditional MVC controllers.")
    add_bullet("Microsoft SignalR Core (WebSockets): ", "The real-time bidirectional communication engine enabling server-authoritative room state replication, turn countdown timers, in-game messaging, and sub-50ms move transmissions.")
    add_bullet("WPF (Windows Presentation Foundation) Client: ", "Utilizes the Model-View-ViewModel (MVVM) architecture with CommunityToolkit.Mvvm, custom Direct DrawingContext vector graphics for infinite canvas panning/zooming, and responsive data-binding.")
    add_bullet("Entity Framework Core 9 & SQLite: ", "Implements a robust dual-database model: a central cloud SQLite database (caro_server.db) hosted in persistent cloud storage, complemented by local client SQLite databases for offline replay persistence.")
    add_bullet("Microsoft Azure App Service: ", "Hosts the production backend server in Japan East on Linux container infrastructure, featuring TLS 1.3 encryption, automatic HTTPS redirection, and WebSocket streaming capabilities.")

    # Table 1: Technical Stack Summary
    t1 = doc.add_table(rows=6, cols=3)
    t1.alignment = WD_TABLE_ALIGNMENT.CENTER
    t1_headers = ["Layer / Subsystem", "Framework / Library", "Role & Configuration"]
    for i, h in enumerate(t1_headers):
        cell = t1.cell(0, i)
        cell.paragraphs[0].text = h
        cell.paragraphs[0].runs[0].font.name = "Times New Roman"
        cell.paragraphs[0].runs[0].font.size = Pt(12)
        cell.paragraphs[0].runs[0].font.bold = True
        cell.paragraphs[0].alignment = WD_ALIGN_PARAGRAPH.CENTER
        shd = parse_xml(r'<w:shd xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" w:fill="E8EEF5"/>')
        cell._tc.get_or_add_tcPr().append(shd)

    t1_data = [
        ("Presentation Layer", "WPF (.NET 9.0) + CommunityToolkit", "MVVM Desktop GUI, InfiniteCaroCanvas, Vector Controls"),
        ("Server Backend", "ASP.NET Core 9 Minimal APIs", "SignalR CaroHub, PBKDF2 Hasher, HMAC-SHA256 TokenService"),
        ("Persistence Layer", "EF Core 9 + Microsoft.Data.Sqlite", "Cloud multiplayer DB and local offline replay repositories"),
        ("Cloud Hosting", "Microsoft Azure App Service (Linux)", "Public URL: https://carogame-d2bafxdybvg3grae...azurewebsites.net"),
        ("Packaging & Install", "Inno Setup 6.4 + Self-Contained", "One-click standalone Windows installer CaroGame_Setup_v1.0.exe"),
    ]
    for r_idx, row in enumerate(t1_data, start=1):
        for c_idx, val in enumerate(row):
            c = t1.cell(r_idx, c_idx)
            c.paragraphs[0].text = val
            c.paragraphs[0].runs[0].font.name = "Times New Roman"
            c.paragraphs[0].runs[0].font.size = Pt(11.5)
            if c_idx == 0:
                c.paragraphs[0].runs[0].font.bold = True

    add_caption("Table 1. Comprehensive technology stack and framework dependencies")

    add_h2("1.2. Deployment and Setup Procedures")
    add_p(
        "The application provides two deployment mechanisms tailored to developers and non-technical end-users:"
    )
    add_bullet("1. Production End-User Installation (Zero-Prerequisite): ", 
               "Users download and execute 'CaroGame_Setup_v1.0.exe' (49.4 MB). The installer includes the full .NET 9 self-contained runtime, gaming fonts (Cecefontvn), audio synthesizers, and icons. It installs the platform in under 10 seconds without requiring external dependencies.")
    add_bullet("2. Developer CLI Execution: ", 
               "Developers with the .NET 9 SDK execute 'dotnet run --project src/CaroGame.Server' followed by 'dotnet run --project src/CaroGame.Wpf'. Automated tests are executed via 'dotnet test'.")

    add_image_if_exists("docs/images/vs_publish.png", 14.5, "Figure 1. Successful production release build and cloud deployment verification")

    # -------------------------------------------------------------
    # SECTION 2: MAIN USER INTERFACE
    # -------------------------------------------------------------
    add_h1("2. MAIN APPLICATION INTERFACE")
    add_p(
        "Caro Arena features a modern Cyber Dark user interface built entirely upon a Single-Window Shell model (MainWindow.xaml). "
        "The interface eliminates disruptive window popups by dynamically swapping views within a central ContentControl container "
        "orchestrated by NavigationService and Dependency Injection."
    )

    add_h2("2.1. Navigation and Main Menu Architecture")
    add_p(
        "Upon launching the client, the application verifies the local authentication token. If authenticated, the user enters "
        "MenuView.xaml, which acts as the operational hub displaying user credentials, Elo rank badges, matchmaking options, "
        "and settings controls."
    )

    add_image_if_exists("docs/images/ui_menu.png", 13.5, "Figure 2. Primary Navigation Hub and User Profile Card (MenuView)")

    add_h2("2.2. Battle Arena Interface (GamePlayView)")
    add_p(
        "The in-match battlefield view (GamePlayView.xaml) is structured into three coordinated panels:"
    )
    add_bullet("Opponent and Status Card (Left): ", 
               "Visualizes circular avatars, custom player titles (e.g., Grandmaster), live turn indicators, and synchronized chess countdown clocks with latency compensation.")
    add_bullet("Infinite Canvas Grid (Center): ", 
               "Hosts InfiniteCaroCanvas, which renders Cartesian lines and stone markers using Direct DrawingContext. Players pan across infinite space by holding the right mouse button and zoom dynamically with the scroll wheel.")
    add_bullet("Match Drawer (Right): ", 
               "Contains turn-by-turn move notation history and real-time room chat with localized timestamp bubbles.")

    add_image_if_exists("docs/images/ui_gameplay.png", 14.5, "Figure 3. Interactive Battle Arena featuring Infinite Canvas and Live Match Drawer (GamePlayView)")

    # -------------------------------------------------------------
    # SECTION 3: DETAILED APPLICATION FUNCTIONALITIES
    # -------------------------------------------------------------
    add_h1("3. DETAILED APPLICATION FUNCTIONALITIES")

    add_h2("3.1. Authentication and Security Verification")
    add_p(
        "The authentication system (AuthView.xaml) provides separate Login and Registration workflows modeled after modern platforms like Chess.com:"
    )
    add_bullet("Real-Time Availability Checking: ", "Debounced queries immediately alert the user whether a username or email is already taken before submitting the registration form.")
    add_bullet("Masked Input with Eye Toggle: ", "Custom PasswordInputControl masks sensitive input with solid dots while allowing quick visibility toggling via vector eye buttons.")
    add_bullet("Guest Play Mode: ", "Enables instant local and offline access without requiring an account.")

    add_image_if_exists("docs/images/ui_auth.png", 13.5, "Figure 4. Authentication view with real-time validation and masked password controls (AuthView)")

    add_h2("3.2. Infinite Coordinate Board Engine & Dual Rule Arbitration")
    add_p(
        "Traditional digital Gomoku games constrain players to a 15x15 or 20x20 matrix. Caro Arena removes this constraint "
        "entirely by utilizing a dynamic sparse dictionary (Dictionary<Coordinate, CellState>). The board expands indefinitely "
        "along positive and negative axes while consuming minimal RAM."
    )
    add_p(
        "The platform natively supports two official arbitration rule engines (IRuleEngine):"
    )
    add_bullet("Vietnamese Rule (Two-End Blocked): ", "A row of 5 consecutive stones blocked at both ends by opponent stones is not counted as a win. Players must achieve an unblocked five or six consecutive stones to claim victory.")
    add_bullet("Free Gomoku Rule: ", "Any uninterrupted line of 5 or more consecutive stones immediately claims victory.")

    add_callout_box(
        doc,
        "function CheckWin(board, lastCoord, player):\n"
        "    directions = [ (1,0), (0,1), (1,1), (1,-1) ] // Horizontal, Vertical, Diagonals\n"
        "    for each (dx, dy) in directions:\n"
        "        count = 1\n"
        "        forwardBlocked = false, backwardBlocked = false\n"
        "        count += ScanLine(board, lastCoord, dx, dy, player, out forwardBlocked)\n"
        "        count += ScanLine(board, lastCoord, -dx, -dy, player, out backwardBlocked)\n"
        "        if Rule == Vietnamese:\n"
        "            if count == 5 and not (forwardBlocked and backwardBlocked): return WIN\n"
        "            if count >= 6: return WIN\n"
        "        else if Rule == Free:\n"
        "            if count >= 5: return WIN\n"
        "    return ONGOING",
        "ALGORITHM 1: DUAL-RULE VICTORY ARBITRATION ENGINE"
    )

    add_h2("3.3. Real-Time Online Multiplayer & Authoritative Synchronization")
    add_p(
        "Multiplayer games communicate over bidirectional WebSocket connections managed by CaroHub.cs on Azure App Service. "
        "The architecture enforces server authority to eliminate client-side cheating:"
    )
    add_bullet("Matchmaking Queue: ", "Automatically pairs players within comparable Elo rating brackets (+/- 150 points).")
    add_bullet("Custom Room System: ", "Allows users to generate 6-character room codes and optional passwords for private duels.")
    add_bullet("Authoritative Clocks: ", "Turn timers are decremented on the server; when a timer reaches zero, the server automatically forfeits the timed-out participant.")

    add_callout_box(
        doc,
        "client.SendMove(x, y):\n"
        "    await HubConnection.InvokeAsync(\"SendMove\", roomCode, x, y)\n\n"
        "server.OnSendMove(roomCode, x, y):\n"
        "    room = ActiveRooms[roomCode]\n"
        "    if sender != room.ActivePlayer or not room.Board.IsEmpty(x, y): return Invalid\n"
        "    room.Board.Set(x, y, room.ActivePlayerRole)\n"
        "    winner = RuleEngine.CheckWin(room.Board, (x, y))\n"
        "    if winner != None:\n"
        "        EloCalculator.UpdateRatings(room.Player1, room.Player2, winner)\n"
        "        Clients.Group(roomCode).SendAsync(\"MatchFinished\", winnerDetails)\n"
        "    else:\n"
        "        room.SwitchTurn()\n"
        "        Clients.Group(roomCode).SendAsync(\"MoveReceived\", x, y, senderRole)",
        "ALGORITHM 2: AUTHORITATIVE SIGNALR STATE SYNCHRONIZATION"
    )

    add_h2("3.4. Minimax Artificial Intelligence with Threat Pattern Tables")
    add_p(
        "For offline play, MinimaxAiEngine.cs calculates optimal moves across Easy, Medium, and Hard tiers. "
        "The AI executes Alpha-Beta pruning over candidate moves generated within the active board bounding box. "
        "Candidate moves are evaluated against ThreatPatternTable.cs, which assigns weighted heuristic scores to strategic formations "
        "(win-in-one, open four, blocked four, open three, split three)."
    )

    add_h2("3.5. Security Hardening and Cloud Infrastructure")
    add_p(
        "The backend server is hardened against OWASP Top Ten vulnerabilities prior to internet deployment:"
    )
    add_bullet("Transport Encryption: ", "Enforces TLS 1.3 HTTPS redirection and HTTP Strict Transport Security (HSTS).")
    add_bullet("Multi-Tier Rate Limiting: ", "Enforces 10 req/min for authentication, 3 req/min for OTP delivery, and 120 req/min globally to prevent brute-force attacks and email spam.")
    add_bullet("Cryptographic Password Storage: ", "Hashes passwords using PBKDF2 with HMAC-SHA256, 100,000 iterations, and unique 128-bit cryptographic salts.")
    add_bullet("Persistent Cloud Storage: ", "Configures SQLite to persist on Azure's dedicated '/home/data/caro_server.db' volume, ensuring zero data loss upon container restarts.")

    add_image_if_exists("docs/images/azure_deployed.png", 14.5, "Figure 5. Cloud deployment verification running on Microsoft Azure App Service")

    # Table 2: Feature Matrix
    t2 = doc.add_table(rows=7, cols=3)
    t2.alignment = WD_TABLE_ALIGNMENT.CENTER
    t2_headers = ["Category", "Feature Name", "Technical Implementation"]
    for i, h in enumerate(t2_headers):
        cell = t2.cell(0, i)
        cell.paragraphs[0].text = h
        cell.paragraphs[0].runs[0].font.name = "Times New Roman"
        cell.paragraphs[0].runs[0].font.size = Pt(12)
        cell.paragraphs[0].runs[0].font.bold = True
        cell.paragraphs[0].alignment = WD_ALIGN_PARAGRAPH.CENTER
        shd = parse_xml(r'<w:shd xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" w:fill="E8EEF5"/>')
        cell._tc.get_or_add_tcPr().append(shd)

    t2_data = [
        ("Authentication", "Real-Time Validation & Eye Toggle", "Debounced endpoints, PasswordInputControl, Guest play"),
        ("Game Modes", "PvP Local, PvAI (3 Tiers), Online Ranked", "Minimax Alpha-Beta, SignalR WebSocket Hub, Elo Calculator"),
        ("Game Rules", "Vietnamese & Free Gomoku Rules", "Dynamic IRuleEngine arbitration with win-path highlight"),
        ("Social & Rank", "Global Leaderboard, Profiles & Friends", "Podium 1-2-3 visuals, Title badges, online challenge popups"),
        ("Review & History", "Step-by-Step Replay & Undo/Redo", "Dual stack MoveHistoryManager & timeline slider playback"),
        ("Security & Cloud", "OWASP Security, Rate Limiting, Cloud", "Azure App Service, TLS 1.3, CSP/HSTS, Persistent SQLite"),
    ]
    for r_idx, row in enumerate(t2_data, start=1):
        for c_idx, val in enumerate(row):
            c = t2.cell(r_idx, c_idx)
            c.paragraphs[0].text = val
            c.paragraphs[0].runs[0].font.name = "Times New Roman"
            c.paragraphs[0].runs[0].font.size = Pt(11.5)
            if c_idx == 0:
                c.paragraphs[0].runs[0].font.bold = True

    add_caption("Table 2. Functional capabilities matrix of Caro Arena platform")

    # -------------------------------------------------------------
    # SECTION 4: CONCLUSION AND FUTURE DIRECTIONS
    # -------------------------------------------------------------
    add_h1("4. CONCLUSION AND FUTURE DEVELOPMENT DIRECTIONS")

    add_h2("4.1. Project Summary and Achievements")
    add_p(
        "Through this project, a complete, highly optimized, and robust multiplayer web application platform has been successfully designed, "
        "implemented, and deployed. Key achievements accomplished throughout the development lifecycle include:"
    )
    add_bullet("Modular Architectural Integrity: ", "Achieved clean separation of concerns across 5 modular projects with 0 circular dependency cycles and 33 passing automated unit tests.")
    add_bullet("Production Cloud Readiness: ", "Successfully hosted the backend server on Microsoft Azure App Service with public domain availability and TLS 1.3 encryption.")
    add_bullet("Streamlined Distribution: ", "Engineered a self-contained Inno Setup installer (49.4 MB) enabling instant one-click onboarding for new players without requiring pre-installed runtimes.")
    add_bullet("Enterprise Hardening: ", "Integrated multi-layer rate limiting, cryptographic session tokens, and persistent SQLite cloud storage.")

    add_h2("4.2. Future Development Directions")
    add_p(
        "To further expand Caro Arena into an enterprise-scale commercial gaming ecosystem, the following enhancements are planned for future iterations:"
    )
    add_bullet("Cross-Platform Web Client (WebAssembly / Blazor): ", "Port the presentation tier to Blazor WebAssembly or React to enable native web-browser play alongside the desktop application.")
    add_bullet("Tournament & Bracket Engine: ", "Implement Swiss-system and Single-Elimination tournament managers with automated scheduling and prize tracking.")
    add_bullet("AI Deep Reinforcement Learning: ", "Upgrade the Minimax heuristic algorithm with Monte Carlo Tree Search (MCTS) and convolutional neural networks trained via self-play (similar to AlphaZero).")
    add_bullet("Live Spectator & Anti-Cheat System: ", "Add spectator broadcasting channels with move analysis engines and anomaly detection to identify third-party engine assistance during ranked play.")

    # -------------------------------------------------------------
    # SECTION 5: REFERENCES
    # -------------------------------------------------------------
    add_h1("5. REFERENCES")
    refs = [
        ("[1] Microsoft Corporation, \".NET 9 Documentation and Architecture Guides,\" Microsoft Learn, 2024. [Online]. Available: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9",),
        ("[2] Microsoft Corporation, \"Real-time Web Applications with ASP.NET Core SignalR,\" Microsoft Learn, 2024. [Online]. Available: https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction",),
        ("[3] CommunityToolkit, \"MVVM Toolkit Architecture and Source Generators,\" .NET Foundation, 2024. [Online]. Available: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/",),
        ("[4] OWASP Foundation, \"OWASP Top 10: 2021 The Definitive Guide to Web Application Security,\" OWASP, 2021. [Online]. Available: https://owasp.org/Top10/",),
        ("[5] A. Elo, \"The Rating of Chessplayers, Past and Present,\" Arco Publishing, New York, 1978.",),
        ("[6] S. Russell and P. Norvig, \"Artificial Intelligence: A Modern Approach (4th Edition),\" Pearson, 2020. [Minimax and Alpha-Beta Search].",),
        ("[7] Jordan Russell and Martijn Laan, \"Inno Setup Documentation and Scripting Reference,\" JRSoftware, 2024. [Online]. Available: https://jrsoftware.org/isinfo.php",),
        ("[8] Microsoft Azure, \"Azure App Service on Linux Architecture and Deployment Documentation,\" Microsoft, 2024. [Online]. Available: https://learn.microsoft.com/en-us/azure/app-service/",),
    ]
    for r in refs:
        p = doc.add_paragraph()
        p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
        p.paragraph_format.space_before = Pt(0)
        p.paragraph_format.space_after = Pt(4)
        p.paragraph_format.left_indent = Cm(0.8)
        p.paragraph_format.first_line_indent = Cm(-0.8)
        run = p.add_run(r[0])
        run.font.name = "Times New Roman"
        run.font.size = Pt(12)

    output_path = os.path.abspath("docs/Caro_Game_Final_Report.docx")
    doc.save(output_path)
    print(f"Academic report generated successfully at: {output_path}")

if __name__ == "__main__":
    generate_academic_report()
