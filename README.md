# Minecraft Server Manager

**Paper und Velocity lokal steuern — ohne Terminal-Marathon.**

Ein schlankes Desktop-Programm für Windows: Du siehst alle Server-Instanzen auf einen Blick, startest und stopst sie per Klick, liest die Konsole mit und pflegst `server.properties` direkt in der Oberfläche. Downloads kommen von der offiziellen **PaperMC-API** — keine dubiosen JARs aus irgendwelchen Ordnern.

---

## Warum das existiert

Minecraft-Server zu betreiben heißt oft: Java-Pfad, Ports, EULA, RCON, Proxy-Forwarding — und irgendwann hast du fünf Terminalfenster und vergisst, welcher Prozess zu welchem Ordner gehört. Diese App bündelt den Alltag: **ein Fenster**, klare Statusanzeigen, **Quick-Setup** für neue Paper-Server und ein **Cluster-Wizard**, der Velocity plus Paper-Backends mit sinnvollem Forwarding aufsetzt.

---

## Das kann die App

| Bereich | Was du damit machst |
|--------|----------------------|
| **Dashboard & Sidebar** | Alle registrierten Instanzen und Cluster-Gruppen — mit einem Klick in die Details. |
| **Quick-Setup** | Neuen **Paper**-Server anlegen: Version wählen (live von der API), JAR laden, Port, Weltname, EULA, optional **RCON** — fertig. |
| **Cluster-Wizard** | **Velocity**-Proxy plus zwei **Paper**-Backends mit gemeinsamem Forwarding-Setup — ideal für kleine Netzwerke ohne Copy-Paste-Chaos. |
| **Konsole** | Server-Log der gewählten Instanz; Befehle optional direkt eintippen. |
| **RCON** | Passwort und Port aus der Instanz — Test der Verbindung möglich. |
| **Konfiguration** | `server.properties` im Editor bearbeiten; Schnellinfos zu Bind-Adresse und „Quick Connect“-Text fürs Clipboard. |
| **Automation** | Optional Skripte **vor Start** und **nach Stopp** pro Server (z. B. Backups, Hooks). |
| **Presets** | Schnellbefehle (z. B. `save-all`, Whitelist) — abhängig vom Loader (Paper/Velocity), aus `command-presets.json`. |
| **Erweiterungen** | Hinweise zu Plugin-/Mods-Ordnern; Unterstützung verschiedener Loader-Typen in den Metadaten (u. a. Vanilla, Purpur, Fabric, Forge — Verwaltung individuell pro Instanz). |
| **Globale Einstellungen** | Bevorzugtes **Java**, **Artifact-Cache**, **Server-Stammordner**, Standard-**JVM-Argumente**, **Dark Theme**. |
| **Not-Aus** | **Alle stoppen** — beendet alle vom Manager überwachten Server-Prozesse. |

> **Hinweis:** Öffentlich erreichbare Server gehören **nur** mit Absicht und Absicherung ins Internet. Beim Proxy-Setup: typischerweise nur den **Velocity-Port** nach außen öffnen, Backends bleiben intern.

---

## Voraussetzungen

- **Windows** (die Oberfläche ist als Desktop-App für dieses Ziel ausgelegt).
- Zum **Selbstbauen**: [.NET SDK](https://dotnet.microsoft.com/download) passend zum Projekt (**net10.0** — siehe `MinecraftServerManager.csproj`).
- Zum **Ausführen** eines *framework-dependent* Builds: passende **.NET-Runtime** auf dem Rechner (gleiche Hauptversion wie das Target Framework).
- **Java**: für Paper/Velocity/Java-Server — entweder im **PATH** oder als expliziter Pfad in den Einstellungen bzw. pro Instanz.

---

## Schnellstart (Entwicklung)

Im Projektroot:

```bat
build.bat Release
```

Die EXE liegt anschließend unter:

`src\MinecraftServerManager\bin\Release\net10.0\`

Ohne Argument baut `build.bat` standardmäßig **Release**. Für einen Debug-Build:

```bat
build.bat Debug
```

**Release-ZIP** (Publish für `win-x64`, framework-dependent, danach ZIP nach `dist\`):

```bat
release.bat
```

Ergebnis: `dist\MinecraftServerManager-<Version>-<Datum>.zip` — praktisch zum Verteilen.

---

## Bedienung in drei Minuten

1. **App starten** — beim ersten Start werden Standardpfade für Server und Artefakt-Cache angelegt (anpassbar unter *Einstellungen*).
2. **Quick-Setup** — neuen Paper-Server erstellen, EULA bestätigen, Port setzen, Download abwarten.
3. **Instanz auswählen** — Start/Stop, Konsole lesen, Properties ändern, RCON testen.
4. Optional **Cluster-Wizard** — Velocity + Backends für ein kleines Netzwerk mit einem durchgängigen Setup.

---

## Wo liegen die Daten?

| Was | Ort (Standard) |
|-----|----------------|
| **App-Zustand** (Instanzen, Cluster, Einstellungen) | `%LocalAppData%\MinecraftServerManager\state.json` |
| **Artifact-Cache** (geladene JARs) | `%LocalAppData%\MinecraftServerManager\artifacts` |
| **Server-Stammordner** | `%USERPROFILE%\Documents\MinecraftServerManager\servers` |

Pfade kannst du in der App überschreiben — die JSON-Datei ist lesbar und backup-freundlich.

---

## Technischer Stack

- **UI:** [Avalonia](https://avaloniaui.net/) 12 — Fluent Theme, Inter-Schrift.
- **MVVM:** CommunityToolkit.Mvvm.
- **APIs:** PaperMC `api.papermc.io` für Paper- und Velocity-Builds.
- **Sprache / Runtime:** C#, **.NET 10** (`net10.0`).

---

## Projektstruktur (Kurzüberblick)

```
src/MinecraftServerManager/
├── Models/           # ServerInstance, Cluster, Einstellungen, Enums
├── ViewModels/       # Hauptlogik der Oberfläche
├── Views/            # Avalonia-XAML (Hauptfenster, Dialoge, Wizard)
├── Services/         # Registry, Prozessüberwachung, PapermcApi, RCON, Pfade, …
└── Assets/           # Styles, command-presets.json, Icons
```

---

## Tipps & Randnotizen

- **EULA:** Ohne Zustimmung zur [Minecraft EULA](https://aka.ms/MinecraftEULA) kein Quick-Setup — das ist Absicht.
- **Firewall:** Nach dem ersten Start Ports freigeben, wenn du von anderen Rechnern aus erreichbar sein willst.
- **Loader-Mix:** Die App kennt mehrere Loader-Typen; Paper/Velocity sind am stärksten in Wizard und API integriert — andere JARs kannst du als eigenständige Instanzen mit manuellem Pfad betreiben.

---

## Mitmachen / Build-Probleme

- Stelle sicher, dass die **SDK-Version** zum Target Framework passt (`dotnet --version`).
- Bei Publish-Problemen: `dotnet workload list` und ggf. Visual Studio Build Tools aktualisieren.

---

*Viel Spaß beim Hosten — mit weniger Chaos und mehr Überblick.*
