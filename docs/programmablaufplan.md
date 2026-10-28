# Programmablaufpläne (PAP)

> Beleg-Anforderung C6: „Programmablaufpläne zur Darstellung der Programmlogik“.
> Die Pläne beschreiben den **tatsächlich implementierten** Ablauf beider
> Programme (`MainWindow.xaml.cs` von Virtuell und Real; der Logikkern liegt in
> `QuadroControl.Core`). Darstellung als Mermaid-Diagramme (in GitHub/VS Code
> direkt gerendert).

## 1. Verbindungs- und Kommunikationsablauf (Gesamtablauf)

Entspricht den 5 Schritten der VL5: verbinden → Handshake → Heartbeat →
steuern/armen → trennen.

```mermaid
flowchart TD
    S(["Programmstart — Zustand: Disconnected"]) --> A["COM-Port in TB_COMPort eintragen<br/>(Real: Button COM-Ports suchen / HoleAktuelleCOMPorts)"]
    A --> B["Button: COMPort verbinden"]
    B --> C{"Verbinde() erfolgreich?<br/>(Zustandsmaschine: Disconnected → PortOpen)"}
    C -- "nein" --> C1["Fehlermeldung im Verbindungsstatus<br/>Zustand bleibt Disconnected"] --> A
    C -- "ja" --> D["Button: FC initialisieren und Heartbeat"]
    D --> E["Handshake: genau 5 Nachrichten senden<br/>1) GCSTelemetryStats Status 00<br/>2) GCSTelemetryStats Status 01<br/>3) GCSTelemetryStats Status 03<br/>4) Metadaten ManualControlCommand (VL5 wortwörtlich)<br/>5) Metadaten AccessoryDesired (VL5 wortwörtlich)"]
    E --> F{"Handshake komplett?"}
    F -- "ja" --> G["Heartbeat-Thread starten<br/>GCSTelemetryStats Status 03, jede Sekunde<br/>Zustand: HeartbeatActive"]
    F -- "fehler" --> F1["Meldung im Verbindungsstatus"] --> D
    G --> H["Button: Controller lesen<br/>Controller-Thread starten (Takt ca. 20 ms)"]
    H --> I["Steuerung: Sticks und Tasten<br/>siehe PAP 3 (Controller-Zyklus)"]
    I --> J{"Taste A gedrückt?"}
    J -- "ja" --> K["Arming-Sequenz (PAP 2)<br/>Zustand: Armed, LED grün"]
    K --> L["Flugsteuerung"]
    J -- "nein" --> L
    L --> M{"Taste B gedrückt?"}
    M -- "ja" --> N["Disarming: AccessoryDesired = −1<br/>Zustand: HeartbeatActive, LED aus"]
    M -- "nein" --> L
    N --> L
    L --> O["Button: COMPort trennen<br/>(oder Fenster schließen)"]
    O --> P["Sauberes Trennen (PAP 4):<br/>ggf. disarmen, Neutralbefehl senden,<br/>Threads stoppen, Verbindung schließen"]
    P --> Z(["Zustand: Disconnected — Ende"])
```

## 2. Arming-Sequenz (Taste A, E5)

```mermaid
flowchart TD
    S(["Taste A im Controller-Zyklus erkannt"]) --> Q{"Zustand = HeartbeatActive<br/>(Handshake fertig, noch nicht bewaffnet)?"}
    Q -- "nein" --> X["Ignoriert — Zustandsmaschine<br/>lässt kein vorzeitiges Arming zu"] --> E(["Ende"])
    Q -- "ja" --> L1["ManualControlCommand senden:<br/>Throttle = −1 UND Thrust = −1<br/>(Rest neutrale Werte)"]
    L1 --> W["50 ms warten"]
    W --> C2{"5 Wiederholungen<br/>gesendet?"}
    C2 -- "nein" --> L1
    C2 -- "ja" --> A1["AccessoryDesired senden:<br/>AccessoryVal = +1"]
    A1 --> ST["Zustandsmaschine: HeartbeatActive → Armed"]
    ST --> GUI["GUI: LED leuchtet grün, Text „Armed“<br/>(PROG-010A)"]
    GUI --> E2(["Ende — QC ist bewaffnet"])
```

## 3. Controller-Thread — ein Zyklus (ca. 20 ms, VL6-Muster)

```mermaid
flowchart TD
    S(["Zyklus beginnt"]) --> C{"XBox.IsConnected?"}
    C -- "ja" --> R["State = XBox.GetState() lesen"]
    C -- "nein" --> EV
    R --> EV["SafetyMonitor.Evaluate(<br/>controllerConnected, isArmed, eingabeAktiv)"]
    EV --> L{"Controller verloren<br/>und bewaffnet?"}
    L -- "ja" --> D1["Disarming-Sequenz<br/>(AccessoryDesired = −1)<br/>Meldung in der GUI"]
    L -- "nein" --> E1
    D1 --> E1{"Eingabe aktiv?<br/>(Stick über Totzone ±2500,<br/>Trigger oder Taste)"}
    E1 -- "nein" --> N1["Neutralbefehl bauen:<br/>alle Werte 0, Connected = 0<br/>(PROG-011: Rotoren stehen still)"]
    E1 -- "ja" --> M1["ControllerMapping:<br/>links Y → Throttle/Thrust 0…1<br/>links X → Yaw, rechts X → Roll,<br/>rechts Y → Pitch (−1…1)<br/>Taste A/B: Arming/Disarming je Zustand"]
    N1 --> G1
    M1 --> G1{"Zustandsmaschine erlaubt Senden?<br/>(HeartbeatActive oder Armed)"}
    G1 -- "nein" --> A2["Nichts senden —<br/>keine Motorwerte vor dem Handshake"]
    G1 -- "ja" --> S1["ManualControlCommand senden<br/>(alle 9 Felder, feste Reihenfolge;<br/>Checksumme hängt die Kurs-DLL an)"]
    A2 --> GUI
    S1 --> GUI["GUI-Werte über Dispatcher + Delegat<br/>aktualisieren (E6): Achsen, Tasten,<br/>Real zusätzlich TB_DLLHex"]
    GUI --> W["20 ms warten"]
    W --> S
```

## 4. Sicherheitsreaktionen (Trennen / Controller-Verlust)

```mermaid
flowchart TD
    T1["Auslöser 1: Button COMPort trennen"] --> M
    T2["Auslöser 2: Fenster wird geschlossen<br/>(OnClosing)"] --> M
    T3["Auslöser 3: Controller-Verlust<br/>im bewaffneten Zustand (PROG-009A)"] --> M
    M{"Ist der QC bewaffnet?<br/>(Zustand Armed)"} -- "ja" --> D["Disarming-Sequenz:<br/>AccessoryDesired = −1<br/>Zustand Armed → HeartbeatActive"]
    M -- "nein" --> N
    D --> N["Neutraler ManualControlCommand:<br/>alle Werte 0 (Rotoren stehen still)"]
    N --> TH["Threads anhalten<br/>(Heartbeat, Controller — bool-Flag statt Abort)"]
    TH --> V["VerbindungTrennen() der Kurs-DLL"]
    V --> C1["Zustandsmaschine: → Disconnected"]
    C1 --> GUI["GUI: LED dunkel, Text „Disarmed“,<br/>Verbindungsstatus aktualisiert"]
    GUI --> E(["Ende — definierter sicherer Zustand"])
```

## Hinweise zu den Plänen

- Die **Checksumme (CRC-8)** erscheint in keinem PAP als eigener Schritt, weil
  die App sie niemals berechnet — `SchickeHexString()` der Kurs-DLL hängt sie
  automatisch an (Vertragsregel, wissen §5 Feld G).
- Die Zustandsmaschine (`Disconnected → PortOpen → Handshaking →
  HeartbeatActive → Armed`) ist die strukturelle Sicherheitssperre: Jeder
  unzulässige Übergang wirft eine Ausnahme, statt still Steuerwerte zu senden.
- GUI-Zugriffe aus den Nebenthreads laufen ausschließlich über
  `Dispatcher.CheckAccess()` + `BeginInvoke` mit dem Delegaten `UpdateGUI`
  (WPF-Regel E6 aus VL6).
