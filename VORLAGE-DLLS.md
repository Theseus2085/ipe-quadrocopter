# Kurs-DLLs und Bibliotheken (libs/) – woher kommen sie?

Die Dateien in `src/QuadroControl.*/libs/` sind **bewusst nicht im Git-Repo**
(siehe `.gitignore`: `libs/`, `*.dll`) — es sind kurs-eigene Binärdateien der
Professur, die nicht veröffentlicht werden dürfen.

## Benötigte Dateien pro App-Projekt

| Datei | Virtuell | Real | Quelle |
|---|---|---|---|
| `vKTCQuadroControl.dll` | ja | – | `Vorlage_VS.zip` → `Vorlage/bin/Debug/` |
| `KTCQuadroControl.dll` | – | ja | `Vorlage_VS.zip` → `Vorlage/bin/Debug/` |
| `CRC.dll` | ja | ja | `Vorlage_VS.zip` → `Vorlage/bin/Debug/` |
| `SerialPort.dll` | ja | ja | `Vorlage_VS.zip` → `Vorlage/bin/Debug/` |
| `LED_aus.png`, `LED_grn.png` | ja | ja | `Vorlage_VS.zip` → `Vorlage/bin/Debug/` |

## Beschaffung

`Vorlage_VS.zip` gibt es im OPAL-Kurs *Interdisziplinäre Produktentwicklung*
unter **Projekt/Beleg → Vorlagen → Programmierung**. Zip entpacken und die
obigen Dateien aus `Vorlage/bin/Debug/` in beide `libs/`-Ordner kopieren:

```
src/QuadroControl.Virtuell/libs/   ← vKTCQuadroControl.dll, CRC.dll, SerialPort.dll, LED_*.png
src/QuadroControl.Real/libs/       ← KTCQuadroControl.dll, CRC.dll, SerialPort.dll, LED_*.png
```

Ohne diese Dateien bauen die beiden WPF-Projekte in Visual Studio nicht
(die `.csproj`-Dateien referenzieren sie per `HintPath`).

`SharpDX.XInput` (Xbox-Controller) kommt dagegen automatisch per
NuGet-Paketreferenz — dafür muss nur einmalig Internet verfügbar sein.
