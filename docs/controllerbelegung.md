# Controllerbelegung (Steuerungskonzept, Entscheidung D3v2)

> Gehört zum Beleg-Abschnitt „Steuerungskonzept und Controllerbelegung“ (C5 in
> `harte-kriterien.md`). Umsetzung im Code: `QuadroControl.Core/Controller/ControllerMapping.cs`
> (Mapping), im Controller-Thread beider `MainWindow.xaml.cs`. Eingabegerät ist
> der Xbox-360-Controller per USB (SharpDX.XInput, `UserIndex.One`) — Tastatur/
> Maus sind laut Aufgabenstellung kein Ersatz (B2).
>
> Die Zuordnung der Achsen ist eine **eigene, dokumentierte Entscheidung** (D3v2,
> 08.11.2026, Simon). Das Lastenheft schreibt nur Eingabegerät (B2), die
> UAVTalk-Wertebereiche (E2) und die Tasten A/B für Arming/Disarming (UE4) vor;
> welche physische Eingabe welches Feld steuert, ist frei wählbar und muss nur
> dokumentiert werden (C5). Vorgängerversion D3 war eine Mode-2-Belegung
> (Gas links), siehe ABWEICHUNGEN.md #19.

## Belegungstabelle (Entscheidung D3v2, dokumentiert)

| Eingabe (Xbox-360-Controller) | UAVTalk-Feld (ManualControlCommand) | Wertebereich | Raw-Bereich (XInput) |
|---|---|---|---|
| **R2 (rechter Trigger)** | `Throttle` (Gas) und `Thrust` (= Throttle) | 0 … 1 | 0 … 255 (byte) |
| **Linker Stick X** (links/rechts) | `Yaw` (Gieren) | −1 … 1 | −32768 … 32767 |
| **Rechter Stick X** | `Roll` (Rollen) | −1 … 1 | −32768 … 32767 |
| **Rechter Stick Y** | `Pitch` (Nicken) | −1 … 1 | −32768 … 32767 |
| **Taste A** | Arming-Sequenz (E5) | — | `GamepadButtonFlags.A` |
| **Taste B** | Disarming-Sequenz (E5) | — | `GamepadButtonFlags.B` |
| Linker Stick Y | nicht belegt (nur Anzeige) | — | −32768 … 32767 |

Begründung: Gas und Lage sind auf getrennte Finger/Daumen verteilt (rechter
Zeigefinger Höhe, rechter Daumen Lage, linker Daumen Gieren); Trigger loslassen
= 0 Gas ist die gefahrlose Ruhelage.

## Skalierung (exakt wie im Code)

Beide Formeln begrenzen zusätzlich auf den Zielbereich (`Clamp`):

- **Gas**: Werte bis `TriggerTotzone` (10) → 0; darüber
  `(R2 − 10) / (255 − 10)` → 0…1 (Trigger nicht gezogen = 0 % Gas,
  voll gezogen = 100 % Gas, stetig ohne Sprung)
- **Yaw/Roll/Pitch**: `Achswert / 32767` → −1…1 (Mittelstellung ≈ 0)

Der aktuelle Rohwert jeder Achse und des rechten Triggers wird im laufenden
Betrieb in den `TB_Analog*`- und `TB_RightTrigger`-Feldern der GUI angezeigt
(Trigger-Ausgabe 0–255); Programm 2 zeigt zusätzlich den Gaswert als DLL-Hex
(`TB_DLLHex`).

## Arming / Disarming über die Tasten

- **Taste A (Arming)** — nur im Zustand `HeartbeatActive`, also nach
  abgeschlossenem Handshake: es werden **55×** ein `ManualControlCommand` mit
  `Throttle = −1` **und** `Thrust = −1` gesendet (Abstand 50 ms), danach ein
  `AccessoryDesired` mit `AccessoryVal = +1` und **ein abschließender weiterer
  `ManualControlCommand` mit `Throttle = −1`** (E5; DLL-Kontrakt 03.11.2026
  dekompiliert + live bestätigt, ABWEICHUNGEN.md #17 — die DLL puffert die
  letzten 50 Throttle-Werte und aktiviert Arming nur bei Summe −50).
  Danach Zustand `Armed`, LED leuchtet grün (PROG-010A).
- **Taste B (Disarming)** — nur im Zustand `Armed`: ein `AccessoryDesired`
  mit `AccessoryVal = −1`. Zustand zurück auf `HeartbeatActive`.

Die Arming-Sequenz sendet ihre Throttle-Werte selbst; sie ist unabhängig davon,
wie der Trigger gerade steht.

Tastendruck wird im Feld „Tasten“ der Controller-Ausgabe angezeigt
(`A (Arming)` / `B (Disarming)`).

## Nicht belegte Eingaben

Linker Stick Y, linker Trigger, Schultertasten, D-Pad und weitere Tasten werden
**nicht** für die Steuerung verwendet (`FlightModeSwitchPosition` bleibt 0,
`Collective` 0, `Channel[10]` 0). Sie zählen aber als „Eingabe aktiv“ für
PROG-011: solange irgendeine Eingabe anliegt, sendet die App die Steuerwerte;
ohne jede Eingabe sendet sie den neutralen Befehl (alle Werte 0), damit die
Rotoren stillstehen.

## Totzonen

- Sticks unter **±2500** (Rohwert) gelten als „kein Eingang“ (`StickTotzone` im
  Code-Behind) — das verhindert, dass ein leichter Stick-Versatz dauerhaft
  Steuerwerte sendet, obwohl der Bediener die Hände vom Controller genommen hat.
- Rechter Trigger unter **11** (Rohwert 0–10, `TriggerTotzone` in
  `ControllerMapping`) ergibt exakt 0 Gas — Ruhe-Rauschen des Triggers löst
  keinen Motorlauf aus.
