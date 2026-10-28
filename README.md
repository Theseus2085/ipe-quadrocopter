# IPE-Quadrocopter — Steuerungsapplikation (C#/WPF)

Steuerungssoftware für den Quadrocopter der Lehrveranstaltung **Interdisziplinäre
Produktentwicklung** (Professur für Virtuelle Produktentwicklung, TU Dresden):
zwei WPF-Programme steuern den Flugcontroller (CC3D *Revolution*, LibrePilot)
per **UAVTalk** über die vom Lehrstuhl gestellte Funktionsbibliothek — einmal
gegen die **virtuelle Testumgebung** (Programm 1) und einmal gegen den **realen
Quadrocopter** (Programm 2). Die Eingabe erfolgt über einen Xbox-360-Controller
(SharpDX.XInput, Mode-2).

> **Ehrlichkeitshinweis:** Der Logikkern und alle Unit-Tests wurden auf einer
> Linux-VM entwickelt und geprüft (`dotnet test`: **79/79 grün**). Die beiden
> **WPF-Anwendungen können nur unter Windows mit Visual Studio kompiliert und
> getestet werden** (WPF/.NET Framework 4.5.2). Was dazu auf einem
> Windows-PC zu prüfen ist, steht als nummerierte Checkliste in
> **[docs/windows-testplan.md](docs/windows-testplan.md)**.

## Projektaufbau

```
IPE-Quadrocopter.sln
├── src/
│   ├── QuadroControl.Core/          Logikkern: UAVTalk-Protokoll, Zustandsmaschine,
│   │                                Controller-Mapping, Sicherheitslogik (keine DLL-/GUI-Abhängigkeit)
│   ├── QuadroControl.Virtuell/      Programm 1: virtuelle Testumgebung (vKTCQuadroControl.dll)
│   └── QuadroControl.Real/          Programm 2: realer Quadrocopter (KTCQuadroControl.dll)
├── tests/
│   └── QuadroControl.Core.Tests/    79 xunit-Tests (Protokoll-Vektoren, Sequenzen, Sicherheit)
├── docs/                            Dokumentation (Ablaufpläne, Klassen, Sicherheit, Controller, Windows-Test)
├── DOKUMENTATION/
│   └── erklaerung.html              selbsterklärende Programmdokumentation (eine Datei, offline öffnen)
└── VORLAGE-DLLS.md                  woher die Kurs-DLLs/LED-Bilder kommen (nicht im Repo!)
```

| Projekt | Typ | Ziel-Framework | Baut auf |
|---|---|---|---|
| QuadroControl.Core | Klassenbibliothek | netstandard2.0 **+ net452** | Linux **und** Windows |
| QuadroControl.Virtuell | WPF-Anwendung | .NET Framework 4.5.2 | nur Windows (VS2019) |
| QuadroControl.Real | WPF-Anwendung | .NET Framework 4.5.2 | nur Windows (VS2019) |
| QuadroControl.Core.Tests | xunit-Tests | net8.0 | Linux **und** Windows |

Der Kern (`QuadroControl.Core`) enthält Protokoll-Erzeugung (UAVTalk-Header,
Payloads, Handshake, Heartbeat), die Kommunikations-Zustandsmaschine
(`Disconnected → PortOpen → Handshaking → HeartbeatActive → Armed`), das
Controller-Mapping und die Sicherheitslogik — bewusst **ohne** Referenz auf die
Kurs-DLLs oder WPF. Die beiden WPF-Apps kapseln jeweils ihre Kurs-DLL in einem
kleinen Transport-Adapter (`VirtuellQuadroTransport` / `RealQuadroTransport`)
und enthalten die Threads (Controller ~20 ms, Heartbeat 1 s) sowie die GUI.

## Voraussetzungen (Windows-PC)

1. **Visual Studio 2019** mit Workload *.NET-Desktopentwicklung*
   (VS 2017 geht auch; die Projekte sind old-style `.csproj` für .NET 4.5.2).
2. **Kurs-DLLs**: `Vorlage_VS.zip` aus dem OPAL-Kurs entpacken und die Dateien
   aus `Vorlage/bin/Debug/` in die beiden `libs/`-Ordner kopieren:
   - `src/QuadroControl.Virtuell/libs/` ← `vKTCQuadroControl.dll`, `CRC.dll`, `SerialPort.dll`, `LED_aus.png`, `LED_grn.png`
   - `src/QuadroControl.Real/libs/` ← `KTCQuadroControl.dll`, `CRC.dll`, `SerialPort.dll`, `LED_aus.png`, `LED_grn.png`

   Ohne diese Dateien bauen die WPF-Projekte **nicht** (Details: [VORLAGE-DLLS.md](VORLAGE-DLLS.md)).
   Die LED-Bilder werden für die Arming-Anzeige (PROG-010A) als Resource eingebunden.
3. **NuGet**: `SharpDX.XInput 4.2.0` wird beim ersten Build automatisch
   wiederhergestellt (dafür einmalig Internet nötig; in VS: *Build →
   Projektmappe erstellen* löst den Restore aus).
4. Xbox-360-Controller per USB (für Programmstart ohne Controller reicht
   ebenfalls — die App zeigt dann „Controller-Verlust“/neutrale Werte).

## Bauen und Starten

1. `IPE-Quadrocopter.sln` in Visual Studio 2019 öffnen.
2. NuGet-Restore läuft automatisch (sonst: Rechtsklick auf die Projektmappe →
   *NuGet-Pakete wiederherstellen*).
3. **Programm 1 zuerst** (E7: „Testumgebung zuerst“): Startprojekt
   `QuadroControl.Virtuell` setzen und starten (F5).
4. Danach `QuadroControl.Real` für den realen Quadrocopter — **nur propellerlos
   testen** (Sicherheitshinweis F1), erst beim Flug mit Propellern.

### Unit-Tests (auch ohne Windows ausführbar)

```bash
dotnet test tests/QuadroControl.Core.Tests/QuadroControl.Core.Tests.csproj
```

Erwartet: **79 passed / 0 failed** (bekannte Protokoll-Vektoren wie
`1.0f → 00 00 80 3F`, Feldreihenfolge ManualControlCommand, exakte
5-Nachrichten-Handshake-Sequenz, Zustandsmaschinen-Übergänge, Sicherheitslogik).

## Testablauf mit der virtuellen Umgebung (+ Quadrocopter.exe)

1. **Quadrocopter.exe** (Unity-Demonstrator aus dem OPAL-Kurs,
   `Quadrocopter_EXE.zip`) starten — Netzwerkzugriff **nicht** erlauben
   (Abbrechen) — und eine Szene wählen (I Ebene, II Sporthalle, III Wiese).
2. Eigenes Programm `QuadroControl.Virtuell` starten.
3. In der GUI (Aufbau wie UE4-Folie 3 „Zielprogramm für Übung 4“):
   1. COM-Port eintragen (z. B. `COM3`) → **COMPort verbinden**.
   2. **FC initialisieren und Heartbeat** → sendet die 5 Handshake-Nachrichten
      und startet den Heartbeat (1 Nachricht/s, eigener Thread).
   3. **Controller lesen** → startet die Controller-Abfrage (~20 ms Takt).
   4. Taste **A** = Arming (LED leuchtet grün + „Armed“), Taste **B** = Disarming.
   5. Fliegen: linker Stick Y = Gas, linker Stick X = Gieren, rechter Stick
      X = Rollen, rechter Stick Y = Nicken (Mode-2).
4. Beenden: erst Disarmen, dann **COMPort trennen** (die App sendet vorher
   automatisch einen neutralen Steuerbefehl, damit die Rotoren stillstehen).

Roter/grüner Rahmen in `Quadrocopter.exe` = Disarmed/Armed. Details und
Fehlersuche: [docs/windows-testplan.md](docs/windows-testplan.md).

## Dokumentation

| Datei | Inhalt |
|---|---|
| `DOKUMENTATION/erklaerung.html` | Gesamterklärung des Programms (Architektur, UAVTalk, Ablauf, GUI, Threads, Sicherheit) — offline als eine Datei öffnen |
| `docs/programmablaufplan.md` | Programmablaufpläne (Verbindung, Arming, Controller-Schleife, Sicherheit) |
| `docs/klassendiagramm.md` | Klassendiagramm (Core + Adapter der beiden Apps) |
| `docs/sicherheitsfeatures.md` | Sicherheitsfeatures mit Anforderungs-IDs (PROG-009/009A/010A/011 …) |
| `docs/controllerbelegung.md` | Controller-Belegung Mode-2 (Steuerungskonzept C5) |
| `docs/windows-testplan.md` | Nummerierte Prüfliste für den Windows-PC (Build, virtueller Test, realer Test propellerlos) |

## Offene Punkte für den Windows-PC (Zusammenfassung)

- WPF-Build beider Apps in VS2019 (unter Linux nicht kompilierbar — daher
  hier nicht ausgeführt worden).
- Verhalten der Kurs-DLLs (`SchickeHexString`, CRC-8-Anhang) und der
  Testumgebung kann nur dort verifiziert werden.
- Realer FC: nur nach Kurs-Konfiguration (LibrePilot, Throttle Curve max. 0,3
  bei 100 % Input — PROG-004A) und **propellerlos** (F1) testen.
