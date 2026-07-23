# Unity Procedural Hexapod Walker

A physics-based, procedural animation simulation of a multi-legged walker (hexapod robot) built in **Unity 6**. Rather than relying on traditional keyframed walk animations, the robot's movement is dynamically computed in real-time using Inverse Kinematics (IK), raycasting for terrain detection, and a custom gait sequencer.

## Key Features

* **Procedural Animation (IK):** Legs dynamically step towards target positions using raycasts to detect ground surfaces.
* **Gait Sequencer:** Supports multiple robotic walking patterns to balance speed and stability:
  * **Wave Gait:** Single leg moves at a time (high stability, slow speed).
  * **Tripod Gait:** Three legs move simultaneously (fast, agile).
  * **Tetrapod Gait:** Two diagonal legs move concurrently.
* **Terrain Adaptation:** Dynamically adapts body height and orientation (roll, pitch) based on local terrain slopes.
* **Integrated Combat & Interaction:**
  * Third-Person Orbit Camera.
  * Raycast-based shooting, bullet physics, and basic enemy health tracking.
  * Weapon HUD and item pickups.
  * Automated turret aiming and target tracking.

## Technical Details & Architecture

### Core Scripts (`Assets/`)
* **`StepManager.cs`:** The brain of the walking sequencer. Schedules which legs can step based on the selected gait pattern, maximum simultaneous step limits, and stagger delays.
* **`LegStepper.cs`:** Controls individual leg trajectories, interpolating steps over time and adjusting for step height.
* **`BodyAdaptation.cs`:** Calculates the average height and normal vector of all active feet to position and tilt the main body, ensuring the robot follows the terrain incline.
* **`RobotController.cs` & `TurretController.cs`:** Manages high-level robot movement inputs and drives active targeting systems for the weapon systems.

### Technologies Used
* **Engine:** Unity 6 (`6000.0.71f1`)
* **Rendering:** Universal Render Pipeline (URP)
* **Input:** New Unity Input System (`InputSystem_Actions`)
* **Language:** C#

## Getting Started

1. Open the project in **Unity 6** (`6000.0.71f1` or later).
2. Load the scene `Assets/Scenes/Environment NEW.unity`.
3. Press **Play** in the Unity Editor to start the simulation.
