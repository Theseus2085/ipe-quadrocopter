# Harte Kriterien – Programmierung (Quadrocopter IPE)

> **COMPLIANCE-REGEL (bindend für jede Architektur-, Design- und Implementierungsentscheidung):**
> Jede Entscheidung in diesem Projekt ist VOR der Umsetzung gegen diese Datei zu prüfen und zu begründen.
> Verletzt eine Lösung ein **FF-Kriterium** (Festforderung) oder eine Abgabe-/Frist-Regel → sie wird NICHT umgesetzt; stattdessen wird Simon die Verletzung explizit gemeldet (welche Regel, woraus sie kommt, was die Alternative ist).
> WA-Kriterien (Wünsche) sind verhandelbar, FF nicht. Änderungen an dieser Datei nur mit Simons ausdrücklicher Freigabe.
> Letzter Abgleich: 2026-10-28 gegen den kompletten OPAL-Kursstand (Lastenheft_Interd_PE.pdf = aktuelle Fassung mit Änderungen 26.10.2026; Lastenheft_Interd_PE_221012.pdf = veraltet, nicht verwenden).

## A. Lastenheft: Elektronik/Programmierung (alle FF = Festforderungen, außer gekennzeichnet)

| ID | Typ | Anforderung | Was das für die Software bedeutet | Verstoß = Punktabzug? |
|---|---|---|---|---|
| PROG-001 | FF | Nutzung der LibrePilot-Firmware | keine andere FC-Firmware flashen/planen | Ja, Festforderung verletzt |
| PROG-002 | FF | Konfiguration des FC nach den Anweisungen der Professur | FC-Setup exakt nach `FC_Konfiguration_SoSe2020.pdf` (Abschnitte I–IV); keine eigenen Wege in LibrePilot | Ja |
| PROG-003 | FF | Einrichtung und Konfiguration des Flightcontrollers | Teil der Software-Disziplin (LibrePilot-Arbeitschritte, s. wissen-programmierung.md §11) | Ja |
| PROG-004 | FF | Einrichtung und Konfiguration des OP-Link | OPLink-Kopplung gemäß Anleitung (Coordinator/Receiver, Kanäle 77–119, 57600 Bd, Max Power 6,3) | Ja |
| PROG-004A | FF | **Throttle Curve auf max. 0,3 bei 100 % Input** (LibrePilot → Configuration → Vehicle), S-Form beibehalten | nach Endkonfiguration zwingend einstellen; niemals Kurve über 0,3 hinaus planen/empfehlen. HINWEIS Widerspruch: Screenshot in FC_Konfiguration_SoSe2020.pdf zeigt max. 0,40 (alter Stand) — Lastenheft ist bindend: 0,3 | Ja |
| PROG-005 | FF | Applikation zur Steuerung des QC über den Controller | komplette Steuerungsapplikation ist Pflichtumfang | Ja |
| PROG-006 | FF | **Programmiersprache C#** | keine andere Sprache/Plattform; WPF-Vorlage ist Basis | Ja |
| PROG-006A | FF | **verständlicher Programmcode** ("Kommentare sinnvoll einsetzen") | Code-Kommentare sind Bewertungsgegenstand, kein Optional | Ja |
| PROG-007 | FF | **Nutzung der vorgefertigten Funktionsbibliothek** | KTCQuadroControl/vKTCQuadroControl verwenden (Verbinde/SchickeHexString/...); Eigenbau-Ersatz der DLL-Funktionen ist regelwidrig; Vorlagen-Delegat `vSchickeHexString` NICHT ÄNDERN | Ja |
| PROG-008 | FF | Erstellung einer GUI | Anwendung braucht echte GUI (WPF), keine Konolen-App | Ja |
| PROG-008A | FF | GUI-Elemente (Button, Checkbox etc.) zur **Kommunikationssteuerung mit dem FC** | Verbinden/Trennen/Handshake/Heartbeat/Arming müssen aus der GUI bedienbar sein | Ja |
| PROG-009 | FF | Implementierung **wesentlicher Sicherheitsfeatures** | Pflicht; ohne Sicherheitsfeatures = Anforderung verfehlt | Ja |
| PROG-009A | FF | **Erkennung des Verbindungsverlusts des Controllers** + Einleitung geeigneter Maßnahmen | Controller-Loss-Detection einbauen (z. B. XBox.IsConnected überwachen) + definierte Reaktion (mind. Statusanzeige; sinnvoll: Disarming/Neutralstellung der Steuerwerte) | Ja |
| PROG-010 | WA | Arming/Disarming im Stand ermöglichen | erwartete Kernfunktion | Wunsch, aber implizit über PROG-008A/Arming-Logik gefordert |
| PROG-010A | FF | **eindeutige Kennzeichnung des Arming-Zustands in der GUI** | Statusanzeige Armed/Disarmed ist Pflicht | Ja |
| PROG-011 | FF | **Stillstand der Rotoren, wenn keine Buttons/Trigger/etc. am Controller betätigt sind** | Neutralwerte/Throttle=0 senden, kein "letzter Wert klebt"; eng verknüpft mit PROG-009A | Ja, sicherheitsrelevant |

Quelle: Lastenheft_Interd_PE.pdf, Abschnitt "Anforderungsliste – Elektronik/Programmierung".

## B. Aufgabenstellung Programmierung (Aufgabenstellung_Interd_PE.pdf)

- **B1 (FF-artig):** Zwei Programme gefordert: (1) Flug in der virtuellen Testumgebung, (2) Flug mit dem realen QC. Ein Programm nur für die Simulation reicht nicht.
- **B2:** Eingabegerät ist der Controller (Xbox 360, USB) – Tastatur/Maus-Steuerung ist kein Ersatz.
- **B3:** Kommunikation PC ↔ QC über UAVTalk (nicht über LibrePilot-Middleware o. ä. – das Steuerprogramm spricht UAVTalk selbst über die DLL).
- **B4:** Gegebene Hardware (CC3D Revolution, OPLink Mini) mit LibrePilot konfigurieren – Teil des Software-Umfangs.
- **B5:** Ausschließlich die von der Professur ausgegebenen Komponenten für Antrieb, Elektrik, Elektronik. (Software-Relevanz: keine fremde Hardware ansteuern, keine anderen DLLs/FW einplanen.)
- **B6:** "Die veröffentlichten Termine ... sind vollständig einzuhalten. Bei Nichtbeachtung ist der Erfolg des Projektes bedroht." → Abschnitt D.
- **B7:** Pflichtenheft der Gruppe = Anforderungsliste auf Basis Lastenheft + Entwicklungsmotiv; Software muss die Programm-Anforderungen des Pflichtenhefts erfüllbar machen.

## C. Abgabedokumente / Beleg (Abgabedokumente_Interd_PE.pdf) – Software-relevant

**DVD (jeder Punkt in separatem Ordner):**
- **C1:** Punkt 5: "Visual Studio-Projekt der Steuerungsapplikation" – komplettes, öffnenbares VS-Projekt abgeben (nicht nur EXE).
- **C2:** Punkt 4: alle Dateien zur Reproduktion der Berechnungsaufgabe (Elektrik, aber Gruppe = eine DVD).
- **C3:** montierter QC + gedruckter Beleg (Hefter/Ringbindung) sind Teil der Gesamtabgabe.

**Beleg, Abschnitt Software – folgende Inhalte MÜSSEN dokumentiert sein:**
- **C4:** Erläuterung des Programmaufbaus: Übersichtsdarstellung + Beschreibung (GUI, Programmablauf)
- **C5:** Steuerungskonzept und Controllerbelegung (welcher Stick/Trigger steuert was)
- **C6:** Programmablaufpläne zur Darstellung der Programmlogik
- **C7:** Klassendiagramm – nur falls eigene Klassen erstellt wurden (Konsequenz: eigene Klassen = Dokumentationspflicht; Class-Diagramm einplanen)
- **C8:** Beschreibung der Sicherheitsfeatures: Art der Features + Implementierung (PROG-009/009A/011 in der Doku wiederfinden)
- **C9:** Formatierungsvorgaben der Professur VPE einhalten (Link im Dokument) – gilt für den ganzen Beleg, auch Software-Kapitel
- **C10:** Titelblatt aus `Titelblatt_Beleg.docx` nutzen (Disziplin "Prog – Programmierung" für Simon)

## D. Termine/Fristen (LV-Planung Stand 12.10.2026 + UE1; OPAL-Knoten Semesterablauf kann sich ändern → vor jedem Termin prüfen)

| Datum | Frist/Ereignis | Software-Betroffenheit |
|---|---|---|
| Di 17.11.26 | Zwischenpräsentation (1), 3. DS ZEU 250 | Projektstand Software muss präsentierbar sein |
| 08./09.12.26 | Abgabe der Fertigungsunterlagen (OPAL-Einschreibung) | indirekt (Gruppe), Fehlen = Projektrisiko |
| Di 26.01.27 | K12 Endkonfiguration (Präsenz) | FC-Endkonfig + Throttle Curve (PROG-004A) direkt danach |
| Di 02.02.27 | Testflüge (CADE/CAVE, OPAL-Einschreibung) | beide Programme müssen flugbereit sein (realer Flug!) |
| **Mi 10.02.27** | **Abgabe Beleg + QC + DVD** | VS-Projekt final (C1), Softwaredoku final (C4–C8) |
| Prüfungszeitraum | Verteidigung | Software-Teil verteidigen können |

"Ausgegebenen Termine vollständig einhalten; bei Nichtbeachtung ist der Erfolg des Projektes bedroht" (Aufgabenstellung).

## E. Technische Vorgaben aus VL5/VL6/UE4 (regelkonforme Umsetzung)

- **E1:** UAVTalk-Nachrichten exakt nach Protokoll: Sync `3C`, korrekter Message-Type, Length ohne Checksumme, ObjectIDs aus der offiziellen Tabelle (`ObjectID-Uebersicht_LibrePilot_1609.xlsx`, IDs ohne "0x"), Little Endian überall, CRC-8 automatisch via SchickeHexString.
- **E2:** ManualControlCommand: ALLE Variablen immer senden, feste Reihenfolge (Throttle, Roll, Pitch, Yaw, Collective=0, Thrust=Throttle, Channel[10]=0, Connected, FlightModeSwitchPosition=0).
- **E3:** Handshake exakt: 3× GCSTelemetryStats (Status 00→01→03) + die 2 vorgegebenen Metadaten-Nachrichten (Bytes wortwörtlich aus VL5 Folie 39).
- **E4:** Heartbeat: GCSTelemetryStats Status=03 jede Sekunde, eigener Thread.
- **E5:** Arming nur bei Throttle=-1 UND Thrust=-1 im ManualControlCommand (mehrfach schreiben), dann AccessoryDesired=1; Disarming AccessoryDesired=-1.
- **E6:** GUI-Updates aus Nebenthreads nur über Dispatcher/Delegate (WPF-Regel, VL6) – direkte GUI-Zugriffe aus Threads = bekannte Fehlerquelle.
- **E7:** Testumgebung zuerst: Programm zuerst gegen vKTCQuadroControl + Quadrocopter.exe lauffähig, Projekt sichern, dann auf KTCQuadroControl/realen FC umschreiben ("Ein Test mit dem realen FC ist unerlässlich").
- **E8:** Werkzeuge: Visual Studio 2015/2017/2019 + ".NET-Desktopentwicklung", Vorlage .NET 4.5.2, SharpDX.XInput via NuGet, LibrePilot 16.09 (nicht 15.09).

## F. Sicherheitsregeln mit Software-Bezug (Sicherheitshinweise_rechtliche_Hinweise.pdf)

- **F1:** Während Entwicklung/Konfiguration Propeller abmontieren ("die Motoren können sich ungeplant in Betrieb setzen") – Software-Tests am realen QC nur propellerlos, außer direkt beim Flug.
- **F2:** CC3D/OPLink nur MIT Antenne betreiben (Zerstörungsgefahr Antennenmodul).
- **F3:** Software-Verhalten muss F1-Rechnung tragen: kein unbeabsichtigtes Arming, kein Motorausgang bei Programmstart/Absturz (PROG-011, PROG-009).
- **F4:** Komponenten sind Lehrstuhl-Eigentum; Verlust = Ersatz (Übergabeprotokoll). Vollständige Rückgabe am Semesterende.

## G. Prüfroutine vor jeder Umsetzung (so wird diese Datei angewendet)

1. Betroffene Kriterien-IDs identifizieren (A–F).
2. Für jede Entscheidung notieren: erfüllt/verletzt/neutral – mit Begründung (kurz, im Arbeitslog der projekt-plan.md).
3. Bei Konflikt mit FF/Abgabe-Frist: STOPP, Simon informieren, complianten Alternativvorschlag machen.
4. Neue Erkenntnisse aus OPAL (Kurs aktualisiert sich!) → diese Datei aktualisieren und Änderungsdatum oben pflegen.
