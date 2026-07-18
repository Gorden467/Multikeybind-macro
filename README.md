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

## Voraussetzungen

- Windows mit **.NET Framework 4** (auf Windows 10/11 vorinstalliert).
- Kein zusätzliches SDK nötig – gebaut wird mit dem in Windows enthaltenen C#-Compiler.

## Einrichtung (empfohlen)

Doppelklick auf **`Install-Multikeys.cmd`**. Das Einrichtungs-Skript

1. prüft, ob **.NET Framework 4** vorhanden ist, und installiert es bei Bedarf
   automatisch (fragt dann nach Administratorrechten),
2. baut `Multikeys.exe`, falls sie noch nicht existiert,
3. legt eine **Desktop-Verknüpfung** und eine **Autostart-Verknüpfung** an,
4. startet Multikeys.

> Warum ein Starter und keine Prüfung in der App selbst? Die `.exe` braucht
> .NET Framework 4 schon zum Starten – deshalb übernimmt das Skript die Prüfung
> und Installation *vor* dem Programmstart.

Optionen: `Install-Multikeys.ps1 -NoAutostart` (kein Autostart),
`-NoShortcuts` (keine Verknüpfungen), `-NoLaunch` (nicht starten).

## Bauen

```powershell
.\build.ps1
```

Danach liegt `Multikeys.exe` im Projektordner. Alternativ die fertige `Multikeys.exe`
direkt starten.

## Benutzung

1. `Multikeys.exe` starten.
2. **Neu** klicken, um ein Makro anzulegen.
3. Bei **Trigger-Taste** auf **Aufnehmen** klicken und die gewünschte Taste drücken.
4. Schritte hinzufügen:
   - **Taste …** – eine Taste aufnehmen und wählen: *Drücken*, *Halten* oder *Loslassen*.
   - **Pause …** – Wartezeit in Millisekunden.
   - **Text …** – beliebigen Text eintippen lassen.
   - **Folge aufnehmen** – mehrere Tasten samt Pausen in einem Rutsch aufzeichnen.
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

## Hinweis

Dieses Werkzeug automatisiert Tastatureingaben auf deinem eigenen Rechner.
Manche Online-Spiele mit Anti-Cheat verbieten Eingabe-Automatisierung –
bitte die jeweiligen Nutzungsbedingungen beachten.

## Lizenz

MIT – siehe [LICENSE](LICENSE).
