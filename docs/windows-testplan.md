# Windows-Testplan (Checkliste für Simons PC)

> Der Logikkern und die 79 Unit-Tests wurden auf der Build-VM (Linux) geprüft.
> **Die WPF-Anwendungen und das Verhalten der Kurs-DLLs können nur unter
> Windows geprüft werden** — dieser Plan listet jeden Schritt mit erwartetem
> Ergebnis. Reihenfolge einhalten: erst virtuell, dann real, **propellerlos**
> zuerst (Sicherheitshinweis F1: „die Motoren können sich ungeplant in Betrieb
> setzen“).

## A. Vorbereitung (einmalig)

| Nr. | Schritt | Erwartetes Ergebnis |
|---|---|---|
| A1 | Visual Studio 2019 mit Workload *.NET-Desktopentwicklung* installiert? (Azure Dev Tools for Teaching, siehe E8) | VS startet, Workload unter *Extras → Tools abrufen* als installiert gelistet |
| A2 | Repo klonen bzw. `IPE-Quadrocopter.sln` per Doppelklick öffnen | Projektmappe zeigt 4 Projekte, keine fehlenden Projektverweise |
| A3 | `Vorlage_VS.zip` (OPAL → Projekt/Beleg → Vorlagen → Programmierung) entpacken; aus `Vorlage/bin/Debug/` die DLLs **und** LED-Bilder in `libs/` kopieren (Liste: [VORLAGE-DLLS.md](../VORLAGE-DLLS.md)) | `src/QuadroControl.Virtuell/libs/` und `src/QuadroControl.Real/libs/` enthalten je 3 DLLs + `LED_aus.png` + `LED_grn.png` |
| A4 | Projektmappe erstellen (*Erstellen → Projektmappe erstellen*) | NuGet `SharpDX.XInput 4.2.0` wird geladen; Build **ohne Fehler** (Hinweise zu XML-Doku sind ok). Bei Fehler `Datei „libs\…“ konnte nicht gefunden werden`: A3 prüfen |

## B. Unit-Tests (schneller Kern-Check, optional auf Windows)

| Nr. | Schritt | Erwartetes Ergebnis |
|---|---|---|
| B1 | In *Ansicht → Test-Explorer* alle Tests ausführen (oder `dotnet test tests/QuadroControl.Core.Tests/QuadroControl.Core.Tests.csproj`) | **79 passed / 0 failed** — deckt Protokoll-Vektoren, Handshake-Sequenz, Feldreihenfolge, Zustandsmaschine, Sicherheitslogik ab |

## C. Virtueller Test (Programm 1 + Quadrocopter.exe, E7)

Vorbereitung: Xbox-Controller per USB verbinden und einschalten.

| Nr. | Schritt | Erwartetes Ergebnis |
|---|---|---|
| C1 | Startprojekt `QuadroControl.Virtuell` setzen, starten (F5) | Fenster „MainWindow“ öffnet: links 4 Buttons, rechts COM-Port + Verbindungsstatus, unten rechts „Disarmed“ mit **dunkler** LED, unten Controller-Ausgabe |
| C2 | `Quadrocopter.exe` (OPAL: `Quadrocopter_EXE.zip`) starten → Netzwerkzugriff **Abbrechen** → Szene I/II/III wählen | Demonstrator zeigt die Szene, Rahmen **rot** (= Disarmed) |
| C3 | COM-Port `COM3` stehen lassen → Button **COMPort verbinden** | Verbindungsstatus: „Verbunden mit COM3 (Handshake noch ausführen).“ |
| C4 | Button **FC initialisieren und Heartbeat** | Status: „Handshake abgeschlossen, Heartbeat aktiv (1/s).“ Testumgebung meldet korrekte Handshake-Folge |
| C5 | Button **Controller lesen** | Achsen-Felder zeigen Live-Werte; Sticks bewegen → Werte ändern sich; Stick loslassen → Werte gegen 0 (Neutralsendung, PROG-011) |
| C6 | Taste **A** am Controller drücken | LED leuchtet **grün** + Text „Armed“; Demonstrator-Rahmen wird **grün** |
| C7 | Kurz fliegen: links Y = Höhe, links X = Drehung, rechts = Neigung | QC bewegt sich wie gesteuert; nach Loslassen der Sticks steht er ruhig (kein „Kleben“ alter Werte) |
| C8 | Taste **B** drücken | LED dunkel + „Disarmed“; Rahmen rot |
| C9 | **Controller während des Flugs ausschalten** (Sicherheitstest PROG-009A) | Meldung „Controller-Verlust erkannt: automatisch entwaffnet“, LED dunkel, QC setzt zur Landung neutral auf |
| C10 | Button **COMPort trennen**, dann Fenster schließen | Status „Getrennt.“; kein Fehler im Ausgabefenster; Demonstrator zeigt Disarmed |
| C11 | Programm neu starten und **FC initialisieren** **ohne** vorheriges Verbinden drücken | Nur Meldung im Status, **kein** Absturz, keine Steuerwerte gesendet (Zustandsmaschine blockt) |

## D. Realer Test (Programm 2) — **propellerlos!**

Vorbereitung: FC + OPLink gemäß Kurs-Anleitung konfiguriert (LibrePilot 16.09,
FC_Konfiguration_SoSe2020; **Throttle Curve max. 0,3 bei 100 % Input**,
PROG-004A; OPLink-KopplungChannels 77–119, 57600 Bd, Max Power 6,3 — PROG-004).
**Propeller abmontiert** (F1). OPLink nur mit Antenne betreiben (F2).

| Nr. | Schritt | Erwartetes Ergebnis |
|---|---|---|
| D1 | Startprojekt `QuadroControl.Real`, starten | Wie C1, zusätzlich roter Sicherheitshinweis „nur propellerlos testen“ in der Kopfzeile |
| D2 | Button **COM-Ports suchen** (HoleAktuelleCOMPorts) | Verfügbare COM-Ports erscheinen im Status; erster wird in TB_COMPort eingetragen (Revolution Virtual COM Port bzw. OPLink) |
| D3 | **COMPort verbinden** → **FC initialisieren und Heartbeat** | Statusmeldungen wie C3/C4; in LibrePilot GCS (falls geöffnet) steigt die UAVTalk-Aktivität |
| D4 | **Controller lesen** → Stickbewegungen | Wie C5; zusätzlich Feld „Gas als DLL-Hex“ zeigt den per `KonvertiereFloatZuHexString` erzeugten Little-Endian-Float (z. B. `00 00 80 3F` bei Vollgas) |
| D5 | **Motorprüfung propellerlos**: Taste **A** (Arming) → vorsichtig Gas geben → sofort Taste **B** | Motoren spinnen an/aus wie befohlen; LED grün → dunkel; **niemals** Motorlauf ohne Eingabe (PROG-011) |
| D6 | Controller-Verlust-Test wie C9 | Automatisches Disarming, Motoren stoppen |
| D7 | **Erst nach erfolgreichem D1–D6**: Propeller montieren, Testflug gemäß Terminplan (02.02.27, CADE/CAVE) | Flug wie in der Testumgebung (C7) |

## E. Nach dem Test

| Nr. | Schritt | Erwartetes Ergebnis |
|---|---|---|
| E1 | Ergebnisse in diesem Plan abhaken; Abweichungen notieren | Liste für die Fehlerbehebung |
| E2 | Projekt sichern (E7-Regel der VL5: „alles funktioniert? → Projekt sichern!“) | ZIP/Kopie des Funktionierenden vor dem Real-Umbau bzw. vor weiteren Änderungen |

## Bekannte Grenzen dieser Checkliste

- Die GUI wurde auf der Linux-Build-VM **nur als XML** geprüft (wohlgeformt,
  Vertragsnamen, Handler). Das visuelle Layout (Abstände, Umbrüche,
  LED-Anzeige) zeigt sich erst unter Windows — Punkt C1 prüft das bewusst als
  Erstes.
- Das Senden selbst (CRC-8-Anhang der DLL, Timing der Testumgebung) ist auf
  der VM nicht ausführbar; die erzeugten Bytefolgen sind dagegen
  bitgenau getestet.
