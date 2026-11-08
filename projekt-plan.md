# Projektplan – Quadrocopter IPE (Programmierung)

## Aktueller Stand

- **2026-10-28:** Projektordner angelegt. Kompletter OPAL-Kurs gespiegelt und ausgewertet:
  Wissen (`wissen-programmierung.md`) + verbindliche Kriterien (`harte-kriterien.md`) erstellt.
- **2026-10-28 abends:** Nachtbuild vorbereitet: dotnet-sdk-8.0 installiert (Core-Tests laufen auf der VM),
  Quellen + PDF-Seiten-Renderings in `quellen/`, Build-Auftrag `BUILD-AUFTRAG.md`, Git-Repo initialisiert,
  Flash-Vision-Pass für Screenshot-Inhalte (FC-Konfig-Werte, GUI-Referenz), Cron-Job 530a5a809043
  (23:00 Berlin: Claude-GLM full baut Logik+Apps, Flash-Pass macht XAML/Doku, Tests bis grün, privater
  GitHub-Push Theseus2085/ipe-quadrocopter, Bericht am Morgen im Home-Channel).
- **2026-10-29:** Nachtbuild fertig: komplette Solution (Core + beide WPF-Apps), Kompilierung auf der
  Linux-VM verifiziert, öffentliches Repo gepusht. Test-Suite-Expansion in der Folgenacht auf
  **385 Unit-Tests** (alle grün, TEST-AUFTRAG T1-T11).
- **2026-11-03:** **Kompletter Laufzeittest der virtuellen Testumgebung auf Azure-Windows-VM
  (Windows Server 2022): erfolgreich.** Verbinden → Handshake → Heartbeat → Controller →
  Arming → Flug → Disarming in Szenen I/II/III gegen den echten Unity-Simulator, inklusive
  Wiederverbinden + Re-Handshake nach Heartbeat-Ausfall. Dabei DLL-Arming-Kontrakt dekompiliert
  und Sequenz korrigiert (ABWEICHUNGEN 17, 18). Release-Paket für Simons PC erstellt
  (Nextcloud `/IPE-Quadrocopter/`).
- **2026-11-08:** Doku auf den Stand 08.11. gebracht (Arming-v2-Sequenz überall nachgezogen,
  Testzähler, Live-Test-Vermerke) und **Sofort-Startpaket `release/`** gebaut: beide Programme
  frisch kompiliert + Unity-Simulator + Installations-ANLEITUNG-INSTALLATION.html — läuft ohne
  jede Installation auf Windows 10/11 (Details: NACHTBERICHT.md).
- Software-Status: **virtuelle Testumgebung vollständig verifiziert**; Real-App baut, Laufzeittest
  mit echtem FC ausstehend.
- Nächster fester Termin für die Gruppe: Zwischenpräsentation 17.11.2026.

## Offene Punkte

- [x] Vorlage_VS.zip als Startpunkt sichern (in quellen/, Nachtbuild extrahiert sie)
- [x] Entscheidung Steuerungskonzept: D3v2-Belegung (R2=GAS, links X=Gieren, rechts X=Rollen, rechts Y=Nicken; links Y frei; A=Arming, B=Disarming) — in BUILD-AUFTRAG.md fixiert, ersetzt D3 (Mode-2) vom 28.10.
- [x] GUI-Layout: UE4-Zielprogramm-Referenz (Screenshots ausgewertet)
- [x] Sicherheitsfeatures konkretisiert (Controller-Loss→Auto-Disarm, Neutral-Befehle, Arming-LED, State-Gate)
- [x] Kompletter Laufzeittest virtuelle Testumgebung (03.11.2026 auf Windows Server 2022, Szenen I–III)
- [x] Sofort-Startpaket `release/` ohne Installationsaufwand (08.11.2026)
- [ ] FC + OPLink konfigurieren (Anleitung I–III) — Simon an seinem Windows-PC, Komponenten seit 27.10. da
- [ ] Windows-Checkliste Restpunkte: C9 (echtes Gamepad ausschalten) + Abschnitt D realer FC,
  propellerlos (docs/windows-testplan.md)
- [ ] Beleg-Softwarekapitel final schreiben (Vorlagen aus docs/)

## Plan

(von Simon zu füllen — seine Planung, Agent arbeitet sie mit ab)

## Arbeitslog

- 2026-10-28 — Ordner-Bootstrap, kompletter OPAL-Kurs-Scan, Wissens- + Kriteriendatei erstellt — fertig
- 2026-10-29 — Nachtbuild: komplette Solution, 79 Tests, öffentlicher GitHub-Push; Nacht 30.10.: Test-Expansion auf 385 Tests
- 2026-11-03 — Azure-VM-Runtime-Tests: Smoke-Test, dann Volltest Szenen I–III inkl. Arming-Fix v2 (ABWEICHUNGEN 17/18); Release-Paket auf NC
- 2026-11-08 — Doku-Standsgleichung (Arming v2 in allen Dateien, Testzähler 385, Live-Vermerke) + release/-Sofortstartpaket gebaut
