# TEST-AUFTRAG — Erweiterung der Test-Suite (Nachtbuild 29.10.2026)

> Work order for the comprehensive test-suite expansion. Model: glm-5.3 FULL (Simon's pick 2026-10-29).
> Ground truth: `harte-kriterien.md` (E1-E6 = Protokoll-Regeln), `wissen-programmierung.md` §5-§8, frozen contract in `BUILD-AUFTRAG.md`.
> Repo state at start: fb81dda, solution builds 0 errors on this VM, 79/79 tests green.

## Ziel (Definition of Done)

1. `tests/QuadroControl.Core.Tests/` erweitert auf **mindestens 150 Tests**, alle gruen
   (`~/dotnet-ms/dotnet test`, exit 0)
2. Komplette Solution baut weiterhin mit 0 Fehlern (`~/dotnet-ms/dotnet build IPE-Quadrocopter.sln`)
3. Neue Tests sind aussagekraeftig: keine Tautologien, jeder Test prueft eine konkrete Vertrags- oder Grenzeigenschaft
4. `src/QuadroControl.Core` bleibt UNVERAENDERT, ausser ein Test legt einen echten Bug offen: dann Fix + neuer Regressionstest + Eintrag in ABWEICHUNGEN.md
5. WPF-App-Projekte (src/QuadroControl.Virtuell, src/QuadroControl.Real): NICHT anfassen (nur bauen als Gate)
6. Milestone-Commits: pro Testbereich ein Commit ("[T1] FloatHex/Hex-Kanten", "[T2] Statemachine-Matrix", ...), am Ende ein Gesamt-Commit falls Reste

## Testbereiche (alle gegen die 16 oeffentlichen Typen in src/QuadroControl.Core)

- **T1 FloatHex + Hex (Kanten):** NaN, +|-Unendlich, +|-0, denormale Werte, float.MinValue/MaxValue; Roundtrip-Eigenschaft mit DETERMINISTISCHEM Seed (mind. 1000 Zufallsfloats: hex -> zurueckparsen -> bitgleich); Hex-Format-Validierung: Gross-/Kleinschreibung, falsche Leerzeichen/Anzahl, leere Strings, ungerade Hexziffern, Nicht-Hex-Zeichen -> definiertes Verhalten (Exception-Typ dokumentieren)
- **T2 UavTalkBuilder + ObjectIds:** Sync-Byte 3C, alle MessageTypes, Length-Feld (int16, LE, Header+Daten ohne Checksumme) fuer ALLE drei Payloads, ObjectIDs byte-exakt (MCC C4 10 74 80 -> Nachricht 80 74 10 C4 usw.), InstanceID 00 00, Checksumme wird NICHT angebaut (DLL-Pflicht, E1)
- **T3 ManualControlCommandData:** Bytematrix - jede Byte-Position des 46-Byte-Payloads einzeln asserting; Feldreihenfolge Throttle,Roll,Pitch,Yaw,Collective=0,Thrust=Throttle-Spiegel,Channel[10]=0,Connected,FlightModeSwitchPosition=0; Wertegrenzen -1/0/1; Verhalten ausserhalb des Wertebereichs (clamp oder Exception - dokumentieren was der Code TUT)
- **T4 AccessoryDesired + GCSTelemetryStats:** Arming 1 / Disarming -1 Bytes; GCS-Status-Reihenfolge 00->01->03; 36 Nullbytes + Statusbyte an letzter Position
- **T5 HandshakeSequenceProvider:** exakt 5 Nachrichten in fester Reihenfolge; die 2 Metadaten-Nachrichten byte-identisch mit wissen §7 (wortwurtzlich); keine CRC-Anhaengung
- **T6 CommStateMachine VOLLE Transitionsmatrix:** jeder Zustand x jedes Ereignis, legal und illegal (illegal = Exception/Reject, dokumentieren); vollstaendiger Lebenszyklus Disconnected->PortOpen->Handshaking->HeartbeatActive->Armed->Disarmed; Disarm aus jedem Zustand; Wiederverbindung nach Trennung
- **T7 SafetyMonitor:** Controller-Verlusterkennung (PROG-009A): Entscheid pro Zustand bei IsConnected=false; Neutralverhalten (PROG-011): keine Eingabe -> Throttle=0-Kommando; Verlust->Wiederherstellung->erneuter Verlust; SafetyDecision-Objekt-Inhalte
- **T8 ControllerMapping:** exakte Grenzwerte (-32768, 0, +32767 Sticks; 0, 128, 255 Trigger), Zuordnung Mode-2 (linksY=GAS, linksX=Gieren, rechtsX=Rollen, rechtsY=Nicken) konsistent mit docs/controllerbelegung.md, Neutralstellung ergibt exakt 0.0 bzw. 0.5 je Feld, keine Spruenge (Monotonie ueber Stuetzstellen)
- **T9 QuadroConfig Freeze:** HeartbeatIntervalMs=1000, ArmingRepeatCount, Accessory-Werte 1/-1 - als Freeze-Tests, damit versehentliche Config-Aenderungen rot werden
- **T10 Golden Mission Stream (Integration):** vollstaendige Byte-Sequenz einer simulierten Mission: verbinden -> Handshake (5 Nachrichten) -> 3 Heartbeats -> Arming (MCC Throttle=-1 UND Thrust=-1, mehrfach laut Config, dann AccessoryDesired=1) -> Steuerkommandos (Roll/Pitch/Yaw/Throttle Muster) -> Disarming (-1) -> Neutralkommandos. JEDE Nachricht einzeln asserted. Sequenz ZWEIMAL erzeugen,Streams muessen byte-identisch sein (Determinismus)
- **T11 Cross-Konsistenz:** MessageFactory und UavTalkBuilder erzeugen fuer identische Eingaben identische Bytes (kein Drift zwischen zwei Codepfaden)

## Regeln

- Build/Test IMMER mit `/home/openclaw/dotnet-ms/dotnet` (apt-dotnet hat keine WPF-Targets)
- Keine Aenderungen ausserhalb tests/ (Ausnahme: dokumentierter Core-Bugfix in T-Aufgaben)
- HANDOFF-Datei nach jedem Meilenstein; Quota-Gate vor Child-Start (>=80% kein Start, >=95% Stop); keine API-Requests nach 00:30 UTC
- Fertigmeldung NUR mit selbst ausgefuehrten Zaehlungen (Testanzahl, Build-Exit), nie nur Kinder-Angaben

## Nach den Tests: Leak-Scan + Push + Bericht (Orchestrator fuehrt das selbst aus, kein Child)

1. **Leak-Scan (standing rule, vor JEDEM Push):** getrackte Dateien + History durchsuchen nach (a) Secret-Werten (Token-Muster wie ghp_..., AWS-Schluessel, PRIVATE KEY-Bloecke), (b) personenbezogenen Daten (private Domeins des Umfelds, interne IPs 192.168./10.0., VM-Benutzerpfade), (c) Binaerdateien (dll/exe/pdf/zip) und Kurs-PDFs. Fund = Push blockiert: Fix, Rescan bis 0.
2. **Push:** GitHub-Token aus `~/.hermes/gh-token` in eine Shell-Variable lesen (nie ausgeben, nie in Dateien/Config schreiben); damit einmalig pushen; die Remote-Konfiguration bleibt auf der sauberen HTTPS-URL ohne Token.
3. **Verifizieren:** anonymes ls-remote; Remote-HEAD muss lokalem HEAD entsprechen; Commit-Anzahl remote == lokal.
4. **Bericht:** NACHTBERICHT.md um Abschnitt erweitern (eigene Testzaehlungen vor/nach, Bereiche, Abweichungen, Quota-Prozent via zai_quota.py); committen.
5. **Exit:** eigenen Cron-Job deaktivieren (Name im jobs.json suchen, API update_job enabled=False), Lock entfernen, kompakter Discord-Bericht.
