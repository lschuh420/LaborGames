# Das Player-System

Übersicht über alles, was auf dem Spieler-Charakter liegt — von Bewegung über
Kampf bis zur Lebensanzeige. Spickzettel für die Präsentation.

Das Player-Objekt in der Szene heißt **`HumanMale_Character_FREE`**
(Tag: `Player`, Layer: `Player`). Es basiert auf dem Unity **StarterAssets
Third-Person-Controller** und ist mit eigenen Combat- und Health-Skripten
erweitert.

---

## Die 10 Komponenten auf dem Player (in Reihenfolge im Inspector)

| # | Komponente | Typ | Aufgabe |
|---|---|---|---|
| 1 | **Transform** | Unity | Position / Rotation / Skalierung |
| 2 | **Animator** | Unity | Spielt die Charakter-Animationen (Controller: `StarterAssetsThirdPerson`) |
| 3 | **Character Controller** | Unity | Kollision & Bewegung der Spielfigur (Kapsel) |
| 4 | **Player Input** | Unity (Input System) | Leitet Tastatur-/Maus-Eingaben an die Skripte |
| 5 | **Third Person Controller** | Skript | Laufen, Sprinten, Springen, Ausweichrolle, Kamera |
| 6 | **Basic Rigid Body Push** | Skript | Schiebt physikalische Objekte beim Anlaufen weg |
| 7 | **Starter Assets Inputs** | Skript | Sammelt die Eingabewerte (move, look, jump, sprint) |
| 8 | **Player Shooting** | Skript | Waffen, Schießen, Nachladen, Waffenwechsel |
| 9 | **Player Aim** | Skript | Zielen (ADS): Kamera- & Animations-Umschaltung |
| 10 | **Player Health** | Skript | Leben, Schaden, Regeneration, Tod, God-Mode |

---

## 1. Transform
Position des Spielers in der Welt (Startposition ca. X −232, Y 0.5, Z 67.8).

## 2. Animator
Steuert die Animationen des humanoiden Charakters.
- **Controller:** `StarterAssetsThirdPerson` (Laufen, Springen, Fallen, Rolle …)
- **Avatar:** `HumanMale_CharacterAvatar`
- 11 Animationsclips, getrieben von Werten wie `Speed`, `Grounded`, `Jump`.

## 3. Character Controller (Unity)
Unitys eingebaute Bewegungs-/Kollisionskapsel — kein Rigidbody, sondern
kontrollierte Bewegung. Wichtige Werte:
- **Höhe:** 1.8, **Radius:** 0.28, **Center:** (0, 0.93, 0)
- **Slope Limit:** 45° (max. Steigung), **Step Offset:** 0.25 (Stufenhöhe)

## 4. Player Input (Unity Input System)
Verbindet die Steuerung mit dem Asset `StarterAssets (Input Action)`.
- **Default Map:** `Player`
- **Behavior:** `Send Messages` → ruft `OnMove`, `OnLook`, `OnJump`, `OnSprint`
  auf dem Player auf (diese landen in *Starter Assets Inputs*).

---

## 5. Third Person Controller (Skript)

Das Herzstück der Steuerung. Läuft auf dem Character Controller und treibt den
Animator. **Echte Werte aus dem Inspector:**

- **Bewegung:** Move Speed **2**, Sprint Speed **5.335**, Rotation Smooth **0.12**,
  Speed Change Rate **10**. Der Charakter dreht sich weich in Laufrichtung.
- **Springen / Schwerkraft:** Jump Height **1.2**, Gravity **−15**,
  Jump Timeout **0.3**, Fall Timeout **0.15**. Boden-Erkennung per Kugel
  (Grounded Radius **0.28**) auf dem Layer **Ground**.
- **Ausweichrolle (Dodge-Roll):** auf **Linke Strg**. Roll Speed **8**,
  Roll Duration **0.6 s**, Roll Cooldown **1 s**. Während der Rolle ist normale
  Bewegung gesperrt; `IsRolling` unterbricht kurz das Zielen.
- **Kamera:** treibt das Cinemachine-Ziel `PlayerCameraRoot`, Blickwinkel
  begrenzt auf Top **70°** / Bottom **−30°**.

> Die Bewegung ist kamerarelativ — der Charakter läuft dorthin, wohin die Kamera
> schaut.

## 6. Basic Rigid Body Push (Skript)
Schiebt physikalische Objekte weg, wenn der Spieler dagegen läuft.
> Aktuell **deaktiviert** in der Szene: *Can Push* ist aus und *Push Layers* steht
> auf *Nothing* — die Funktion ist also vorhanden, aber gerade nicht aktiv.

## 7. Starter Assets Inputs (Skript)
Die Brücke zum Input System. Speichert die Roh-Eingaben (`move`, `look`, `jump`,
`sprint`) als einfache Variablen für die anderen Skripte und sperrt den
Maus-Cursor im Spielfenster.

---

## 8. Player Shooting (Skript)

Das zentrale Combat-Skript. Verwaltet **2 Waffenslots**. **Steuerung:**
Linke Maustaste = schießen · **Q / Tab** = Waffe wechseln · **R** = nachladen.

**Echte Werte:**
- **Waffenslots:** 2 (Active Slot 0). Die Waffe sitzt im **Right Hand Holder**
  `jointItemR` (Knochen der rechten Hand).
- **Bullet:** Prefab **PlayerBullet**, Bullet Speed **60**, Shoot Distance **100**.
- **Einschusslöcher:** *Spawn Bullet Hole* an, Größe **0.15** — hinterlässt Decals
  auf Wänden, aber nicht auf Gegnern.
- **Nachladen:** Reload Key **R**, *Auto Reload When Empty* an.
- **Griff-Ausrichtung:** *Align Held Weapon* an — pinnt die Waffe sauber in die
  Hand (pro Waffe über `WeaponData` überschreibbar).

**Ablauf eines Schusses:** Raycast aus der Bildschirmmitte → `PlayerBullet`
fliegt aus der Hand Richtung Trefferpunkt → Schaden kommt aus der `WeaponData`
der aktiven Waffe. Jeder Schuss wird zusätzlich als Geräusch an den Roboter-Boss
gemeldet (`MechSensor.HearNoise`), damit der den Spieler hören kann.

## 9. Player Aim (Skript)

Steuert den **"Aim Down Sight" (ADS)**-Modus mit **Rechtsklick**.

**Echte Verdrahtung:**
- **Normal Cam:** `PlayerFollowCamera` · **Aim Cam:** `PlayerAimCamera`
  (Cinemachine). Beim Zielen wird per Priorität (aktiv **20** / inaktiv **5**)
  auf die enge Schulter-Kamera umgeschaltet.
- **Animation:** Aim Layer Index **1**, Aim Blend Speed **10** — aktiviert die
  Oberkörper-Ebene und die Ziel-Pose.
- **Weapon Check:** prüft über *Player Shooting*, ob eine Waffe in der Hand ist.
- **Aim Rotation Speed 15:** der Körper dreht sich beim Zielen in Blickrichtung
  der Kamera, damit der Spieler dorthin schießt, wohin er schaut.
- Sprinten wird beim Zielen abgebrochen; während einer Rolle ist Zielen gesperrt.

## 10. Player Health (Skript)

Verwaltet die Lebenspunkte. Implementiert `IDamageable` (dasselbe Schadens-
Interface wie die Gegner). **Echte Werte:**
- **Max Health 100.**
- **Regeneration:** Regen Delay **15 s** (so lange ohne Treffer, bevor Heilung
  startet), Regen Rate **5** HP/Sekunde (progressiv).
- **Audio:** optionaler Hit-/Death-Sound, Volume 0.5.
- **God-Mode:** zum Testen mit Taste **K** umschaltbar (unverwundbar).
- **Tod:** deaktiviert Bewegung & Eingabe, stoppt die Animation und löst nach
  2,5 s das `OnPlayerDeath`-Event aus (Game-Over).

---

## Dazugehörige Objekte (nicht auf dem Player, aber Teil des Player-Systems)

Diese liegen als eigene GameObjects in der Szene und arbeiten mit dem Player
zusammen:

| Objekt / Skript | Aufgabe |
|---|---|
| **PlayerFollowCamera** | Normale Third-Person-Kamera (Cinemachine) |
| **PlayerAimCamera** | Enge Schulter-Kamera beim Zielen (Cinemachine) |
| **LowHealthEffect** | Bildschirmeffekt bei wenig HP: Farbe weicht, rote Vignette |
| **Canvas → PlayerHealthUI** | Lebensbalken (vorderer Balken sofort, hinterer zieht verzögert nach) |
| **Canvas → WeaponHUD** | Zeigt die 2 Waffenslots, Waffennamen, Munition, Nachlade-Kreis |
| **WeponPickup (2)** | Aufsammelbare Waffe am Boden → ruft `PlayerShooting.PickUpWeapon()` |
| **GameEndManager** | Reagiert auf das Tod-Event des Spielers |

**Unterstützende Skripte (auf Waffen/Kugeln, nicht auf dem Player):**
- `WeaponData` — Schaden, Feuerrate, Magazingröße pro Waffe
- `Bullet` / `PlayerBullet` — Flugbahn & Trefferberechnung der Kugel
- `WeaponPickup` — Logik zum Aufnehmen

---

## Roter Faden für die Präsentation

1. **Eingabe** — *Player Input* + *Starter Assets Inputs* liefern die Rohdaten.
2. **Bewegung** — *Third Person Controller* macht daraus Laufen / Springen / Rollen
   auf dem *Character Controller*, der *Animator* zeigt die passende Animation.
3. **Zielen** — *Player Aim* wechselt Kamera & Pose (Rechtsklick).
4. **Kampf** — *Player Shooting* → `PlayerBullet` → `IDamageable`; derselbe
   Schadens-Mechanismus gilt für Spieler **und** Gegner.
5. **Überleben** — *Player Health* + UI + *LowHealthEffect* verwalten Leben,
   Heilung und Feedback bis zum Tod.
