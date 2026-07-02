# Präsentations-Notizen: Technischer Deep-Dive I – Der Roboter & KI
*(ca. 5–6 Minuten)*

> **Ziel:** Hier punktest du bei den Dozenten. Zeig ruhig den Unity Editor oder Diagramme statt nur Gameplay. Erkläre, dass viel davon **prozedural** läuft (nichts ist von Hand animiert) und ihr eigene Custom-Scripts geschrieben habt.

---

## 0. Der Einstieg (30 Sek.) – Die Herausforderung

- Ein **riesiger, sechsbeiniger Roboter (Hexapod-Mech)**, der sich glaubhaft über **unebenes Terrain** bewegt.
- Problem: Klassische Animationen würden bei jedem Hügel "in der Luft schweben" oder im Boden versinken.
- **Unsere Lösung:** Alles ist **prozedural** – Füße, Körperhaltung und Verhalten werden in Echtzeit per Mathe/Raycasts berechnet. Kein einziger Schritt ist voranimiert.
- **Merksatz:** *"Der Mech weiß nicht, wie der Boden aussieht – er tastet ihn jeden Frame neu ab."*

---

## 1. Inverse Kinematik (IK) – Wie die Beine funktionieren (~1,5–2 Min.)

> **Demo-Tipp:** Im Editor `LegIK` → Haken bei **`showPresentationGizmos`** setzen. Dann sieht man in der Scene View die Bones (cyan), Gelenke (gelbe Kugeln) und das IK-Target (roter Würfel) live.
> Bei `StepManager` → Haken bei **`showRaycastGizmos`**: zeigt die Raycasts (magenta), die Bodentreffer (grün) und die Boden-Normale (gelb).

### Vorwärts- vs. Inverse Kinematik (kurz erklären)
- **Normale (Forward) Kinematik:** Man dreht jedes Gelenk einzeln → die Fußspitze ergibt sich daraus. Mühsam.
- **Inverse Kinematik:** Wir geben **das Ziel der Fußspitze** vor, und das Script **rechnet die Gelenkwinkel zurück**. Genau das brauchen wir, damit der Fuß exakt auf dem Boden landet.

### Schritt A: Den Boden finden (Raycast)
- Für jedes Bein wird ein **Raycast nach unten** geschickt (`StepManager.GetGroundInfo`).
- Start oberhalb des Beins (`rayStartHeight = 3 m`), nach unten bis `rayDistance = 20 m`.
- Der **Trefferpunkt (`hit.point`)** wird zum neuen Fuß-Ziel.
- Die **Boden-Normale (`hit.normal`)** wird genutzt, um den Fuß **passend zur Hangneigung zu drehen** → der Fuß "klebt" am Untergrund, auch am Hang.

### Schritt B: Das Kniegelenk berechnen (Trigonometrie / Cosinussatz)
- Wir kennen 2 feste Längen: **Oberschenkel (`lengthFemur`)** und **Unterschenkel (`lengthTibia`)** – werden beim Start aus dem Modell gemessen.
- Wir kennen die **Distanz von Hüfte zum Ziel**.
- → Daraus berechnet der **Cosinussatz** den exakten **Hüft-/Kniewinkel**, damit die Fußspitze genau das Ziel trifft:
  ```
  cos(Hüftwinkel) = (femur² + dist² − tibia²) / (2 · femur · dist)
  ```
  *(in Code: `LegIK.SolveIK()`, Zeile ~64)*
- Reihenfolge im Code:
  1. **Yaw:** Hüfte dreht sich Richtung Ziel (`Quaternion.LookRotation`).
  2. **Pitch:** Oberschenkel wird per berechnetem Winkel ausgerichtet (`Quaternion.FromToRotation`).
  3. **Knie:** Unterschenkel wird zum Ziel gebogen.
- **Absturzsicher:** Die Distanz wird auf die max. Beinlänge geklemmt (`Mathf.Clamp`). Wenn der Boden zu weit weg ist, gibt es eine **"ÜBERDEHNT"-Warnung** im Log → schöner Punkt für "Wir haben Debug-Tools eingebaut".

### Schritt C: Der Schritt selbst – nicht teleportieren, sondern gehen (`LegStepper`)
- Ein Fuß **rutscht nicht**, sondern macht einen echten Schritt in **3 Phasen**:
  1. **Pre-Lift** – Fuß hebt minimal an (kein Schleifen).
  2. **Swing** – Fuß schwingt in einem **Bogen** nach vorn (`verticalArc`-Kurve) zum neuen Ziel.
  3. **Lock** – Fuß wird sauber am Zielpunkt fixiert.
- Schritt-Dauer ist **dynamisch**: schneller Mech = schnellere Schritte (`distance / stepSpeed`).

### Schritt D: Der Gang-Rhythmus (`StepManager` – Gait Sequencer)
- Welche Beine wann gehen, steuert ein **Gangmuster**. Wir haben 3 implementiert:
  - **Tripod** – 3 Beine gleichzeitig (schnell & agil).
  - **Tetrapod** (Standard) – 2 diagonale Beine (wuchtig & stabil, "Bastion-Look").
  - **Wave** – jedes Bein einzeln (extrem langsam & schwer).
- **Urgency-System:** Jedes Bein misst, wie weit es von seiner Ideal-Position weg ist. Erst wenn der Abstand einen Schwellwert (`stepThreshold = 0,5 m`) überschreitet, wird ein Schritt ausgelöst → kein Trippeln im Stand.
- **Prediction:** Die Füße werden leicht **dorthin gesetzt, wo der Mech gleich sein wird** (Geschwindigkeit × `predictionSeconds`) → wirkt vorausschauend, nicht hinterherhinkend. Vorderbeine greifen weiter vor (`frontLegReachMultiplier = 1,5`).
- **Staggering:** Max. 2 Beine gleichzeitig in der Luft, mit Mindest-Zeitabstand → er fällt nie um.

### Schritt E: Der Körper passt sich an (`BodyAdaptation`)
- Der **Rumpf liegt nicht starr** auf den Beinen, sondern reagiert:
  - **Höhe:** Körperhöhe = Durchschnitt aller Fußpositionen + Basishöhe → senkt sich in Senken, hebt sich auf Hügeln.
  - **Neigung (Tilt):** Aus den Fußpositionen wird per **Kreuzprodukt** eine "Terrain-Normale" berechnet → der Körper **kippt mit dem Gelände** (max. `maxBodyTilt = 25°`).
  - **Spring/Impact:** Bei jedem Aufsetzen eines Fußes ein kleiner **Feder-Wackler** (Spring-Physik) → fühlt sich schwer und wuchtig an.
  - **Aim-Assist-Lean:** Der Körper neigt sich leicht in Richtung Ziel, um beim Zielen zu helfen.

---

## 2. Behavior Designer – Die KI (~1,5–2 Min.)

> **Demo-Tipp:** Behavior Tree im Editor öffnen und einmal durchscrollen. Wichtig: zeigen, dass es **fertige Standard-Nodes (grün)** UND **eigene Custom-Nodes (Kategorie "Mech Custom")** gibt.

### Was ist ein Behavior Tree (kurz)
- Eine **visuelle State-Machine / Entscheidungsbaum**: von oben nach unten werden Bedingungen geprüft und Aktionen ausgeführt.
- Vorteil: Verhalten ist **modular und sichtbar** – man "verdrahtet" Logik, statt riesige if-else-Skripte zu schreiben.

### Fertige Nodes, die wir nutzen
- Aus dem **Behavior Designer + Movement Pack**: z. B. `CanSeeObject` (Sicht), `Pursue`/`Seek` (Verfolgung über **NavMesh**), `Patrol`, `Cover`.
- Wahrnehmung läuft über einen **NavMeshAgent** für die Wegfindung um Hindernisse.

### Eigene Custom-Scripts (DAS hier betonen!)
Wir haben fertige Nodes **nicht ausgereicht** – also eigene C#-Tasks geschrieben (Kategorie `[TaskCategory("Mech Custom")]`):

| Custom-Script | Aufgabe |
|---|---|
| **`RobotCombatMaster`** | Kugelsichere eigene **State-Machine** im Tree (Jagen/Schießen → Aufladen → Nova-Explosion → Cooldown). Reagiert auf Stun & "Humpeln" (reduziert Tempo, wenn Beine fehlen). |
| **`SetTurretTarget`** | Übergibt das Ziel aus dem **Blackboard** an den `TurretController` (Turm-Steuerung). |
| **`IsPlayerInMeleeRange`** | Bedingung: Ist der Spieler im Nahkampf-/Verteidigungsradius? → löst die Nova aus. |
| **`MechSensor`** | Eigenes **Sinnes-/Wahrnehmungssystem**: Sichtkegel (FoV 90°, 25 m), Sichtlinien-Check gegen Hindernisse, **Gedächtnis** (bleibt 8 s nach Sichtverlust "alarmiert"), Status-Licht (grün/gelb/rot). Speist das Behavior-Tree-Blackboard. |
| **`TurretController`** | Dreht Turm (Yaw) und Kanonen (Pitch) **träge & gewichtig** zum Ziel; meldet, wann er "anvisiert" hat (`IsAimedAtTarget`) → erst dann wird geschossen. |

- **Wichtigste Aussage:** *"Wir haben Behavior Designer als Gerüst genutzt, aber die eigentliche Mech-Intelligenz – Wahrnehmung, Zielen, Kampf-Zustände – in eigenen C#-Nodes implementiert und ins Blackboard verdrahtet."*

---

## 3. Das Schadenssystem (~1,5 Min.)

> **Demo-Tipp:** Kurzes **Video/Clip**, wie ein Bein weggeschossen wird, **Funken fliegen** und der Roboter reagiert (einsacken/humpeln).

### Treffer-Routing (`DamageRouter`)
- Jeder Körperteil-Collider hat einen **Router** mit `damageMultiplier`:
  - **Panzerung:** Multiplikator < 1 → wenig Schaden.
  - **Schwachstellen (gelbe Kugeln am Knie):** Multiplikator hoch → viel Schaden.
- Schaden geht **zentral** an `MechBossHealth` (Gesamt-HP: **2500**), Beine haben **zusätzlich eigene HP (300 pro Bein)**.

### Bein zerstören (`MechLegHealth`) – die Show
Wenn die Bein-HP auf 0 fällt (`DestroyLeg()`), passiert eine **Kettenreaktion**:
1. **Schwachstelle (gelbe Kugel) verschwindet.**
2. **IK wird abgeschaltet** für dieses Bein.
3. **Physik:** Das Bein-Mesh wird vom Körper gelöst, bekommt **Rigidbody + Collider** und fliegt mit Impuls + Drehung weg (fällt realistisch zu Boden).
4. **Explosions-Partikel** + **kontinuierliche Funken** (unsere weißen Funken!) sprühen dauerhaft aus dem kaputten Gelenk – prozedural erzeugt, falls kein Prefab gesetzt ist.
5. **Sound** (Bein-Zerstörung).

### Wie der Roboter darauf reagiert (das Glaubhafte!)
- `StepManager` markiert das Bein als **zerstört** → es macht keine Schritte mehr, die anderen Beine **spreizen sich nach außen** (Splay), um Balance zu halten.
- `BodyAdaptation` ignoriert den fehlenden Fuß → die **betroffene Seite sackt ab**, der Körper **sinkt dauerhaft** ein Stück (10 % pro Bein).
- `MechBossHealth` löst einen **Stun/Stagger** aus (4 s am Boden), Events feuern (`OnMechStun`).
- Ab **2 verlorenen Beinen** → **Limping**: Die KI (`RobotCombatMaster`) **reduziert das Tempo um 60 %** und die Schritt-Animation wird langsamer ("Struggle").
- **Arc-Raiders-Style:** Jeder Treffer hat Konsequenzen – der Boss wird sichtbar schwächer und schwerfälliger.

### Tod (`TriggerMechDeath` / Death Sequence)
- Bei 0 Gesamt-HP: **Kettenreaktion aus 6 Explosionen**, dann finaler Blow-out.
- KI & NavMesh werden abgeschaltet, einzelne Mesh-Teile **fallen physikalisch ab** (`AddExplosionForce`), der Körper kippt zu Boden – nur noch Schrott.

---

## Spickzettel – Kernzahlen & Begriffe (falls jemand nachfragt)

- **6 Beine**, prozedural, IK über **Cosinussatz**.
- **Boden-Erkennung:** Raycast pro Bein (3 m über, 20 m runter), nutzt `hit.point` + `hit.normal`.
- **Gangmuster:** Wave / Tripod / **Tetrapod (Standard)**.
- **Boss-HP:** 2500 · **Bein-HP:** 300/Bein · **Stun:** 4 s · **Limp ab 2 Beinen** (−60 % Speed).
- **KI:** Behavior Designer + eigene Nodes (`RobotCombatMaster`, `MechSensor`, `SetTurretTarget`, `IsPlayerInMeleeRange`), Wegfindung über **NavMesh**.
- **Wahrnehmung:** Sichtkegel 90°, 25 m, 8 s Gedächtnis.
- **Buzzwords für Dozenten:** *Inverse Kinematik, Cosinussatz, Raycast-Grounding, Boden-Normale, prozedurale Animation, Gait-Sequencer, Behavior Tree / Blackboard, Custom-Tasks, Spring-Physik, Schadens-Routing mit Multiplikatoren.*

---

## Relevante Dateien (für die Live-Demo im Editor)

- **IK / Beine:** `Assets/Roboter/LegIK.cs`, `LegStepper.cs`, `StepManager.cs`, `BodyAdaptation.cs`
- **KI:** `Assets/Roboter/DefenseFieldScripts/RobotCombatMaster.cs`, `Assets/Roboter/MechSensor.cs`, `SetTurretTarget.cs`, `IsPlayerInMeleeRange.cs`, `TurretController.cs`
- **Schaden:** `Assets/Roboter/DamageSystem/DamageRouter.cs`, `MechBossHealth.cs`, `MechLegHealth.cs`
- **Gizmos einschalten:** `LegIK.showPresentationGizmos`, `StepManager.showRaycastGizmos`
