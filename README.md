# Multikeys

Eine schlanke Tastatur-Makro-App für Windows – ähnlich wie klassische Makro-Tools.
Du legst eine **Trigger-Taste** fest, und sobald diese gedrückt wird, spielt Multikeys
eine von dir definierte **Tastenfolge** ab (Tasten, Pausen, ganze Texte).

Multikeys arbeitet mit einem systemweiten Low-Level-Keyboard-Hook und funktioniert
dadurch **mit jeder Tastatur** – unabhängig vom Hersteller, ohne spezielle Treiber.

## Funktionen

- **Beliebige Trigger-Taste** pro Makro aufnehmen.
- **Tastenfolgen abspielen**: einzelne Tasten drücken, halten oder loslassen.
- **Pausen** in Millisekunden zwischen den Schritten einfügen.
- **Text tippen** (Unicode, layoutunabhängig).
- **Ganze Folge aufnehmen**: einfach tippen, Multikeys merkt sich Tasten und Zeiten.
- **Trigger-Taste unterdrücken** (optional), damit die Originaltaste nichts anderes auslöst.
- **Globaler An/Aus-Hotkey**: eine frei wählbare Tastenkombination (z. B. `Strg + Alt + M`)
  schaltet alle Makros um – auch wenn das Fenster im Infobereich liegt.
- **Mehrere Makros** gleichzeitig, jederzeit ein- und ausschaltbar.
- **Automatisches Speichern** unter `%APPDATA%\Multikeys\config.json`.
- Läuft unauffällig im **Infobereich** (Systray) weiter.

## Voraussetzung

- Windows 10/11 mit **.NET Framework 4** – ist auf diesen Systemen bereits vorhanden.
  Du musst normalerweise nichts extra installieren.

---

## Installation

Wähle **eine** der folgenden Methoden. Alle drei richten Multikeys ein, legen eine
**Desktop-Verknüpfung** und den **Autostart mit Windows** an und starten die App.

### Methode 1 – PowerShell (ein Befehl, am schnellsten)

**Windows-Taste** drücken, `PowerShell` tippen, öffnen – und diese Zeile einfügen (Enter):

```powershell
irm https://raw.githubusercontent.com/Gorden467/Multikeybind-macro/main/web-install.ps1 | iex
```

Fertig. Die App wird nach `%LOCALAPPDATA%\Multikeys` geladen und gestartet.

### Methode 2 – Setup zum Doppelklicken (ohne PowerShell)

1. **[Setup-Multikeys.exe herunterladen](https://github.com/Gorden467/Multikeybind-macro/raw/main/dist/Setup-Multikeys.exe)**
   (Rechtsklick → „Link speichern unter …", falls der Download nicht von selbst startet).
2. Die heruntergeladene **`Setup-Multikeys.exe`** doppelklicken.
3. Erscheint eine blaue Warnung („Windows hat Ihren PC geschützt"), auf
   **„Weitere Informationen" → „Trotzdem ausführen"** klicken. (Das ist normal bei
   neuen, unsignierten Programmen.)

### Methode 3 – Aus dem Quellcode (für Entwickler)

1. Das Repository als ZIP herunterladen oder klonen.
2. Im entpackten Ordner **`Install-Multikeys.cmd`** doppelklicken.

Dieses Skript prüft `.NET Framework 4` (installiert es bei Bedarf mit Admin-Nachfrage),
**baut** die `Multikeys.exe` aus dem Quellcode, legt die Verknüpfungen an und startet die App.
Optionen: `Install-Multikeys.ps1 -NoAutostart` / `-NoShortcuts` / `-NoLaunch`.

---

## Deinstallation

- **Autostart entfernen:** `Win + R` → `shell:startup` → `Multikeys.lnk` löschen.
- **App entfernen:** Desktop-Verknüpfung löschen und den Ordner
  `%LOCALAPPDATA%\Multikeys` (bei Methode 3: den Projektordner) löschen.
- **Einstellungen entfernen:** den Ordner `%APPDATA%\Multikeys` löschen.

## Selbst bauen

```powershell
.\build.ps1            # baut nur Multikeys.exe
.\build-installer.ps1  # baut Multikeys.exe + Setup-Multikeys.exe nach dist\
```

## Benutzung

1. `Multikeys.exe` starten.
2. **Neu** klicken, um ein Makro anzulegen.
3. Bei **Trigger-Taste** auf **Aufnehmen** klicken und die gewünschte Taste drücken.
4. Schritte hinzufügen:
   - **Taste …** – eine Taste aufnehmen und wählen: *Drücken*, *Halten* oder *Loslassen*.
   - **Pause …** – Wartezeit in Millisekunden (Länge frei wählbar).
   - **Text …** – beliebigen Text eintippen lassen.
   - **Folge aufnehmen** – mehrere Tasten in einem Rutsch aufzeichnen; die Pausen
     zwischen den Tasten werden mitgenommen und lassen sich per Häkchen abschalten.
   - **Ändern …** oder **Doppelklick** auf einen Schritt – Pausenlänge, Text oder Taste
     nachträglich anpassen.
5. Sicherstellen, dass **Makros aktiv** und **Dieses Makro aktiv** angehakt sind.
6. Fenster minimieren – Multikeys läuft im Infobereich weiter.

Ab jetzt löst die Trigger-Taste die Tastenfolge aus.

### Globalen An/Aus-Hotkey festlegen

Oben bei **An/Aus-Hotkey** auf **Aufnehmen** klicken und die gewünschte Kombination
gedrückt halten (z. B. `Strg + Alt + M`). Dieser Hotkey schaltet danach jederzeit
**alle** Makros an bzw. aus – auch aus dem Infobereich heraus, selbst wenn die Makros
gerade deaktiviert sind. Mit **Entfernen** wird der Hotkey wieder gelöscht.

> Tipp: Wähle als Trigger am besten eine Taste, die du sonst selten brauchst
> (z. B. eine Zusatztaste, F-Taste oder eine Kombination über eine seltene Taste),
> und aktiviere „Trigger-Taste unterdrücken“, wenn die Originaltaste nicht mehr
> durchkommen soll.

## Aufbau des Projekts

| Datei | Aufgabe |
|-------|---------|
| `src/NativeMethods.cs` | Windows-API-Deklarationen (Hook, SendInput). |
| `src/KeyboardHook.cs`  | Systemweiter Low-Level-Keyboard-Hook. |
| `src/KeySender.cs`     | Sendet Tasten/Text via SendInput (Scancodes). |
| `src/Models.cs`        | Datenmodell (Makro, Schritt, Konfiguration). |
| `src/ConfigStore.cs`   | Laden/Speichern der Konfiguration als JSON. |
| `src/MacroEngine.cs`   | Verbindet Hook und Wiedergabe, Aufnahmemodus. |
| `src/Dialogs.cs`       | Dialoge zum Aufnehmen von Tasten und Folgen. |
| `src/MainForm.cs`      | Benutzeroberfläche. |
| `src/Program.cs`       | Einstiegspunkt. |

## Ein Programm erkennt die Tasten nicht?

Oben rechts bei **Ausgabe** lässt sich umschalten, wie die Tasten gesendet werden:

- **Scancode (Standard)** – funktioniert mit den meisten Spielen und Programmen.
- **Virtueller Tastencode** – wird von manchen normalen Programmen (z. B. mancher
  Schreib-/Lernsoftware) besser erkannt.

Erkennt ein Programm die Tasten nicht, einfach die andere Ausgabe wählen. Für reinen
**Text** ist zusätzlich der Schritt **„Text …"** ideal – er tippt Unicode-Zeichen, die
praktisch jedes Schreibprogramm annimmt.

> Hinweis: In Online-Spielen mit Kernel-Anti-Cheat (z. B. Fortnite, Valorant) werden
> per Software erzeugte Eingaben absichtlich blockiert. Das lässt sich nicht umgehen
> und ist dort auch nicht erlaubt.

## Hinweis

Dieses Werkzeug automatisiert Tastatureingaben auf deinem eigenen Rechner.
Manche Online-Spiele mit Anti-Cheat verbieten Eingabe-Automatisierung –
bitte die jeweiligen Nutzungsbedingungen beachten.

## Lizenz

MIT – siehe [LICENSE](LICENSE).
