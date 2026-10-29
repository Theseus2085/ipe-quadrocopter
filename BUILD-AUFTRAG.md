# BUILD-AUFTRAG — Quadrocopter-Steuerungsapplikation (IPE, komplette Software)

> Night build work order. Ground truth hierarchy for EVERY decision:
> 1. `quellen/` (original course files: PDFs, texts, Vorlage_VS.zip, ObjectID xlsx)
> 2. `harte-kriterien.md` (binding compliance rules — a violation = STOP, log, do not build)
> 3. `wissen-programmierung.md` (knowledge base)
> 4. Web search (C#/.NET/WPF reference questions ONLY; never overrides course docs)
> Contradiction between files → `quellen/` wins; log the contradiction in ABWEICHUNGEN.md.

## Ziel (Definition of Done)

1. Complete Visual Studio solution `IPE-Quadrocopter.sln` with:
   - `QuadroControl.Core` (netstandard2.0 class library, NO WPF, NO course DLLs) — buildable + unit-tested ON THIS LINUX VM
   - `QuadroControl.Virtuell` (WPF, .NET Framework 4.5.2, vKTCQuadroControl — Programm 1: virtuelle Testumgebung)
   - `QuadroControl.Real` (WPF, .NET Framework 4.5.2, KTCQuadroControl — Programm 2: realer QC)
   - `QuadroControl.Core.Tests` (xunit, net8.0) — `dotnet test` GREEN
2. `README.md` (DE): build/run instructions for Visual Studio 2019 on Windows, project structure, both programs, controller mapping
3. `DOKUMENTATION/erklaerung.html` (DE, self-contained single file, inline CSS): explains the entire program to Simon — architecture, UAVTalk protocol, communication sequence, GUI, threads, safety features; with diagrams (inline SVG or styled HTML, no external deps)
4. `docs/` (DE): `programmablaufplan.md` (Mermaid flow charts: connection sequence, arming, controller loop, safety), `klassendiagramm.md` (Mermaid class diagram of Core + adapters), `sicherheitsfeatures.md` (each feature: requirement ID from harte-kriterien, implementation, behavior), `controllerbelegung.md`
5. All pushed to GitHub as NEW PRIVATE repo `Theseus2085/ipe-quadrocopter`, verified private
6. `ABWEICHUNGEN.md`: every deviation from this order + justification

## Frozen contract (DO NOT CHANGE)

- Protocol bytes exactly per `wissen-programmierung.md` §5–7: Sync `3C`, MessageTypes, Little Endian everywhere, Length = header+data without checksum. The CRC-8 checksum is appended AUTOMATICALLY by the course DLLs (`SchickeHexString`); the app NEVER computes/appends a checksum itself.
- ManualControlCommand payload field order (9 fields, all always present): Throttle, Roll, Pitch, Yaw, Collective(=0), Thrust(=Throttle), Channel int16[10](=0), Connected(1=connected during flight), FlightModeSwitchPosition(=0)
- Handshake = 5 messages in order: GCSTelemetryStats Status 00 / 01 / 03 (36 data bytes 00 + status byte), then the TWO metadata messages VERBATIM (hex in wissen §7 — copy byte-for-byte, never retype)
- Heartbeat: GCSTelemetryStats Status=03, 1 message per second, own thread
- Arming: ManualControlCommand Throttle=-1 AND Thrust=-1 written MULTIPLE times, then AccessoryDesired AccessoryVal=1. Disarming: AccessoryDesired=-1
- ObjectIDs (without 0x): ManualControlCommand C4 10 74 80, AccessoryDesired C4 09 98 5A, GCSTelemetryStats CA D1 DC 0A
- Library use is MANDATORY (PROG-007): Virtuell uses `vVerbinde/vVerbindungTrennen/vSchickeHexString` (delegate from Vorlage, block marked "NICHT ÄNDERN" copied VERBATIM), Real uses `Verbinde/VerbindungTrennen/SchickeHexString/KonvertiereFloatZuHexString/HoleAktuelleComPorts`
- Controller mapping (decision D3, documented): Mode-2 — left stick Y = Throttle (0..1 from -32768..32767), left stick X = Yaw, right stick X = Roll, right stick Y = Pitch (both scaled to -1..1), button A = Arming, button B = Disarming (per UE4)
- Safety features (PROG-009/009A/010A/011): (a) controller-loss detection via `XBox.IsConnected` polling → auto-Disarming + status change; (b) neutral ManualControlCommand (Throttle=0) sent whenever no controller input; (c) arming state unambiguous in GUI (green/red LED images from Vorlage + label); (d) no motor values before handshake complete (state machine gates it)
- GUI element names from UE4: `TB_COMPort`, `BT_COMPortVerbinde`, `BT_COMPortTrennen`, `BT_HS_HB`, `BT_XBoxRead`, `Label_VerbindungStatus`, `Label_ArmingStatus` (+ may extend: COM-port dropdown via `HoleAktuelleComPorts`, heartbeat indicator)
- DLLs: copy from `quellen/Vorlage_VS.zip` (bin/Debug: KTCQuadroControl.dll, vKTCQuadroControl.dll, CRC.dll, SerialPort.dll, LED_aus.png, LED_grn.png) into each app project's `libs/`, reference via HintPath
- WPF apps target .NET Framework 4.5.2 (template csproj as basis — keep its structure/properties)

## Phases (each ends with a git commit; commit message prefix `[P#]`)

- **P0 Setup** (full model): extract Vorlage_VS.zip to a reference dir; create solution + 4 projects per contract; .gitignore (bin/, obj/, .vs/, TestResults/, *.user); Core builds on Linux (`dotnet build`). COMMIT.
- **P1 Core** (full model): implement UavTalk builder (header+payload hex string, no checksum), FloatHex converter (IEEE 754 little-endian, "XX XX XX XX" format — bit-exact equivalent of KonvertiereFloatZuHexString), payload builders for the 3 objects, HandshakeSequenceProvider, HeartbeatMessage, CommStateMachine (Disconnected→PortOpen→Handshaking→HeartbeatActive→Armed; illegal transitions throw), ControllerMapping, SafetyMonitor logic. xunit tests: known vectors — float 1.0f → `00 00 80 3F` (LE); AccessoryDesired=1.0 message = `3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF` (VL5 example, no checksum byte); MCC payload = 46 bytes (4+4+4+4+4+4 float32 + 20 int16[10] + 1 + 1) → Length field = 56/0x0038 → LE `38 00`; state machine transitions; handshake = exact 5-message sequence; metadata strings byte-identical to wissen §7. `dotnet test` green. COMMIT (source, then tests separately).
- **P2 Apps** (full model): both WPF projects as **SDK-style csproj, TargetFramework net452** (falls Referenzassemblies-Build auf net452 hakt: net48 ok), `UseWPF`, `EnableWindowsTargeting=true`, PackageReference `Microsoft.NETFramework.ReferenceAssemblies` (PrivateAssets all), LangVersion 7.3, references to the course DLLs via HintPath (libs/ copied from Vorlage) — opens in VS2019 16.4+. MainWindow.xaml with PLACEHOLDER grid (UI finalized in P3, full model does NOT design visuals), code-behind complete: transport adapters (IQuadroTransport per app), thread structure (ControllerThread + HeartbeatThread + Dispatcher delegates per VL6 pattern), button handlers, safety logic wiring, config from Core. App.xaml + App.xaml.cs (StartupUri). Vorlage's "NICHT ÄNDERN" delegate block copied VERBATIM into MainWindow.xaml.cs. VERIFY ON LINUX: `/home/openclaw/dotnet-ms/dotnet build` (the MS SDK — /usr/bin/dotnet lacks WindowsDesktop targets) of each WPF project, 0 errors. COMMIT.
- **P3 UI pass** (FLASH model, multimodal): MainWindow.xaml design for BOTH apps using `quellen/pdf-render/UE4_Folie-03.png` + `VL5_Folie-3.png` + `quellen/bildinhalte.md` as visual reference (Zielprogramm layout: top = COM-Port controls, middle = status/LEDs, bottom = controller readout); LED images; consistent margins/fonts; code-behind only minimally adjusted (names from contract). COMMIT.
- **P4 Fix loop** (full model): rerun `dotnet test`, fix failures; `/home/openclaw/dotnet-ms/dotnet build` of the FULL solution (all 4 projects, 0 errors — WPF compile verification IS possible on this VM, prototype-proven 2026-10-28 with the real course DLLs); consistency checklist (every contract element present in both apps). COMMIT only when green.
- **P5 Docs** (flash model — it writes user-facing HTML/UI): README.md, DOKUMENTATION/erklaerung.html (self-contained, professional, DE), docs/*.md per Ziel 3-4 plus `docs/windows-testplan.md` (RUNTIME tests that still need Simon's Windows PC: start app, COM connect, handshake/heartbeat, arming, virtual flight in Quadrocopter.exe, controller behavior, safety features live). Full model may draft docs/klassendiagramm.md content. COMMIT.
- **P6 GitHub** (orchestrator, not a child): repo goes PUBLIC (Simon's explicit order 2026-10-28). Push the FULL milestone history — never squash; verify remote commit count == local count.
  - Repo content rules for a PUBLIC repo: `quellen/` (course PDFs, xlsx, Vorlage zip, PNG renders) and `*.dll` are NOT in the repo (untracked on disk, .gitignore). README documents where to obtain Vorlage_VS.zip (OPAL course).
  - MANDATORY LEAK SCAN before EVERY push (standing rule Simon 2026-10-28): scan `git ls-files` content for (a) exact secret values (tokens, keys, passwords — incl. the GitHub token itself), (b) patterns: api[_-]?key, secret, password, token, BEGIN PRIVATE KEY, AWS/GCP/GitHub token shapes, (c) personal data: TUD login (Kennung), Matrikelnummer, phone numbers, home address, (d) third-party binaries (dll/exe) and course PDFs. Finding -> remove file or strip content, rescan. Zero findings = gate passed.
  - Create PUBLIC repo via curl POST api.github.com/user/repos with token from ~/.hermes/gh-token (env only, never echoed/written): name ipe-quadrocopter, private false; VERIFY response `"private": false`.
  - Push: git remote add origin https://Theseus2085:${TOKEN}@github.com/Theseus2085/ipe-quadrocopter.git ; git push -u origin main ; then git remote set-url origin https://github.com/Theseus2085/ipe-quadrocopter.git (scrub token).
  - Verify: anonymous `git ls-remote` SUCCEEDS (public); remote commit count == local `git rev-list --count main`; `GET /git/trees/main?recursive=1` blob list == local `git ls-files` (count + no dll/pdf/quellen paths). If ANY network/permission block occurs: skip push, log to NACHTBERICHT.md (push deferred to morning), continue with P7.
- **P7 Report**: write `NACHTBERICHT.md` (test counts actually executed, what was built, deviations, open items for Simon's Windows PC) + short Discord summary.

## Hard constraints

- COMPLIANCE GATE: before each phase, check `harte-kriterien.md`. Any FF violation → do not build it, log to ABWEICHUNGEN.md, continue with the rest.
- NEVER touch files outside this repo (no skills, no config, no memory).
- NEVER modify `quellen/`, `harte-kriterien.md`, `wissen-programmierung.md`.
- Web search allowed: ONLY for C#/.NET/WPF/LibrePilot reference (APIs, syntax, patterns). Course requirements always come from the local files.
- Unattended: no interactive approvals. `--dangerously-skip-permissions` is pre-approved for this repo only.
- Write HANDOFF-NIGHT-*.md after P1 and update after every phase.
- Quota: check `~/bin/zai_quota.py` before each child launch; >=80% → no new build; >=95% → stop. No API requests after 00:30 UTC.
- The two WPF apps MUST be compilable in VS2019: SDK-style csproj with `EnableWindowsTargeting=true` + `Microsoft.NETFramework.ReferenceAssemblies` (opens in VS2019 16.4+; verified working on this VM 2026-10-28: WPF net48 + course DLLs build clean with `/home/openclaw/dotnet-ms/dotnet`). NEVER use /usr/bin/dotnet for WPF projects (Ubuntu SDK lacks WindowsDesktop targets).
- Definition of "build green" tonight = `~/dotnet-ms/dotnet build` full solution 0 errors + `dotnet test` 0 failures, both executed by the orchestrator itself, counts recorded in NACHTBERICHT.md.
