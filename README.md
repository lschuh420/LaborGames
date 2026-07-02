# Projektname: SPARC

## Teammitglieder
- Lennard Schuh
- Yakob Lahdo
- Balint Schmidt

## Besonderheiten des Projekts & Start
- **Start:** Unter `Assets/Scenes/` die Szene **`Environment NEW.unity`** öffnen und oben im Editor auf Play drücken.
- **Steuerung:** WASD für Bewegung, Maus für Kamera/Umsehen, Maus-Klicks für Schießen.

## Besondere Leistungen, Herausforderungen & Erfahrungen

### Teilbereich: Der Roboter & KI (Lennard)
Da ich vorher noch gar keine Erfahrung mit Unity hatte, war allein das Lernen der Engine schon eine ziemliche Herausforderung. Gleichzeitig dann direkt den komplexen Roboter zu bauen, hat viel Zeit gekostet.

Die größten Herausforderungen und Zeitfresser:
- Laufen lernen: Am Anfang gab es große Probleme, den Roboter überhaupt zum Laufen zu bringen. Es hat einige Tage und viel Ausprobieren gebraucht, bis die sechs Beine endlich vernünftige Schritte gemacht haben.
- Inverse Kinematik (IK): Danach musste die IK so programmiert werden, dass die Füße immer den Boden berühren. Das war ziemlich aufwendig, weil viel Mathematik und Logik dahintersteckt.
- Terrain-Anpassung: Als das Laufen auf einer geraden Fläche klappte, kam das nächste Problem. Der Roboter und das NavMesh mussten so angepasst werden, dass er auch ordentlich über Hügel und unebenes Terrain hoch und runter laufen kann.
- Behavior Designer: Die KI-Logik über die Behavior Trees aufzubauen, war gar nicht so einfach. Da musste man sich erst mal richtig reindenken, bis die ganzen Custom-Scripts so funktioniert haben, wie sie sollten.

### Teilbereich: Der Spieler (Player) & UI (Balint)
Mein Bereich war der komplette Spielercharakter: Steuerung, Schießen, Zielen (Aim), das Lebenssystem sowie die gesamte dazugehörige Benutzeroberfläche (UI).

**Aufbau des Spielers:**
- **Third-Person-Controller:** Als Basis dient das `ThirdPersonController`-Script (StarterAssets) für Bewegung, Springen und Kamera-Handling.
- **Player Shooting (`PlayerShooting.cs`):** Steuert das Abfeuern der Waffe, Munition, Feuerrate und Raycast-/Projektil-Treffer auf die Gegner.
- **Player Aim (`PlayerAim.cs`):** Regelt den Wechsel zwischen normalem Laufen und dem Zielmodus (Aim), inkl. Kamera-Zoom und Anpassung der Steuerung beim Zielen.
- **Player Health (`PlayerHealth.cs`):** Verwaltet die Lebenspunkte des Spielers, das Erleiden von Schaden, die Lebensregeneration (z.B. beim Verstecken) und den Tod.
- **Kameras:** Eine Third-Person-Follow-Kamera für die normale Bewegung sowie eine zweite Aim-Kamera für den Zielmodus (Umschalten über Cinemachine/Kamerawechsel), um beim Zielen näher an den Charakter heranzuzoomen.

**Selbst erstellter Canvas & UI-Scripts:**
- **Weapon HUD (`WeaponHUD.cs`):** Zeigt die aktuelle Waffe und den Munitionsstand an.
- **Healthbar mit `PlayerHealthUI.cs`:** Anzeige der aktuellen Lebenspunkte des Spielers, gekoppelt an das Player-Health-System.
- **Low Health Effect (`LowHealthEffect.cs`):** Ein Bildschirm-Effekt (roter Rand/Vignette), der eingeblendet wird, sobald der Spieler wenig Leben hat, um eine Warnung zu geben.
- **GameEndManager mit `GameEndUI.cs`:** Verwaltet das Spielende (Sieg/Niederlage) und blendet den passenden End-Screen ein.

**Animationen:**
- Für den Spieler wurden **Aim Idle-** und **Aim-Animationen** aus **Mixamo (Adobe)** verwendet und in den Animator eingebunden.

**Größte Herausforderung:**
Das mit Abstand größte Problem war die **korrekte Waffenposition und -rotation in der Hand des Charakters** – sowohl in der **Idle-Aim-Animation** als auch in der eigentlichen **Aim-Animation**. Da die Mixamo-Animationen die Hand ständig bewegen, hat die Waffe immer wieder falsch in der Hand gelegen, sich verdreht oder ist verrutscht. Wir haben sehr viel Zeit investiert, um die Waffe sauber an der Hand auszurichten, haben uns dann aber später bewusst dazu entschieden, **andere Features zu priorisieren**, statt die Waffenausrichtung bis ins letzte Detail perfekt zu machen.

## Eigens erstellte Assets
*Alles, was komplett selbst von Hand erstellt wurde.*
- 3D-Modell des Roboters: Der Körper und die einzelnen Beine des Boss-Gegners wurden komplett selbst in Unity mit ProBuilder modelliert. Da wir das Modell von Null aufgebaut haben, steckt da einiges an Zeit drin.
- Partikeleffekte: Die Funken und Effekte, wenn ein Bein zerstört wird, sind selbst gebaut.
- Skripte: Die Custom-Skripte für die Inverse Kinematik, das Schadenssystem und die KI-Entscheidungen wurden komplett mit Hilfe von KI erstellt, da ich selbst nicht programmiert habe.
- [Weitere Punkte der Teammates]

## Verwendete externe Assets & Inspiration
*Assets aus dem Store oder dem Internet.*
- **Behavior Designer (Opsive):** Für die Erstellung des KI-Entscheidungsbaums (Behavior Tree).
- **StarterAssets:** Für den Third-Person-Controller des Spielers.
- Energiefeld-Asset: Für die Energiewelle, die Flächenschaden um den Roboter herum austeilt.
- Diverse **Sound-Assets**, die im Projektordner liegen.
- **Free Low Poly Human RPG Character:** 3D-Modell des Spielercharakters – https://assetstore.unity.com/packages/3d/characters/humanoids/fantasy/free-low-poly-human-rpg-character-219979
- **Low Poly Weapons Vol.1:** Waffenmodelle für den Spieler – https://assetstore.unity.com/packages/3d/props/guns/low-poly-weapons-vol-1-151980
- **Mixamo (Adobe):** Aim Idle- und Aim-Animationen für den Spieler.
- [Weitere Assets der Teammates]

--------------------------------------------------------------------------------------------------------------------

### Teilbereich: Environment & Map (Yakob)
Aufbau der gesamten Spielwelt (Map, Umgebung, Beleuchtung, Atmosphäre).

## Herausforderungen & Erfahrungen
- Anfangs haben wir uns für eine Arena-ähnliche Map entschieden, aber nach ersten Tests sah alles außerhalb der Arena sehr leer aus – deshalb wurde die Map erweitert, sodass man von der Arena aus höhere Gebäude und eine stimmigere Umgebung sieht.
- Die Map wurde so konfiguriert, dass der Roboter fast überall laufen kann (NavMesh-Optimierung) und der Spieler sich verstecken kann, um Leben zu regenerieren, ohne vom Roboter gesehen zu werden.
- Viele Assets durchprobiert und getestet: Oft habe ich aus einem ganzen Asset-Paket nur ein oder zwei passende Objekte benutzt und den Rest verworfen.
- Die Konfiguration der Map war nicht besonders kompliziert – es war eher ein ständiges Ausprobieren und Testen, was am besten aussieht und funktioniert. Das hat viel Zeit gekostet.
- Manchmal kam ich nicht weiter, weil die Ideen ausgingen oder ich keine passenden Assets gefunden habe, die zum Stil gepasst haben.
- Beleuchtung, Shader und Terrain-Gestaltung durch viele Tutorials gelernt (Hügel formen, Lichtarten, Atmosphäre).
- Die gesamte Map als zentrales Prefab (`Map_complete`) gespeichert, sodass sie einfach in jede Szene eingefügt werden kann.

## Eigens erstellte Assets
- Lichtquellen aus verschiedenen Assets zusammengestellt und in die Map eingefügt (mit passenden Licht- und Schatteneinstellungen).
- Einzelne Objekte aus verschiedenen Paketen kombiniert (z.B. Baumgruppen, Steine, Ruinen) – wie ein Puzzle zusammengesetzt.

## Verwendete externe Assets
- **Low Poly Atmospheric:** Bäume, Pflanzen und Natur-Elemente
- **Simple Apocalypse:** Ruinen, zerstörte Gebäude
- **Simple Fantasy:** Steine, Felsen, Naturformationen
- **SkySeries Freebie:** Himmel und atmosphärische Beleuchtung
- **Tim's Substances:** Boden-Texturen und Terrain-Materialien
- **ASTROFISH GAMES / Medieval Wells Props:** Brunnen, Wegelemente
- Weitere kleinere Assets aus dem Unity Asset Store










## Video & Projekt-Link
- **Video:** [Link zum Video](https://drive.google.com/file/d/1ApLz4zzQKtQK3OSSHhS8_yY8Gg9TX8ZT/view?usp=sharing)
- **Projekt:** [GitHub Repository (LaborGames)](https://github.com/lschuh420/LaborGames)
