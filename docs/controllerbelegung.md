# Controllerbelegung (Steuerungskonzept, Mode-2)

> Gehört zum Beleg-Abschnitt „Steuerungskonzept und Controllerbelegung“ (C5 in
> `harte-kriterien.md`). Umsetzung im Code: `QuadroControl.Core/Controller/ControllerMapping.cs`
> (Mapping), im Controller-Thread beider `MainWindow.xaml.cs`. Eingabegerät ist
> der Xbox-360-Controller per USB (SharpDX.XInput, `UserIndex.One`) — Tastatur/
> Maus sind laut Aufgabenstellung kein Ersatz (B2).

## Belegungstabelle (Entscheidung D3, dokumentiert)

| Eingabe (Xbox-360-Controller) | UAVTalk-Feld (ManualControlCommand) | Wertebereich | Raw-Bereich (XInput) |
|---|---|---|---|
| **Linker Stick Y** (hoch/runter) | `Throttle` (Gas) und `Thrust` (= Throttle) | 0 … 1 | −32768 … 32767 |
| **Linker Stick X** (links/rechts) | `Yaw` (Gieren) | −1 … 1 | −32768 … 32767 |
| **Rechter Stick X** | `Roll` (Rollen) | −1 … 1 | −32768 … 32767 |
| **Rechter Stick Y** | `Pitch` (Nicken) | −1 … 1 | −32768 … 32767 |
| **Taste A** | Arming-Sequenz (E5) | — | `GamepadButtonFlags.A` |
| **Taste B** | Disarming-Sequenz (E5) | — | `GamepadButtonFlags.B` |

Dies ist die **Mode-2**-Belegung (Gas links, wie bei Flugmodell-Fernsteuerungen
üblich), dokumentiert als Entscheidung D3 und im GUI-Hinweis der GroupBox
„Controller-Ausgabe“ beider Programme angezeigt.

## Skalierung (exakt wie im Code)

Beide Formeln begrenzen zusätzlich auf den Zielbereich (`Clamp`):

- **Gas**: `(leftStickY − (−32768)) / (32767 − (−32768))` → 0…1
  (Stick unten = 0 % Gas, Stick oben = 100 % Gas)
- **Yaw/Roll/Pitch**: `Achswert / 32767` → −1…1
  (Mittellung ≈ 0)

Der aktuelle Rohwert jeder Achse wird im laufenden Betrieb in den vier
`TB_Analog*`-Feldern der GUI angezeigt (plus `TB_LeftTrigger`, Ausgabe 0–255).

## Arming / Disarming über die Tasten

- **Taste A (Arming)** — nur im Zustand `HeartbeatActive`, also nach
  abgeschlossenem Handshake: es werden **5×** ein `ManualControlCommand` mit
  `Throttle = −1` **und** `Thrust = −1` gesendet (Abstand 50 ms), danach ein
  `AccessoryDesired` mit `AccessoryVal = +1` (E5). Danach Zustand `Armed`,
  LED leuchtet grün (PROG-010A).
- **Taste B (Disarming)** — nur im Zustand `Armed`: ein `AccessoryDesired`
  mit `AccessoryVal = −1`. Zustand zurück auf `HeartbeatActive`.

Tastendruck wird im Feld „Tasten“ der Controller-Ausgabe angezeigt
(`A (Arming)` / `B (Disarming)`).

## Nicht belegte Eingaben

Trigger, Schultertasten, D-Pad und weitere Tasten werden **nicht** für die
Steuerung verwendet (`FlightModeSwitchPosition` bleibt 0, `Collective` 0,
`Channel[10]` 0). Sie zählen aber als „Eingabe aktiv“ für PROG-011: solange
irgendeine Eingabe anliegt, sendet die App die Stick-Werte; ohne jede Eingabe
sendet sie den neutralen Befehl (alle Werte 0), damit die Rotoren stillstehen.

## Totzone

Sticks unter **±2500** (Rohwert) gelten als „kein Eingang“ (`StickTotzone` im
Code-Behind) — das verhindert, dass ein leichter Stick-Versatz dauerhaft
Steuerwerte sendet, obwohl der Bediener die Hände vom Controller genommen hat.
