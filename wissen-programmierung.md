# Wissen Programmierung – Interdisziplinäre Produktentwicklung (Quadrocopter)

> Wissensdatei für Simons Disziplin im Gruppenprojekt: **Programmierung (Prog)**.
> Stand: 2026-10-28. Quelle: kompletter OPAL-Kurs "Interdisziplinäre Produktentwicklung" (NC-Spiegel, Kurs-ID 13878525957), alle Dokumente gescannt.
> Verbindliche Anforderungen stehen in **`harte-kriterien.md`** (dort mit Konsequenzen). Diese Datei ist das Hintergrundwissen.
> Pflicht-Quellen bei Arbeit am Code: VL5-/VL6-Skript + UE4-Skript (OPAL: Vorlesungen / Übungen).

---

## 1. Projektüberblick

- **Lehrveranstaltung:** Interdisziplinäre Produktentwicklung, Professur Virtuelle Produktentwicklung (VPE / früher KTC), TU Dresden, WiSe 2026/27.
- **Ziel:** Gruppe von 7 Studierenden entwickelt, konstruiert, fertigt, montiert und testet einen flugfähigen Quadrocopter (QC). Abschluss: Beleg + DVD + montierter QC + Verteidigung.
- **Disziplinen:** Projektmanagement (P), Konstruktion/M-CAD (K), Elektrik/E-CAD (E), **Programmierung (Prog) = Simon, gesamte Software**.
- **Modulnote:** gehört zu Mechatronik (Bäker): 120 min schriftliche Prüfung + Beleg (komplette Dokumentation + Verteidigung).
- **Kontakte:**
  - Dr.-Ing. Bernhard Saske – Vorlesung, Übungen, Projekt, **Programmierung** (bernhard.saske@tu-dresden.de, ZEU 219d, 0351-463-34418)
  - Prof. Dr.-Ing. Kristin Paetzold-Byhain – Modulverantwortliche (kristin.paetzold@tu-dresden.de, ZEU 219a)
  - Sebastian Schwoch – VL Elektrokonstruktion; Dr.-Ing. Stefan Holtzhausen – VL Additive Fertigung
  - Sebastian Klement – Komponentenausgabe/-rückgabe, Endkonfiguration (sebastian.klement@tu-dresden.de)
  - Werkstatt: Daniel Michael, Christian Norkus
- **Entwicklungsmotiv:** Gruppe wählt 1–3 Motive (z. B. Sport, Einsteiger, Indoor, Transportabel, Pakettransport, Modular, Allwetter ...), eigene Ideen ausdrücklich erwünscht. Motiv beeinflusst das Pflichtenheft (Anforderungsliste) der ganzen Gruppe.
- **Pflichtenheft ≠ Lastenheft:** Lastenheft stellt die Professur (Auftraggeber). Die Gruppe erarbeitet daraus ein Pflichtenheft (Anforderungsliste) als Entwicklungsgrundlage.

## 2. Aufgabe Programmierung (aus Aufgabenstellung_Interd_PE.pdf)

1. Gegebene Hardware konfigurieren: Flightcontroller **CC3D Revolution** + **OPLink-Mini Groundstation** mit **LibrePilot**.
2. **Anwendung zur Steuerung des QC per PC programmieren.** Eingabegerät: **Controller** (Xbox 360, USB). Kommunikation PC ↔ QC über **UAVTalk**.
3. **Zwei Programme sind gefordert:**
   - Programm 1: Flug in der **virtuellen Testumgebung** (Testumgebung + Quadrocopter.exe-Demonstrator).
   - Programm 2: Flug mit dem **realen QC**.
4. Ausschließlich die von der Professur ausgegebenen Komponenten nutzen (Antrieb, Elektrik, Elektronik).

## 3. Systemarchitektur (Gesamtsystem)

```
[Xbox 360 Controller] --USB--> [PC/Laptop: Steuerprogramm (C#, WPF)]
                                    |  wandelt Controllerstellung in UAVTalk-Nachrichten
                                    |  Handshake + Heartbeat + GUI + Sicherheitsfeatures
                              [Serieller Port / COM]
                                    |
                        (entweder USB-Kabel direkt zum FC)
                        (oder 433 MHz Funk über OPLink Mini)
                                    |
                     [Flightcontroller CC3D Revolution]
                       - Kernanwendung: Regelalgorithmus
                       - UAVObjects: lesen/schreiben
                       - setzt ManualControlCommand in Motorwerte um
```

- **Kernanwendung im FC** regelt den Flug selbst. Das PC-Programm ist KEIN Regler – es liefert nur Steuerbefehle (Throttle/Roll/Pitch/Yaw) und Arming über UAVObjects.
- **Telemetrie-Pfade:** USB-Kabel direkt an FC (COM-Port "Revolution Virtual COM Port") ODER drahtlos über OPLink Mini (dann COM-Port des OPLink Mini nutzen; USB-Kabel am FC nur noch Stromversorgung). Beides funktioniert parallel.
- **Xbox-Controller:** 2 Sticks (je X/Y, ±32767), 2 Schultertrigger (0–255), Schultertasten, A/B/X/Y, Start/Back, D-Pad. Steuersignale für Steigen/Sinken, Nicken (Pitch), Rollen (Roll), Gieren (Yaw).
- **Bewegungsarten des QC:** Steigen/Sinken = Translation Z; Nicken = Rotation um Y; Rollen = Rotation um X; Gieren = Rotation um Z. Quadrocopter: 6 DOF, gesteuert über 4 Grundbewegungsarten (Drehzahlmischung der 4 Rotoren, 2× CW / 2× CCW).

## 4. Hardware (aus Lastenheft + Übergabeprotokoll)

| Komponente | Details |
|---|---|
| Flightcontroller | CC3D Revolution (36×36×8 mm, 9 g), LibrePilot-Firmware 16.09, Antenne Pflicht |
| Telemetrie | OPLink Mini Ground Station (433 MHz, 29×20 mm, 3 g) + Antenne (nie ohne Antenne betreiben) |
| Motoren | 4× DJI Snail 2305 (2400 kV, max. Schub 8,8 N mit 6030S-Prop) |
| ESCs | 4× DJI Snail 430-R (+ ESC-Programmer-Kabel zur Konfiguration) |
| Propeller | DJI 6030S Quick Release, 2× CW, 2× CCW (Markierung am Quick-Release = CCW) |
| Akku | P-Ion 3S 3200 mAh (240 g) – beim Herrn Klement einsehbar, wird gestellt |
| PDB | Power Board mit UBEC 5 V und 12 V |
| Steuerung | Xbox 360 Controller für Windows, kabelgebunden USB |
| Sonstiges | Spannungstester (Anzeige + Taster müssen zugänglich/sichtbar bleiben – Konstruktion, nicht Software) |

## 5. UAVTalk-Protokoll (VL5)

Standardisierte Bytefolgen zwischen PC und FC. Alles **Little Endian** (Bytefolge jedes Feldes umgedreht – auch im Header!).

**Nachrichtenaufbau:**

| Feld | Name | Länge | Inhalt |
|---|---|---|---|
| A | Sync-Value | 1 B | immer `3C` |
| B | Message-Type | 1 B | `20` Objektdatenübertragung, `21` Objektanfrage, `22` Übertragung m. Bestätigungsanfrage, `23` positive Bestätigung, `24` negative Bestätigung |
| C | Length | 2 B | int16, Gesamtzahl Bytes Header+Daten, OHNE Checksumme |
| D | ObjectID | 4 B | ID des UAVObjects (Tabelle OPAL – Softwareanleitungen – UAVTalk und LibrePilot / ObjectID-Uebersicht_LibrePilot_1609.xlsx) |
| E | InstanceID | 2 B | meist `00 00` (AccessoryDesired ist mehrfach instanziiert) |
| F | Daten | X B | Bytefolgen ALLER Variablen des UAVObjects, feste Reihenfolge, immer ALLE Felder senden |
| G | Checksumme | 1 B | CRC-8; wird von `SchickeHexString()` automatisch berechnet und angehängt |

- Beispiel aus VL5: `3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF ??` (AccessoryDesired schreiben).
- ObjectID im Little Endian: ManualControlCommand ID `C4 10 74 80` → in der Nachricht `80 74 10 C4`.
- Aus der ObjectID-Tabelle (xlsx): IDs OHNE führendes "0x" in die Nachricht eintragen.

**Variablenkodierung:** int32/float32 (IEEE 754) ergeben unterschiedliche Bytefolgen für denselben Wert (100 int32 = `00 00 00 64`, 100 float = `42 C8 00 00`). Konvertierungshilfe: `KonvertiereFloatZuHexString(float)` der KTCQuadroControl (liefert bereits Little-Endian-String "XX XX XX XX").

## 6. UAVObjects – die drei wichtigen (VL5)

**ManualControlCommand** – ObjectID `C4 10 74 80`, Instanz `00 00`. Wichtigstes Objekt: Steuerbefehle an den FC. Datenabschnitt in DIESER Reihenfolge (alle Felder immer senden):

| # | Name | Typ | Wertebereich |
|---|---|---|---|
| 1 | Throttle | float32 | -1 = deaktiviert (für Arming nötig); 0…1 = Gasstellung |
| 2 | Roll | float32 | -1…1 |
| 3 | Pitch | float32 | -1…1 |
| 4 | Yaw | float32 | -1…1 |
| 5 | Collective | float32 | immer `00 00 00 00` (für Flug unwichtig) |
| 6 | Thrust | float32 | immer gleich Throttle-Wert |
| 7 | Channel | int16[10] | 20 Bytes, alle `00` |
| 8 | Connected | enum, 1 B | `00` Disconnected, `01` Connected (für Flug benötigt) |
| 9 | FlightModeSwitchPosition | uint8, 1 B | `00`…`05`; `00` bei Lehrstuhlkonfiguration |

**AccessoryDesired** – ObjectID `C4 09 98 5A`, Instanz `00 00`. "Schalter" für Arming/Disarming. Daten: AccessoryVal float32: `1` = Arming, `-1` = Disarming.

**GCSTelemetryStats** – ObjectID `CA D1 DC 0A`. Aktiviert die UAVTalk-Schnittstelle am FC. Daten 37 Bytes gesamt: TxDataRate…RxCrcErrors (alle `00`), am Ende Status enum 1 B: `00` Disconnected, `01` HandshakeReq, `02` HandshakeAck, `03` Connected.

- Vollständige ObjectID-Tabelle (114 Objekte, LibrePilot 16.09): OPAL → Softwareanleitungen → UAVTalk und LibrePilot → `ObjectID-Uebersicht_LibrePilot_1609.xlsx` (auch im NC-Spiegel).
- Metadaten-Objekt: ObjectIDMeta liegt NAHE der ObjectID, die Regel "ObjectID + 1" gilt NUR für einfach instanziierte Objekte (ManualControlCommand: C4 10 74 80 → Meta C4 10 74 81). Für mehrfach instanziierte Objekte weicht sie ab (AccessoryDesired: C4 09 98 5A → Meta C4 09 98 5C, LibrePilot-Konvention). Metadaten-Bytes deshalb NIEMALS ableiten, immer die wortwörtlichen VL5-Strings verwenden (ABWEICHUNGEN #16, korrigiert 2026-10-30).
- UAVObjects live ansehen: LibrePilot GCS → "System"-Tab.

## 7. Kommunikationsablauf PC ↔ FC (5 Schritte, VL5 Folie 37 ff.)

1. **Verbindung aufbauen:** COM-Port öffnen via `Verbinde("COMx")` (KTCQuadroControl) bzw. `vVerbinde(...)` (Testumgebung). FC erscheint im Geräte-Manager als "Revolution Virtual COM Port (COMx)".
2. **Handshake (einmalig):** 5 UAVTalk-Nachrichten senden:
   - 3× GCSTelemetryStats: Status `00`, dann `01`, dann `03` (restliche Datenbytes je `00`)
   - dann 2 Metadaten-Anpassungen (wortwörtlich aus VL5 Folie 39 übernehmen):
     - `3c 20 12 00 81 74 10 c4 00 00 41 00 c0 07 64 00 00 00` (ManualControlCommand-Metadaten)
     - `3c 20 12 00 5c 98 09 c4 00 00 41 00 d0 07 64 00 00 00` (AccessoryDesired-Metadaten)
3. **Heartbeat:** GCSTelemetryStats mit Status `03` **jede Sekunde** senden, in **eigenem Thread**.
4. **Flug:** armen, fliegen, disarmen.
   - Arming: ManualControlCommand **mehrmals hintereinander** mit Throttle = -1 schreiben (Rest egal) → dann AccessoryDesired = 1.
   - Arming nur möglich, wenn **Throttle UND Thrust = -1** (VL5 Folie 41).
   - Disarming: AccessoryDesired = -1.
   - Steuerbefehle laufend via ManualControlCommand, in eigenem Thread.
5. **Trennen:** `VerbindungTrennen()` (erst bei Programmbeendigung).

**LED-Feedback:** blaue/rote LED am FC zeigen den Zustand der Kommunikationsschritte (Blinkverhalten ändert sich mit Zustand).

## 8. Bibliotheken (Pflicht: PROG-007)

Vorlage: OPAL → Projekt/Beleg → Vorlagen → Programmierung → `Vorlage_VS.zip` (WPF, .NET 4.5.2). Enthält in `bin/Debug`: `KTCQuadroControl.dll` (+ .xml Doku), `vKTCQuadroControl.dll`, `SerialPort.dll` (v0.7.x), `CRC.dll`, LED-Bilder (LED_aus.png, LED_grn.png).

**KTCQuadroControl (realer FC):**
| Methode | Beschreibung |
|---|---|
| `bool Verbinde(string COMPort)` | COM-Port verbinden ("COMx"); true bei Erfolg |
| `bool VerbindungTrennen()` | trennen; true bei Erfolg |
| `string[] HoleAktuelleComPorts()` | aktuell belegte COM-Ports des PC |
| `static void SchickeHexString(string)` | HexString "XX XX XX ..." senden, wandelt in Bytes, **berechnet CRC-8 automatisch und hängt an**. Voraussetzung: COM verbunden |
| `static string KonvertiereFloatZuHexString(float)` | float → "XX XX XX XX" bereits Little Endian |

Aufruf: public Methoden über Objektinstanz (`QuadroControl meinQC = new QuadroControl(); meinQC.Verbinde(...)`), static über Klasse (`QuadroControl.SchickeHexString(...)`). Namensraum `using KTCQuadroControl;`.

**vKTCQuadroControl (virtuelle Testumgebung):**
| Methode | Beschreibung |
|---|---|
| `bool vVerbinde(string comPort)` | virtueller COM-Port, Name beliebig |
| `bool vVerbindungTrennen()` | trennen, Rückgabe immer true |
| `vSchickeHexString(string)` | **Delegat-Funktion, bereits in der Vorlage definiert – NICHT ÄNDERN** (steht in MainWindow.xaml.cs unter "NICHT ÄNDERN"); ohne Objekt-/Klassenbezug aufrufbar |

Nur in KTCQuadroControl: `HoleAktuelleComPorts()`, `KonvertiereFloatZuHexString()`. Objekt `vmyQC` ist in der Vorlage bereits instanziiert.

**Vorlage-Mechanik (MainWindow.xaml.cs der Vorlage):** Delegat `public delegate void DelString(string hexString);`, Felder `private vQuadroControl vmyQC; DelString DelStringObject;`, und die Dispatcher-gesicherte Methode `vSchickeHexString` (CheckAccess → BeginInvoke) sind vorgegeben und dürfen nicht geändert werden.

**SharpDX.XInput** (Xbox-Controller, VL6): via NuGet-Paket-Manager installieren ("SharpDX.Xinput"). Nutzung:
```csharp
using SharpDX.XInput;
private Controller XBox = new Controller(UserIndex.One);
private State myState;
myState = XBox.GetState();
int leftTrigger = myState.Gamepad.LeftTrigger;          // 0...255
int rightAnalogX = myState.Gamepad.RightThumbX;          // -32768...32767
if (myState.Gamepad.Buttons == GamepadButtonFlags.A) { } // Tastenabfrage
```
Wertebereiche beachten: Trigger 0–255, Sticks ±32767 (Aufskalierung auf -1…1 für Roll/Pitch/Yaw nötig).

## 9. Testumgebung + Demonstrator Quadrocopter.exe

**Testumgebung (vKTCQuadroControl):** simuliert die Reaktion des QC auf UAVTalk-Nachrichten; Rückmeldung über korrekte Durchführung der Kommunikationsschritte, des Armings und der übergebenen ManualControlCommand-Werte. "Das Verhalten der Testumgebung kommt dem des FC sehr nah. Ein Test mit dem realen FC ist aber unerlässlich!"

**Quadrocopter.exe** (Unity-Demonstrator, `Quadrocopter_EXE.zip`, OPAL → Demonstrator für eigenen PC; Anleitung: `Anleitung_Quadrocopter_exe.pdf`):
1. Eigenes Steuerungsprogramm in Visual Studio starten (Testumgebung muss aktiv und voll funktionsfähig sein).
2. Quadrocopter.exe starten; Netzwerkzugriff NICHT erlauben (Abbrechen).
3. Szene wählen: I – Ebene, II – Sporthalle, III – Wiese.
4. Im Steuerungsprogramm: COM-Port verbinden → Handshake + Heartbeat → Arming.
5. Fliegen. Roter Rahmen = Disarmed, grüner Rahmen = Armed. Anzeige Flugdauer/Höhe/Distanz. "Zurücksetzen"-Button der Testumgebung setzt QC auf Startplattform.

**Vorgehen laut VL5 (empfohlene Reihenfolge):** Programm für Testumgebung + Quadrocopter.exe schreiben → alles funktioniert? → Projekt sichern! → für realen FC umschreiben (im Wesentlichen: KTCQuadroControl statt vKTCQuadroControl, echter COM-Port).

**Demonstrator-Interna (aus der EXE):** Unity-App; liest/schreibt XML (`XML_schreiben.xml`, QCBaugruppeCollection mit Rotorpositionen/Farben, Grundkörper mit STL/OBJ-Pfaden). Für die Steuerungsapplikation nicht direkt relevant (Testumgebung kapselt die Schnittstelle), zeigt aber: Rotorpositionen ±0,1294 m Diagonale, Höhe 0,09 m.

## 10. Programmstruktur (WPF, VL6)

- **WPF-App** (MainWindow : Window). Visual Studio 2015/2017/2019 ("Version ist egal"), Paket ".NET-Desktopentwicklung". .NET 4.5.2 (Vorlage). Uni-Bezug kommerzieller VS-Versionen: Microsoft Azure Dev Tools for Teaching (tu-dresden.de/ing/maschinenwesen/cad/service/software/imagine).
- **Threading:** GUI = Haupt-Thread. Controller-Abfrage, Heartbeat, Steuersignale = eigene Threads (`using System.Threading`).
  ```csharp
  Thread ControllerThread = new Thread(ControllerThreadFunction); ControllerThread.Start();
  private void ControllerThreadFunction() { while (true) { /* Controller lesen, Werte versenden */ } }
  ```
  Kontrolliertes Beenden: bool-Flag + try/catch(ThreadAbortException), `ControllerThread.Abort()`.
- **GUI-Updates aus Nebenthreads nur über Dispatcher + Delegate** (System.InvalidOperationException sonst):
  ```csharp
  public delegate void UpdateGUIDelType(string[] values);        // namespace-Ebene
  private UpdateGUIDelType UpdateGUIDel;                          // in MainWindow
  UpdateGUIDel = UpdateGUI;                                       // im Konstruktor
  private void UpdateGUI(string[] values) {
      if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(DispatcherPriority.Normal, UpdateGUIDel, values); return; }
      // GUI-Elemente schreiben (Textbox1.Text = values[1]; ...)
  }
  ```
- **Namenskonventionen der Vorlage/Übungen (sollte beibehalten werden):** Buttons `BT_*` (BT_COMPortVerbinde, BT_COMPortTrennen, BT_HS_HB, BT_XBoxRead/BT_XBoxStart), TextBoxen `TB_*` (TB_COMPort, TB_LeftTrigger, TB_AnalogRY, TB_ButtonA), Labels `Label_*` (Label_VerbindungStatus, Label_ArmingStatus). Für UE4 geforderte GUI-Elemente: COM-Port-Eingabe + Verbindungsstatus + Arming-Status + Buttons für die Kommunikationsschritte.
- **Entwicklungsablauf (VL6):** Algorithmus festlegen (umgangssprachlich / Programmablaufplan, Tool papdesigner) → Realisierung festlegen (GUI-Gestaltung, Threadstruktur) → Codieren → Testen (Testumgebung, dann FC) → Benutzen.

## 11. FC-/OPLink-Konfiguration mit LibrePilot (Anleitung FC_Konfiguration_SoSe2020.pdf)

LibrePilot **16.09** (Installer auf OPAL → Libre Pilot Download, `LibrePilot-16.09_i686.exe`), inkl. aller Treiber installieren.

**I. Grundkonfiguration FC:**
1. Antenne aufstecken. LibrePilot 16.09 installieren. FC per Micro-USB anschließen → Geräte-Manager: "Revolution Virtual COM Port (COMx)", Nummer merken.
2. Firmware-Tab: falls Firmware-Tag nicht 16.09 → "Upgrade and Erase" (Ziel: Tag 16.09).
3. Configuration → Hardware: **USB VCP Function = "USB Telemetry"** → Save → Disconnect → FC trennen, 5 s warten, neu anschließen → über COM-Port verbinden.
4. Configuration → Input → **Arming Settings**: "Arm airframe using throttle off and:" = **"Accessory 0"**; **Arming timeout = 0 seconds** → Save.
5. Configuration → Vehicle → Multirotor: Motor output channels wie in Anleitung (Abbildung) → Save.

**II. OPLink Mini + FC koppeln (433 MHz):**
1. OPLink Mini mit Antenne anschließen (nie ohne Antenne betreiben), in LibrePilot Configuration → OPLink: Protocol **OPLinkCoordinator**, Link Type **Data**, Max Chan 119, Min Chan 77, Max Power 6,3, ComSpeed 57600, VCP Port Serial → Save, 15 s warten, neu verbinden → **Device ID notieren**.
2. FC verbinden → OPLink: Protocol **OPLinkReceiver**, Coordinator ID = Device-ID des OPLink Mini, Rest wie oben → Save, 15 s warten.

**III. Kontrolle:** OPLink Mini UND FC am PC → LibrePilot über COM-Port des OPLink Mini verbinden → Konfiguration des CC3D muss erscheinen. Steuerungsprogramm kann dann den OPLink-COM-Port nutzen (USB am FC nur noch Strom).

**IV. Endkonfiguration (nach Montage, bei Herrn Klement):** danach im Vehicle-Tab die **Throttle Curve** einstellen: **max. 0,3 bei 100 % Input** (PROG-004A), S-Form beibehalten (guter Ausgangspunkt, darf für besseres Flugverhalten angepasst werden). Achtung Widerspruch: der Screenshot in der Anleitung zeigt Stützpunkte 0.00/0.10/0.20/0.31/0.40 (älterer Stand); das Lastenheft (bindend, FF) fordert max. 0,3 → auf 0,3 einstellen. Input → Flight Mode Switch Settings: **Flight Mode Count = 1**, Pos. 1 = Stabilized1/Bank1/Assisted None. Stabilization Modes: Stabilized1 = Roll **Attitude**, Pitch **Attitude**, Yaw **AxisLock**, Thrust **CruiseControl**. Failsafe Settings: Default (Failsafe-Flugmodus-Checkbox unchecked; Throttle -100 %, alle anderen 0 %). Arming Settings wie oben (Accessory 0, Timeout 0 s).

**Aus den Screenshots extrahierte Konkretwerte (bildinhalte.md, 2026-10-28):**
- Airframe Type: **Quad X**. Motor output channels: **NW = Channel9, NE = Channel10, SE = Channel11, SW = Channel12** (Status dann "Mixer OK"; "Reverse all motors" unchecked; Tricopter Yaw Servo = None; RC Input Accessory0–3 → Output None → Curve1).
- Input Channel Configuration: Throttle/Roll/Pitch/Yaw/FlightMode/Collective/Accessory0–3 alle Type None, Number Disabled, Min 1000 / Neutral 1500 / Max 2000; Deadbands: Roll/Pitch/Yaw 2, Assisted Control 8.
- GUI-Referenz Zielprogramm (UE4 Folie 3): Fenster "MainWindow", 4 Buttons links untereinander ("COMPort verbinden", "COMPort trennen", "FC initialisieren und Heartbeat", "Controller lesen"), rechts oben TextBox "COMXY" + Label "Verbindungsstatus", rechts unten Label "Armingstatus". Keine LEDs/GroupBoxes im UE4-Zielprogramm (LED-Bilder liegen der Vorlage trotzdem bei).

## 12. Semesterablauf WiSe 2026/27 (Interd_PE_LV_Planung_WS26_OPAL.pdf, Stand 12.10.2026 – "kann sich ändern, regelmäßig prüfen")

Videos (Flipped Classroom, laufend): VL3 Additive Fertigung, VL4 Elektrokonstruktion, VL5 Programmierung I (UAVTalk, Variablenkodierung), VL6 Programmierung II (Controller, Threads), UE2/UE3/UE4.

| Datum | Ereignis |
|---|---|
| Di 13.10.26 | UE1 Projekteinführung (ZEU 250); Gruppeneinschreibung ab 12:00 (bis 27.10.) |
| Di 20.10.26 | K1 Konsultation |
| Di 27.10.26 | UE2 + **Ausgabe Teile Quadrocopter** (Übergabeprotokoll!) |
| Di 03.11.26 / 10.11.26 | K3 / K4 Konsultation |
| **Di 17.11.26** | **Zwischenpräsentation (1)**, 3. DS, ZEU 250 |
| Di 24.11.26 | K5 |
| Di 01.12.26 | K6 Konsultation Fertigungsunterlagen |
| **08./09.12.26** | **Abgabe der Fertigungsunterlagen** (Termine per OPAL-Einschreibung) |
| Di 15.12.26 | K8; danach Weihnachtsferien |
| KW 52–53 (22.12.–03.01.) | Fertigung der Komponenten (Werkstatt) |
| Mo 04.01.27 | K9 |
| Di 12.01.27 | K10; **Montage des QC** beginnt |
| Di 19.01.27 | K11 |
| **Di 26.01.27** | **K12 Endkonfiguration (Präsenz, Schwoch/Saske)** → danach Throttle Curve etc. |
| **Di 02.02.27** | **Testflüge** (CAVE, Termin-Einschreibung im OPAL) |
| **Mi 10.02.27** | **Abgabe**: Beleg (gedruckt) + montierter QC + DVD mit allen Dateien |
| Prüfungszeitraum | Präsentation/Verteidigung der Ergebnisse |

Software-relevante Ableitung: Testumgebung + Steuerungsprogramm können ab UE4 (jetzt) entwickelt werden; reale Flüge erst nach Endkonfiguration (26.01.27) und Testflugtermin (02.02.27); DVD-Abgabe 10.02.27 → Visual-Studio-Projekt muss final, lauffähig und dokumentiert sein.

## 13. Beleg/Dokumentation – Software-Teil (Abgabedokumente_Interd_PE.pdf)

DVD (jeder Punkt eigener Ordner): ... **5. Visual-Studio-Projekt der Steuerungsapplikation** ...

Beleg (gedruckt), Abschnitt Software:
- Erläuterung des Programmaufbaus: Übersichtsdarstellung + Beschreibung (GUI, Programmablauf)
- **Steuerungskonzept und Controllerbelegung**
- **Programmablaufpläne** zur Darstellung der Programmlogik
- Klassendiagramm (nur falls eigene Klassen erstellt wurden)
- **Beschreibung der Sicherheitsfeatures**: Art der Features + Implementierung
- Formatierungsvorgaben der Professur VPE einhalten: tu-dresden.de/ing/maschinenwesen/imm/vpe/studium/informationen_zu_abschlussarbeiten
- Titelblatt-Vorlage: `Titelblatt_Beleg.docx` (Disziplin-Kürzel u. a. "Prog – Programmierung")

## 14. Umfeld / Sonstiges

- **Flugdaten der Elektrik:** `Vorlagen/Elektrik/Höhenverläufe/Hoehenverlauf_Gruppe_N.csv/xlsx` (Spalten: Zeit [s], Höhe [m]) – Messdaten vorjähriger Gruppen (Elektrik-Disziplin, Höhenverlaufsimulation).
- **Linkliste (OPAL):** LibrePilot Forum, LibrePilot Wiki.
- **Sicherheit (für Software-Verhalten mitdenken):** Propeller während Entwicklung/Konfiguration abmontieren (Motoren können sich ungeplant in Betrieb setzen!), ESC-Signaltöne nach Akku-Anschluss abwarten, Grenzspannung 3,5 V/Zelle am Spannungstester, QC-Flug nur im Käfig. → Software muss so gebaut sein, dass Rotoren NIE ungewollt laufen (PROG-011, PROG-009).
- **Vorlagen-Hinweis (OPAL-Knoten "Vorlagen"):** CAD-Modelle der Komponenten werden übers Semester aktualisiert – immer aktuellste Version verwenden (betrifft v. a. Konstruktion).
- **Aufzeichnungen** (videocampus.sachsen.de): VL3–VL6, UE2–UE4 (Links in den OPAL-Knoten).
- **Literatur C#:** Kühnel "Visual C# 2010"; Theis "Einstieg in Visual C# 2013"; Microsoft C#-Doku; csharpme.de; Vorgehensmodelle: Literaturübersicht_Vorgehensmodelle.pdf (Eigner, modellbasierte virtuelle Produktentwicklung u. a.).
- **NC-Spiegel-Pfade** (privater Mirror aller Kursdateien; lokale Kopie: `quellen/` im Projektordner): `courses/Interdisziplinäre Produktentwicklung/<Knoten>/...` – Vorlage_VS.zip in `95375272527266/Programmierung/`, ObjectID-xlsx in `95375272527061/UAVTalk und LibrePilot/`, FC-Anleitung in `95375272527061/Flightcontroller-Konfiguration/`, Quadrocopter_EXE.zip in `99323401808601/`.
