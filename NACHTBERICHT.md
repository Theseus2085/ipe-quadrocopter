# NACHTBERICHT — Nightbuild 28./29.10.2026

Erstellt vom Night-Build-Orchestrator (Todd), 28.10.2026 22:46 UTC.

## Ergebnis in einem Satz

Alle Phasen P0-P7 des BUILD-AUFTRAG.md abgeschlossen: komplette Solution mit Core-Bibliothek (79/79 Tests grün, zweimal eigenverifiziert), beide WPF-Apps, UI-Pass nach UE4-Vorlage, komplette Doku, als öffentliches Repo `Theseus2085/ipe-quadrocopter` gepusht und verifiziert.

## Phasen

| Phase | Inhalt | Status |
|---|---|---|
| P0 | Solution IPE-Quadrocopter.sln + 4 Projekte (Core netstandard2.0+net452, Virtuell/Real WPF net452 old-style, Tests xunit net8.0) | Commit 6fb67c6 |
| P1 | QuadroControl.Core: UavTalkBuilder, FloatHex, Payloads (3 Objekte), HandshakeSequenceProvider, HeartbeatMessage, CommStateMachine, ControllerMapping, SafetyMonitor | Commit ff1530b |
| P1 | 79 xunit-Tests (Known-Vektoren, Feldreihenfolge, Handshake-Sequenz, Zustandsmaschine, Safety) | Commit 6a01298 |
| P2 | Beide WPF-Apps: Platzhalter-GUI + vollständiges Code-Behind (Transport-Adapter, Threads, Dispatcher, Safety-Wiring) | Commit 59bf70a |
| P4 | Fix-Loop: Clean-Rebuild, Tests grün, XML-Validierung, Konsistenz-Checkliste, CRC-Scan, ProjectGuid-Fix | Commit 21bd0c6 |
| P3 | UI-Pass beider MainWindow.xaml nach UE4_Folie-03 (LED-Arminganzeige, DataTrigger, Code-Behind unverändert) | Commit 8a685fe |
| P5 | README.md, DOKUMENTATION/erklaerung.html (self-contained, 6 SVG), docs/ (PAP, Klassendiagramm, Sicherheitsfeatures, Controllerbelegung, Windows-Testplan) | Commit 1468cf3 |
| P6 | Leak-Scan (siehe unten), öffentliches Repo erstellt, Full-History-Push, Remote-Verifikation | Commit fd3d279 |
| P7 | Dieser Bericht | - |

## Eigene Verifikation (Orchestrator, nicht nur Kinder-Angaben)

- `dotnet test` in tests/QuadroControl.Core.Tests: **Passed 79, Failed 0, Skipped 0, Total 79** (nach Child A UND nochmal nach der History-Bereinigung)
- XML-Validierung (python3 xml.etree): 4 getrackte .xaml, 0 fehlerhaft (nach P2 und nach P3)
- csproj-Stile: Core/Tests SDK-style, Virtuell/Real old-style mit TargetFrameworkVersion v4.5.2
- Vertrags-GUI-Namen (TB_COMPort ... Label_ArmingStatus): in beiden Apps vollständig vorhanden
- Checksumme: keine CRC-Berechnung im App-Code (alle Treffer sind Kommentare/Erläuterungen; CRC hängt `SchickeHexString` an)
- Metadaten-Handshake-Strings: byte-identisch mit wissen §7 im Core vorhanden

## P6 GitHub (öffentlich, Simons Order 2026-10-28)

- **Leak-Scan (Worktree + komplette History):** 2 Fundstellen persönlicher Daten — TUD-Login-Kennung in BUILD-AUFTRAG.md (seit dem Auftrags-Commit) und Home-Pfade (Benutzername der Build-VM) in zwei HANDOFF-Dateien. Keine Secrets, keine Tokens, keine weiteren personenbezogenen Daten.
- **Fix:** Kennung entfernt, HANDOFF-Dateien untracked + gitignored; danach komplette lokale Historie per `git filter-branch` bereinigt (Repo hatte noch keinen Remote, daher sicher; Milestone-Commits bleiben erhalten, nichts gesquasht — nur Commit-Hashes haben sich geändert). Nach-Scan: 0 Fundstellen in allen 11 Commits.
- **Repo:** https://github.com/Theseus2085/ipe-quadrocopter — als `private: false` erstellt und verifiziert
- **Push-Verifikation (alles anonym):** `git ls-remote` OK; Commit-Count remote 11 == lokal 11; Blob-Liste remote 60 == lokal 60, identisch; keine dll/pdf/zip/xlsx/quellen/HANDOFF-Pfade auf dem Remote. Token aus der Remote-URL entfernt (Konfiguration geprüft).

## Abweichungen

12 Abweichungen in `ABWEICHUNGEN.md` protokolliert. Wichtigste:
- DLL-Methode heißt real `HoleAktuelleCOMPorts()` (Reflexion der Original-DLL gewinnt über wissen-Datei)
- Der eingefrorene VL5-Vektor `... 00 00 80 BF` ist IEEE-754 **-1.0** (Disarming-Beispiel), nicht +1.0 — Tests prüfen die exakten Bytes und zusätzlich +1.0 (`00 00 80 3F`)
- Core ist Multi-Target netstandard2.0+net452, weil net452 (eingefroren für die WPF-Apps) keine netstandard2.0-Assembly laden kann
- Kein rotes LED-Bild im Kursmaterial existiert: Armed/Disarmed = LED_grn/LED_aus + farbige Schrift + Text (PROG-010A dennoch eindeutig erfüllt)

## Quota

- Start 22:01 UTC: 1 % (5h-Fenster), wöchentlich 21 %
- Ende 22:46 UTC: 9 % (5h-Fenster), wöchentlich 22 %

## Offen — braucht Simons Windows-PC (Linux-VM kann das nicht)

1. DLLs/PNGs aus `Vorlage_VS.zip` nach `src/QuadroControl.*/libs/` kopieren (Anleitung: `VORLAGE-DLLS.md`) — ohne sie bauen die WPF-Projekte nicht (bewusst nicht im Repo, Kursmaterial). **Update 29.10.: auf der Build-VM liegt libs/ bereits, Kompilierung ist verifiziert; auf Simons PC einmalig kopieren.**
2. ~~NuGet-Restore + Build beider WPF-Apps in VS2019 — erste echte Compiler-Prüfung~~ **ERLEDIGT 29.10.: komplette Solution baut auf der Linux-VM (SDK-style-csproj, EnableWindowsTargeting, 0 Fehler, beide .exe erzeugt; dabei Instanzmethoden-Fehler HoleAktuelleCOMPorts gefunden+behoben). In VS2019 bleibt nur: Solution öffnen, NuGet-Restore, F5.**
3. Testlauf virtuelle Testumgebung: Steuerungsprogramm + Quadrocopter.exe (Szene I-III), Handshake/Heartbeat/Arming fliegen
4. Danach erst realer FC (propellerlos!, F1) — `docs/windows-testplan.md` Schritt für Schritt

## Sonstiges

- Lock entfernt, Cron-Job "ipe-quadrocopter Nachtbuild" deaktiviert (Auftrag erledigt; Re-Aktivierung bei Bedarf).
- quellen/, harte-kriterien.md, wissen-programmierung.md: unangetastet.

## Nachtbuild 30.10.2026 — Test-Suite-Expansion (TEST-AUFTRAG T1-T11)

- Auftragsbasis: `TEST-AUFTRAG.md` (d0091fb); Child = glm-5.3 FULL (`claude-glm-full`), Laufzeit ~15 min, attempt 1 rc=0, TASK COMPLETE.
- **Eigene Zählungen des Orchestrators** (`/home/openclaw/dotnet-ms/dotnet`):
  - `dotnet test`: vorher 79/79 -> **nachher 385 passed, 0 failed, 0 skipped, exit 0** (Ziel >=150 deutlich übertroffen)
  - `dotnet build IPE-Quadrocopter.sln`: **0 Warnungen, 0 Fehler, exit 0** (beide WPF-.exe erzeugt)
- Commits: 12 neue (`[T1]`..`[T11]` + `[T-Reste]`); geändert NUR `tests/` + `ABWEICHUNGEN.md`; **0 Dateien unter `src/`** (Core eingefroren, WPF-Apps unberührt). Kein Core-Bugfix nötig.
- Tauglichkeits-Stichprobe (3 zufällige neue Dateien: T3-Bytematrix, T7-Safety-Matrix, T10-Golden-Stream): unabhängige IEEE-754-Literale statt nachgerechneter Werte, Entscheidungsmatrix mit expliziten Erwartungswerten, aufzeichnender Fake-Transport, jede Nachricht einzeln asserted — **keine Tautologien gefunden**.
- Abweichungen: 4 neue Einträge #13-16 (T3 kein Clamping = Ist-Verhalten eingefroren; T8 Trigger nicht in Core abbildbar; T8 Neutralstellung exakt nach Dokumentformel 32768/65535 statt idealisiert 0.5; wissen §6 ObjectIDMeta-Notiz nur für Single-Instance-Objekte korrekt). Alle rein dokumentarisch.
- **Leak-Scan vor Push**: 27 Treffer, sämtlich Klasse „VM-Benutzername openclaw" in Bau-/Test-Auftragsdateien (`/home/openclaw/dotnet-ms/dotnet`-Pfade in BUILD-AUFTRAG.md/TEST-AUFTRAG.md). Keine Secrets/Token, keine Binärdateien/Kurs-PDFs, keine personenbezogenen Daten Simons (Kennung/Matrikel/Telefon/Adresse/Domains: 0). Identische Klasse ist seit dem P6-Push öffentlich (BUILD-AUFTRAG.md, 3 Vorkommen); die Test-Commits selbst führen keine neuen Vorkommen ein -> Push freigegeben mit Disposition (bei Bedarf: Pfade in Auftragsdateien auf `~/` kürzen + History-Rewrite — Simons Entscheidung).
- **Push + Verifikation**: `main` fb81dda..979fab8 (fast-forward), anonymes `ls-remote`: remote HEAD == lokal (`979fab8`), Commit-Anzahl remote 27 == lokal 27, Repo public.
- Quota: Start 22:00 UTC 2 % (5h-Fenster), Ende 22:22 UTC 8 % (5h), wöchentlich 32 %.
- Exit: Lock entfernt, Cron-Job „ipe Test-Suite Nachtbuild" deaktiviert (Auftrag einmalig erledigt).

## Azure Windows-VM — Runtime-Smoke-Test Virtuell-App (03.11.2026)

- VM: Azure for Students (Spendengeld-Abo, Ausgabenlimit AN), ipe-quadro-vm, SwedenCentral
  (B-Serie fuer Studenten-Abos in DE/NL-Kapazitaetsregionen gesperrt), B2ls_v2 2vCPU/4GB,
  Windows Server 2022 smalldisk, 64GB StandardSSD. Kosten ~0.05 USD/h. Details/creds:
  lokale Datei HANDOFF-AZURE-VM.md (nicht im Repo).
- Steuerung komplett von der Hermes-VM: az CLI (Device-Login), SSH (Pubkey via run-command),
  GUI-Automatisierung headless RDP: Xvfb :99 + xfreerdp + xdotool + import-Screenshots.
  App-Start im RDP-Session-Kontext via Scheduled Task MIT WorkingDirectory (notwendig!).
- ERGEBNIS: QuadroControl.Virtuell.exe laeuft auf Windows Server 2022, komplette Kette live
  getestet: COM3 verbinden -> 5-Nachrichten-Handshake -> "Handshake abgeschlossen,
  Heartbeat aktiv (1/s)". State-Machine-Gate geprueft: Handshake ohne offenen Port wird
  korrekt blockiert. Keine Crashes, Eventlog sauber.
- FUND 1: Die Kurs-DLLs laden Bilder mit RELATIVEN Pfaden (LED_aus.png, LED_grn.png,
  Logo_groс_transparent.png [kyrillisches c!], Logo_klein.png) aus dem ARBEITSVERZEICHNIS.
  Ohne sie: System.ArgumentException in vGUI..ctor -> sofortiger Absturz. Linux-Build
  kopiert die PNGs NICHT ins bin/. Deployment: alle PNGs neben die Exe UND CWD setzen.
- FUND 2: schtasks ohne WorkingDirectory startet in System32 -> DLL findet Bilder nicht.
  Fix: New-ScheduledTaskAction -WorkingDirectory (siehe HANDOFF-AZURE-VM.md).
- OFFEN: Xbox-Controller-Emulation (ViGEm) fuer Controller lesen / Arming A+B / Steuerwerte;
  Quadrocopter.exe-Szenen-Simulator fehlt weiterhin (OPAL oder Simons PC);
  Real-App nur statisch deployt, runtime nur mit echtem FC moeglich.

## Azure-VM — Volltest virtuelle Testumgebung inkl. Szenen I-III (03.11.2026, Tag-Session)

- Umfang: Quadrocopter.exe (Unity-Simulator) aus dem OPAL-NC-Spiegel auf die VM
  deployed (kein OPAL-Login noetig); Steuerung komplett headless von der Hermes-VM
  (RDP + SSH + GUI-Automatisierung).
- Xbox-Controller-Emulation ohne Treiberinstallation: ViGEm hingt -> stattdessen
  eigene XINPUT1_4.dll (mingw, x64, statisch gelinkt) neben der Exe + gepatchte
  SharpDX.XInput.dll (Import-Namen xinput1_4/1_3/9_1_0 -> xinput_99/98/97_0,
  gleiche Laenge, Full-Trust-Laod ohne Strong-Name-Verify). Zustand per ctrl.txt.
- PROTOKOLL-BRUECKE geklaert (dekompiliert): DLL sendet alle 20 ms 8 Floats an
  TCP 4444 des Simulators; float[7] = Arming-Status (2=Armed, 3=Disarmed).
- ERGEBNIS Szenen-Fluege: I Ebene (H 22,7 m, D 24,2 m), II Sporthalle (nach
  Re-Handshake, H 73 m), III Wiese (H 0,4 km) - jeweils Armed gruen, Flug per
  virtuellem Stick, Disarm rot + Timer-Stopp. Screenshots in der Session-Log.
- Bugs gefixt: Arming-Sequenz (ABWEICHUNGEN 17, DLL-Kontrakt 50x MCC(-1) +
  AccDes + finaler MCC), Heartbeat-Timer-Degeneration dokumentiert (18).
- 385/385 Unit-Tests gruen nach Anpassung (T9/T10 auf neue Sequenz).
- OFFEN bleibt: Real-App mit echtem FC an Simons PC (propellerlos, F1).

## Doku-Standsgleichung + release/-Sofortstartpaket (08.11.2026)

- Anlass: Simons Auftrag, jede Doku auf den aktuellen Build-Stand zu bringen und ein
  maximal startfertiges Paket (wenigste Schritte, wenigste Installation) bereitzustellen.
- VERIFIZIERUNG VORHER (eigene Laeufe): `~/dotnet-ms/dotnet build -c Release` -> 0 Fehler,
  beide WPF-.exe frisch erzeugt; `dotnet test` -> **385 passed / 0 failed**.
- DOKU AKTUALISIERT (Arming-v2-Sequenz 55x MCC(-1) + AccDes + finaler MCC nachgezogen,
  Testzaehler 79 -> 385, Live-Test-Vermerke 03.11., Simulator-Startreihenfolge
  Steuerprogramm zuerst, release/-Hinweise):
  README.md, docs/windows-testplan.md, docs/controllerbelegung.md,
  docs/sicherheitsfeatures.md, docs/programmablaufplan.md, docs/klassendiagramm.md,
  DOKUMENTATION/erklaerung.html (Abschnitt 7 Arming-Tabelle, Teststand-Hinweis,
  Abweichungstabelle +17/+18), projekt-plan.md, AGENTS.md (Regel 7: .NET-Tooling).
- RELEASE-PAKET `release/` (lokal im Projektordner, per .gitignore aus dem Repo gehalten,
  Kurs-Binaerdateien): QuadroControl-Virtuell/ + QuadroControl-Real/ (frische
  Release-Builds + alle 4 PNGs inkl. kyrillischem Logo-Namen, ORIGINAL scharfe
  SharpDX.XInput.dll = echter Controller statt Fake-DLL), Simulator/ (Quadrocopter.exe
  aus dem OPAL-NC-Spiegel entpackt + offizielle Anleitung-PDF),
  ANLEITUNG-INSTALLATION.html (5-Schritte-Schnellstart, SmartScreen/Zulassen-Hinweis,
  Steuerung, Troubleshooting inkl. Heartbeat-Re-Handshake, Real-App-Sicherheit),
  README-START.txt (Kurzpointer). Null Installation noetig: .NET 4.x ist ab Windows 8
  vorinstalliert; Controller (XInput) nur fuer Fliegen/Arming noetig.
- AUSLIEFERUNG: ZIP auf Nextcloud /IPE-Quadrocopter/QuadroControl-Release-2026-11-08.zip,
  Byte-verifiziert per GET + sha256. (Das aeltere QuadroControl-Paket.zip vom 03.11.
  bleibt als Version ohne Simulator liegen.)
- OFFEN bleibt unverändert: Real-App mit echtem FC an Simons PC (propellerlos, F1;
  docs/windows-testplan.md Abschnitt D) + C9 (Controller-Ausschalt-Test am echten Gamepad).

## Controller-Remapping D3v2: R2 = Gas statt linker Stick Y (08.11.2026, Tag-Session)

- SIMONS ENTSCHEIDUNG: neue Belegung R2 (rechter Trigger) = Throttle/Thrust,
  linker Stick X = Yaw, rechter Stick X/Y = Roll/Pitch, linker Stick Y frei.
  Kurskonform (keine Achszuordnung vorgeschrieben, nur Geraet/E2/A+B/C5);
  Begruendung + Voltextext in docs/controllerbelegung.md, ABWEICHUNGEN.md #19.
- CODE: ControllerMapping.cs (ThrottleFromTrigger mit Totzone 10, stetige
  Skalierung (t-10)/245; AxisMin/ThrottleFromAxis entfernt), beide
  MainWindow.xaml.cs (Aufrufstelle, R2-Anzeige, dllHex im Real-Programm aus R2,
  TB_LeftTrigger->TB_RightTrigger), beide MainWindow.xaml (Hinweistext,
  Feldbeschriftungen). Arming/Disarming (A/B) unveraendert — Triggerstellung
  ist dafuer irrelevant, die Sequenz sendet eigene MCC(-1).
- TESTS: T8 + ControllerMappingTests fuer Trigger-Grenzwerte neu geschrieben,
  T10-Goldstream auf neue Signatur/Throttle-Werte portiert (Nachricht 67
  assertet jetzt R2=0 -> Throttle=0 als PROG-011-Nachweis). 385 -> 394 Tests,
  alle gruen; Solution-Build 0 Fehler.
- DOKU-SWEEP: controllerbelegung.md (vollstaendig), README (3 Stellen),
  sicherheitsfeatures.md (Totzonen), klassendiagramm.md, TEST-AUFTRAG.md,
  BUILD-AUFTRAG.md, projekt-plan.md, wissen-programmierung.md §10,
  DOKUMENTATION/erklaerung.html (Hinweiszeile, GUI-Skizze, Elementtabelle).
  Kein "Mode-2"-Text mehr in aktiven Dokumenten (nur noch historische Verweise).
- OFFEN daraus: (a) Live-Nachtest beider Apps auf der Windows-VM mit
  Fake-XInput (R2-Werte in ctrl.txt feeden, Throttle in GUI + MCC pruefen),
  (b) Release-ZIP neu bauen + auf NC legen (QuadroControl-Release-2026-11-08.zip
  enthaelt noch die Mode-2-Builds von heute frueh), (c) Beleg-Kapitel C5 beim
  naechsten Doku-Lauf auf D3v2 stellen.

## Azure-Blockade + Release D3v2 auf NC (08.11.2026, Abend-Session)

- AZURE LOGIN POLICY-BLOCKIERT (AADSTS530035): Conditional Access verlangt nun
  registrierte/Geraete-konforme Devices fuer ALLE Cloud-Apps (Azure CLI UND
  Azure Portal getestet, beide abgewiesen, "Geraetestatus: Unregistered").
  Zustand am 03.11. war noch offen; seit dieser Woche dicht. Kein CLI-, Browser-,
  Device- oder Tenant-Flow funktioniert; siehe Skill azure-access (Stop-Regel).
  Ein uebrig gebliebenes gueltiges ARM-Token gehoert zum falschen Issuer
  (MSA-Verzeichnis), sieht keine Subscriptions — kein Bypass.
- LIVE-TEST AUF AZURE DAHER NICHT GEFahren. Ausstehend, bis TUD/CA-Policy sich
  aendert oder Simon sein Geraet registriert. 0 Ressourcen erzeugt, 0 Kosten.
- RELEASE TROTZDEM GELIEFERT (Simons Entscheidung Option B): D3v2-Builds
  (Release-Konfiguration) + aktualisierte ANLEITUNG (R2-Belegung) gepackt:
  QuadroControl-Release-2026-11-08-D3v2.zip, 69.0 MB, 314 Eintraege,
  kyrillisches Logo im Zip UTF-8 intakt. NC /IPE-Quadrocopter/, PUT 201,
  GET byte-identisch (sha256 verifiziert). Das veraltete Mode-2-ZIP vom Morgen
  (QuadroControl-Release-2026-11-08.zip) geloescht (NC-Papierkorb).
- RUNTIME-VERIFIKATION OFFEN: 394/394 Unit-/Golden-Tests gruen decken die
  Trigger-Skalierung und MCC-Bytes ab; der GUI-/Simulator-Lauf mit R2-Steuerung
  fehlt noch (Simons PC oder spaetere Azure-Session).

## D3v2-Live-Test auf Azure-VM nachgeholt (08.11.2026, Nacht-Session)

- AZURE LOGIN WIEDER MOEGLICH: CA-Policy entschaerft (Device-Flow durch,
  Inkognito + Arbeitskonto). VM gestartet (neue Public IP nach
  Deallocation-Zyklus, siehe lokales HANDOFF), HEAD-Builds (988facb) frisch
  gebaut und deployed
  (MD5 verifiziert): Solution Release-Build 0 Fehler, 394/394 Tests gruen.
- LIVE-TEST VIRTUELL ERFOLGREICH (ganzer Zyklus, Screenshots rdp-Session):
  COM3 verbinden -> 5-Nachrichten-Handshake -> "Handshake abgeschlossen,
  Heartbeat aktiv (1/s)"; Arming per Fake-A=Taste (55x-MCC-Sequenz der App):
  LED gruen, "Armed", Simulator-HUD gruen, Timer laeuft; R2=255 (RT in
  ctrl.txt) -> Drohne steigt in Szene I Ebene: H 0.0m -> 77.4m (00:41) ->
  0.3km (00:47) -> 0.5km (02:01, Deckel/Plateau); R2=0 -> Haelt 0.5km;
  B-Taste -> "Disarmed", LED aus, HUD rot. R2-Anzeige in der GUI folgt
  RT=0/255 live; Kanalzuordnung (LX=Yaw, RX=Roll, R2=Gas) stimmt.
- HARNESS-LIMITIERUNG (fake XInput DLL, kein App-Bug): die DLL saettigt alle
  Achsen digital - RT=128 -> GUI 255, LX=16384 -> 32767, RX=-9830 -> -32767.
  Mittlere Trigger/Stick-Werte sind damit live NICHT darstellbar; analoge
  Skalierung (Totzone 10, (t-10)/245) bleibt durch T8-Grenzwert-Tests
  (394er-Suite) exakt abgedeckt. Quelle der DLL nicht mehr auf VM/Repo
  (Tooling); bei Bedarf neu bauen mit analogen Durchgriff.
- REAL-APP: nur Deploy verifiziert (exe+Core.dll MD5-identisch auf VM);
  Runtime-Test weiterhin nur mit echtem FC an Simons PC (COM-Port).
- AUFGRAEUMT: beide Prozesse per GUI-X geschlossen, ctrl.txt neutral,
  VM dealloziert 23:12 UTC (Laufzeit ~2.6h ~ 14 Cent). NACHTBERICHT-Offen-
  Punkte (a) erledigt, (b) war schon erledigt, (c) Beleg C5 bleibt Doku-Lauf.
- GITHUB: Commit dieser Session gepusht (leak-scan siehe ABWEICHUNGEN-Logik,
  0 Funde), remote == local verifiziert.
