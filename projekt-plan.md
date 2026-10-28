# Projektplan – Quadrocopter IPE (Programmierung)

## Aktueller Stand

- **2026-10-28:** Projektordner angelegt. Kompletter OPAL-Kurs gespiegelt und ausgewertet:
  Wissen (`wissen-programmierung.md`) + verbindliche Kriterien (`harte-kriterien.md`) erstellt.
- **2026-10-28 abends:** Nachtbuild vorbereitet: dotnet-sdk-8.0 installiert (Core-Tests laufen auf der VM),
  Quellen + PDF-Seiten-Renderings in `quellen/`, Build-Auftrag `BUILD-AUFTRAG.md`, Git-Repo initialisiert,
  Flash-Vision-Pass für Screenshot-Inhalte (FC-Konfig-Werte, GUI-Referenz), Cron-Job 530a5a809043
  (23:00 Berlin: Claude-GLM full baut Logik+Apps, Flash-Pass macht XAML/Doku, Tests bis grün, privater
  GitHub-Push Theseus2085/ipe-quadrocopter, Bericht am Morgen im Home-Channel).
- Software-Status: Implementierung läuft heute Nacht (BUILD-AUFTRAG.md).
- Nächster fester Termin für die Gruppe: Zwischenpräsentation 17.11.2026.

## Offene Punkte

- [x] Vorlage_VS.zip als Startpunkt sichern (in quellen/, Nachtbuild extrahiert sie)
- [x] Entscheidung Steuerungskonzept: Mode-2-Belegung (links Y=GAS, links X=Gieren, rechts X=Rollen, rechts Y=Nicken; A=Arming, B=Disarming) — in BUILD-AUFTRAG.md fixiert
- [x] GUI-Layout: UE4-Zielprogramm-Referenz (Screenshots ausgewertet)
- [x] Sicherheitsfeatures konkretisiert (Controller-Loss→Auto-Disarm, Neutral-Befehle, Arming-LED, State-Gate)
- [ ] FC + OPLink konfigurieren (Anleitung I–III) — Simon an seinem Windows-PC, Komponenten seit 27.10. da
- [ ] Windows-Checkliste abarbeiten (docs/windows-testplan.md nach dem Nachtbuild)
- [ ] Beleg-Softwarekapitel final schreiben (Vorlagen aus docs/ nach dem Nachtbuild)
- [ ] Morgen: Nachtbericht lesen, Build verifizieren, ggf. Push nachholen

## Plan

(von Simon zu füllen — seine Planung, Agent arbeitet sie mit ab)

## Arbeitslog

- 2026-10-28 — Ordner-Bootstrap, kompletter OPAL-Kurs-Scan, Wissens- + Kriteriendatei erstellt — fertig
