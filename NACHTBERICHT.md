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
