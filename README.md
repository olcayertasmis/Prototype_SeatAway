# Seat Puzzle - Mobile Prototype

Mobile-ready grid-based puzzle game developed for the technical assessment.

---

## 🚀 Core Features
- **Dynamic Grid System:** Fully scalable grid-based layout with obstacles and entrances.
- **AI Pathfinding:** Robust BFS (Breadth-First Search) implementation for passenger navigation.
- **Custom Level Editor:** A visual, non-programmer-friendly Editor Window for rapid level design.
- **Mobile Optimized:** Integrated Safe Area handling and high-performance rendering.

### 🛠️ Custom Level Editor Tooling
To ensure Game Designers can build and iterate levels seamlessly, I developed a custom visual editor. Designers can paint obstacles, place seats, and define entrances without touching a single line of code.

---

## 🏗️ Architecture & Design Patterns
- **SystemLocator (Service Locator):** Decouples core managers (Grid, UI, Passenger, etc.) for high modularity.
- **Event-Driven Communication:** Systems interact through lightweight structs (Events), ensuring a clean and decoupled codebase.
- **Object Pooling:** Utilizes Unity's modern `UnityEngine.Pool` API via a centralized `PoolManager` to handle entity lifecycles without GC spikes.
- **Draw Call Optimization:** Uses `MaterialPropertyBlock` for procedural coloring of mesh instances.
- **Physics-Free Collisions:** Implemented `Physics.BoxCast` and `Mathf.Clamp` boundaries for seat dragging to ensure crisp movement without the overhead and jitter of Rigidbody physics.

## 🎨 Game Feel (Juice)
- **Responsive Controls:** Custom drag-and-drop logic with `DOTween` for smooth snapping.
- **Visual Feedback:** "Ghost Seat" visual aid during dragging and "Timer Pulse" for urgency.
- **Animation Polish:** High-quality tweened animations for seat jumps, UI pops, and passenger arrivals.

## 🔌 SDK Integration (Stubs)
- **AnalyticsManager:** Ready-to-go stubs for tracking level progression and funnel data.
- **AdManager:** Prepared methods for interstitial ad logic at key game loop trigger points.

## ⏳ Technical Decisions & Trade-offs
- **BFS over A*:** For the current small grid sizes with unweighted node traversal, BFS provides optimal performance and simplicity. A* would introduce unnecessary overhead (heuristic calculations and priority queue sorting) for this specific design constraint.
- **Geometric Raycasting:** Instead of placing heavy physical colliders to act as "invisible walls" or floors, I used mathematical `Plane.Raycast` and grid-coordinate clamping. This prevents the physics engine from constantly calculating raycast hits, saving significant CPU cycles on mobile devices.

## 🔮 Future Improvements (Given More Time)
While the current prototype serves as a highly functional foundation, I would implement the following features with an extended development timeline:

- **Data-Driven Color Architecture:** Currently, seat and passenger colors are mapped via an `Enum` and a `switch-case` material assignment for rapid prototyping. I would refactor this into a `ColorData` ScriptableObject system. This would allow Game Designers to create new colors, assign materials, and balance passenger distribution purely through the Inspector, completely decoupling visual assets from the C# logic.
- **Advanced Level Editor Tooling:**
- *State Management:* Integrating Unity's `Undo.RecordObject` and dirty flags to support standard Undo/Redo/Discard operations during level design.
- *Visual Clarity:* Implementing custom Editor Gizmos to visually combine and distinguish multi-cell seats (e.g., drawing a distinct bounding box for a 2x1 seat vs. two adjacent 1x1 seats) to improve the designer's UX.
- **Dynamic Camera Framing:** Developing a camera controller system that automatically calculates the orthographic size and position based on the `GridManager`'s bounding box. This would ensure that levels of any size (e.g., 4x4 vs 8x12) are perfectly centered and framed across all device aspect ratios.
- **Progression & Save System:** Implementing a lightweight local persistence layer (using JSON or PlayerPrefs) to track level progression, unlocked stages, and session data.
- **Dynamic Environments & Theming:** Expanding the `LevelData` ScriptableObject to support specific environment themes (biomes). This would allow the grid visuals, background colors, and ambient to dynamically change based on the current level, providing visual variety as the player progresses.

## ⚙️ Requirements
- **Unity Version:** 6000.3.10f1 LTS
- **Plugins:** DOTween, Input System

---

## 📱 Screenshots

### Gameplay & Safe Area
<p align="center">
  <img src="https://github.com/user-attachments/assets/395b96e7-c9af-49e0-ab7e-e92b0680a84e" width="32%" alt="SafeArea1" />
  <img src="https://github.com/user-attachments/assets/5876f0ff-ce44-40a6-9a05-1da7b862df24" width="32%" alt="SafeArea2" />
  <img src="https://github.com/user-attachments/assets/e1e70028-f497-44ca-84cb-5e0f46c4e47a" width="32%" alt="Gameplay1" />
</p>

### Custom Level Editor
<p align="center">
  <img src="https://github.com/user-attachments/assets/750b47d8-0692-4fa1-8e85-50e0c210c220" width="49%" alt="LevelEditor2" />
  <img src="https://github.com/user-attachments/assets/3d53e7f6-4784-430d-9969-304556ca1422" width="49%" alt="LevelEditor1" />
</p>
