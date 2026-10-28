# Sicherheitsfeatures

> Beleg-Anforderung C8: „Beschreibung der Sicherheitsfeatures: Art der Features
> + Implementierung“. Jedes Feature ist hier mit seiner Anforderungs-ID aus
> `harte-kriterien.md`, der Implementierungsstelle und dem Verhalten beschrieben.
> Die Kernlogik liegt unit-testbar im Core (`QuadroControl.Core/Safety/SafetyMonitor.cs`,
> `QuadroControl.Core/Communication/CommStateMachine.cs`), die Verdrahtung im
> Controller-Thread beider `MainWindow.xaml.cs` (identisch in Programm 1 und 2).

## Übersicht

| # | Feature | Anforderung | Kernstelle |
|---|---|---|---|
| 1 | Zustandsmaschine sperrt Motorwerte vor dem Handshake | PROG-009, F3 | `CommStateMachine` |
| 2 | Controller-Verlust-Erkennung → Auto-Disarming | PROG-009A | `SafetyMonitor.Evaluate` |
| 3 | Neutralbefehl ohne Eingabe (Rotorstillstand) | PROG-011 | `SafetyMonitor` + `ManualControlCommandData.Neutral()` |
| 4 | Eindeutige Arming-Anzeige (LED + Text + Farbe) | PROG-010A | `MainWindow.xaml` (DataTrigger) |
| 5 | Arming nur über definierte Sequenz im sicheren Zustand | PROG-008A, E5, F3 | `ArmingSequenz()` |
| 6 | Sauberes Trennen / Fenster schließen | PROG-009, F3 | `VerbindungSauberTrennen()` |
| 7 | Arming-Zustand blockiert Steuerannahme (A/B-Tasten wirken nur passend) | PROG-009A, E5 | Controller-Thread |
| 8 | Sicherheitshinweis in der GUI (Programm 2) | F1 | Kopfzeile `MainWindow.xaml` (Real) |
| 9 | Kein „Kleben“ des letzten Steuerwerts | PROG-011 | Neutralbefehl je Abfragezyklus |

---

## 1. Kommunikations-Zustandsmaschine (PROG-009, F3)

**Art:** Motorwerte können grundsätzlich nicht vor einem abgeschlossenen
Handshake gesendet werden — strukturell, nicht per Zufall.

**Implementierung:** `CommStateMachine` mit den Zuständen
`Disconnected → PortOpen → Handshaking → HeartbeatActive → Armed`. Jeder
unzulässige Übergang wirft eine `InvalidOperationException` (z. B. „Arming nur
mit aktivem Heartbeat möglich“). Der Controller-Thread sendet nur, wenn
`CanSendControlCommands` (→ `HeartbeatActive` oder `Armed`) gilt.

**Verhalten:** Wird „FC initialisieren und Heartbeat“ vor „COMPort verbinden“
gedrückt, erscheint eine Fehlermeldung in der GUI — es geht kein Byte mit
Steueranteil an den FC. Auch Arming ohne Handshake ist unmöglich. Getestet in
`tests/.../CommStateMachineTests.cs`.

## 2. Controller-Verlust-Erkennung + automatisches Disarming (PROG-009A)

**Art:** Erkennung des Verbindungsverlusts des Eingabegeräts + definierte Reaktion.

**Implementierung:** Der Controller-Thread pollt in jedem Durchlauf (~20 ms)
`XBox.IsConnected`. Ist der Controller nicht mehr verbunden, liefert
`SafetyMonitor.Evaluate(controllerConnected, isArmed, anyInputActive)`
`ShallDisarm = isArmed` und `ShallSendNeutralCommand = true`.

**Verhalten:** Verliert das Programm im bewaffneten Zustand den Controller,
wird automatisch die Disarming-Sequenz (`AccessoryDesired = −1`) gesendet und
der neutrale Steuerbefehl (alle Werte 0, `Connected = 0`). Die GUI meldet
„Controller-Verlust erkannt: automatisch entwaffnet“, LED auf dunkel/„Disarmed“.
Zusätzlich sendet `Connected = 0` dem FC, dass keine aktive Steuerung anliegt.

## 3. Stillstand der Rotoren ohne Eingabe (PROG-011)

**Art:** „Stillstand der Rotoren, wenn keine Buttons/Trigger am Controller
betätigt sind“ — kein „letzter Wert klebt“.

**Implementierung:** `SafetyMonitor.Evaluate` liefert ohne Eingabe
`ShallSendNeutralCommand = true`; der Controller-Thread sendet dann in jedem
Zyklus `SafetyMonitor.NeutralCommand()` — ein `ManualControlCommand` mit
Throttle/Roll/Pitch/Yaw/Collective/Thrust = 0, `Channel[10]` = 0,
`Connected = 0`, `FlightModeSwitchPosition` = 0. Totzone: Stick-Auslenkungen
unter ±2500 Rohwerte gelten bereits als „keine Eingabe“.

**Verhalten:** Lässt der Bediener die Sticks los, kehrt der QC in Neutralstellung
zurück; nach dem Loslassen bleibt kein Gaswert stehen. Getestet in
`SafetyMonitorTests.cs` und `ManualControlCommandTests.cs` (Neutrallast: alle
Felder 0).

## 4. Eindeutige Arming-Kennzeichnung in der GUI (PROG-010A)

**Art:** Unmissverständliche Zustandsanzeige Armed/Disarmed.

**Implementierung:** Rechts unten der GUI sitzen (UE4-Folie 3: „Armingstatus“)
das Label `Label_ArmingStatus` mit dem Text `Armed`/`Disarmed`, die Schriftfarbe
(grün/rot) und die LED `Image_ArmingLED` mit den Vorlagen-Bildern
`LED_grn.png` (leuchtend) bzw. `LED_aus.png` (dunkel). Die Umschaltung erfolgt
rein XAML-seitig über `DataTrigger` auf dem Label-Inhalt — dieselbe Stelle, die
auch das Code-Behind setzt; es kann also keine abweichende Doppelbelegung
entstehen.

**Verhalten:** Bewaffnet = **leuchtende grüne LED + grüner Text „Armed“**.
Entwaffnet = **dunkle LED + roter Text „Disarmed“**. (Die Vorlage enthält kein
rotes LED-Bild; die Farbe Rot trägt die Schrift — siehe ABWEICHUNGEN.md #11.)

## 5. Arming nur über definierte Sequenz (PROG-008A, E5, F3)

**Art:** Kein unbeabsichtigtes Arming (F3), Arming über GUI/Controller bedienbar (PROG-008A).

**Implementierung:** `ArmingSequenz()` läuft nur im Zustand `HeartbeatActive`
(sichert die Zustandsmaschine zusätzlich ab): 5× `ManualControlCommand` mit
`Throttle = −1` **und** `Thrust = −1` (Abstand 50 ms), danach
`AccessoryDesired = +1`. Disarming: `AccessoryDesired = −1` — nur im Zustand
`Armed`.

**Verhalten:** Arming kann nur durch den ausdrücklichen Tastendruck A **nach**
erfolgreichem Handshake erfolgen. Ein einzelner Zufalls-Stickstoß kann die
Sequenz nicht auslösen (Taste A ist ein Button-Flag, kein Stickwert), und die
Mehrfach-Schreibung folgt exakt der Vorgabe der VL5.

## 6. Sauberes Trennen und Fenster schließen (PROG-009, F3)

**Implementierung:** `VerbindungSauberTrennen()` (Button „COMPort trennen“ und
`OnClosing`): Threads anhalten → falls bewaffnet: Disarming-Sequenz →
neutraler Steuerbefehl → `VerbindungTrennen()` → Zustandsmaschine auf
`Disconnected`.

**Verhalten:** Auch beim einfachen Schließen des Fensters gehen dem FC niemals
„klebende“ Gaswerte hinterher; die Rotoren stehen definiert still, bevor die
Schnittstelle geschlossen wird.

## 7. Tastenlogik passend zum Zustand (PROG-009A, E5)

**Implementierung:** Taste A wirkt nur, wenn `!_commState.IsArmed`, Taste B nur,
wenn `_commState.IsArmed` (Zusatzsicherung zur Zustandsmaschine).

**Verhalten:** Doppeltes Arming oder Disarmen ohne Wirkung — die Zustände können
nicht „kippen“.

## 8. Sicherheitshinweis in der GUI (F1)

**Implementierung:** Kopfzeile von Programm 2 (`QuadroControl.Real`) zeigt
dauerhaft: „ACHTUNG: Am realen Quadrocopter nur propellerlos testen …“.

**Verhalten:** Erinnert den Bediener vor jedem Start an die Regel, dass Software-
Tests am realen QC nur ohne Propeller erfolgen („die Motoren können sich
ungeplant in Betrieb setzen“); erst der Flug selbst erfolgt mit Propellern.

## 9. Nicht blockierende Fehlkanäle (PROG-011, E2)

**Implementierung:** `ManualControlCommandData` schreibt **immer alle 9 Felder**
in fester Reihenfolge; nicht benutzte Felder (`Collective`, `Channel[10]`,
`FlightModeSwitchPosition`) werden auf 0 gesetzt statt weggelassen, `Thrust`
wird in `ToPayload()` zwangsweise mit `Throttle` synchronisiert.

**Verhalten:** Der FC erhält nie Teilsätze mit Alt-Werten — es kann kein Feld
„stehen bleiben“, das einen alten Steuerzustand repräsentiert.
