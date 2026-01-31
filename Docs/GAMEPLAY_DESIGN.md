# Gameplay Design Document

## Genre & Core Identity

**Genre:** Real-Time Strategy / Squad Management / Survival

**Elevator Pitch:** Command a squad of miners and soldiers on hostile alien planets. Extract resources while evading or fighting alien threats. Every mission is a tense balance between greed and survival.

**Primary Inspirations:**
- **Alien: Dark Descent** - Real-time squad tactics against overwhelming alien threat
- **Satisfactory** - Base building, resource extraction, production chains
- **XCOM** - Squad management, pre-mission prep, high stakes

---

## Core Gameplay Loop

```
MISSION SELECT
     ↓
SQUAD LOADOUT (XCOM-style pre-mission)
     ↓
PLANETARY DROP
     ↓
┌─────────────────────────────────────┐
│  CORE LOOP:                         │
│  1. Explore / Scout                 │
│  2. Establish mining operation      │
│  3. Defend against alien waves      │
│  4. Extract resources               │
│  5. Decide: push further or evac?   │
└─────────────────────────────────────┘
     ↓
EXTRACTION (success/failure)
     ↓
DEBRIEF / UPGRADE / STORY PROGRESSION
     ↓
(repeat)
```

---

## Moment-to-Moment Gameplay

### Phase 1: Deployment
- Squad drops onto planet surface
- Limited visibility - fog of war
- Initial safe zone near drop ship
- Player chooses direction to scout

### Phase 2: Exploration & Setup
- Send scouts ahead to find resource deposits
- Discover alien nest locations (avoid or mark for later)
- Find defensible positions for mining outpost
- Environmental hazards to navigate

### Phase 3: Mining Operations
- Deploy mining equipment on resource nodes
- Mining creates NOISE - attracts aliens over time
- "Threat meter" builds as you mine
- More resources = more danger
- Must protect miners while they work

### Phase 4: Defense
- Aliens attack in waves based on threat level
- Real-time tactical combat (not turn-based)
- Position soldiers, set up chokepoints
- Use building system for defensive structures
- Father/daughter can have special abilities?

### Phase 5: The Decision
Every mission presents a choice:
- **Evac now:** Safe, but less resources
- **Push further:** More resources, higher risk of squad loss
- This creates natural tension and player agency

### Phase 6: Extraction
- Call for evac (dropship takes time to arrive)
- Survive final wave while waiting
- Anyone left behind is LOST (permadeath for squad?)
- Father and daughter are essential (game over if lost?)

---

## Systems

### Squad Management
- Recruit/train soldiers between missions
- Each soldier has skills, personality, relationships
- Permadeath creates attachment and stakes
- Father and daughter are always in squad (story requirement)

### Resource Economy
- Multiple resource types (minerals, alien materials, rare elements)
- Resources used for:
  - Upgrading equipment
  - Building base facilities
  - Story progression (Earth needs X to rebuild Y)
  - Unlocking new planets/regions

### Building System (Implemented)
Current implementation supports:
- **Floors/Foundations** - Base building surfaces
- **Walls** - With variants (solid, window, door, corner)
- **Roofs** - Cover from elements/aerial threats
- **Props** - Stairs, decorative elements
- **Machines** - Category ready for mining/production equipment

Building uses grid snapping and ghost preview (green=valid, red=invalid).

**Planned additions:**
- Defensive structures (turrets, barricades)
- Mining equipment (drills, extractors, conveyors)
- Power systems?
- Storage containers

### Alien Threat System
- Aliens react to noise/activity
- Different alien types with different behaviors
- Nest locations spawn reinforcements
- Boss aliens for high-value deposits?
- Time of day cycle affecting alien activity?

---

## Progression Structure

### Mission-Level Progression
1. Complete mission objectives
2. Extract with resources
3. Bonus for optional objectives
4. Penalty for squad losses

### Campaign Progression
1. Start with limited gear and small squad
2. Unlock better equipment through resources
3. Access new planets with better resources (and worse aliens)
4. Story beats unlock between mission tiers
5. Final mission = culmination of father/daughter arc

### Upgrade Trees (TBD)
- Soldier abilities and gear
- Mining efficiency
- Base defenses
- Dropship capabilities
- Story-related unlocks

---

## Control Scheme

**Input:** Mouse + Keyboard (New Input System implemented)

**Camera:** Top-down or isometric RTS view (primary), potential FPS mode for story moments

**Current Implementation:**
- First-person controller exists (for story scenes?)
- Building system uses center-screen cursor
- Grid snapping with R/T for rotation

**Planned:**
- Click to select units
- Right-click to move/attack
- Drag box for multi-select
- Hotkeys for abilities and building

---

## UI Requirements

### In-Mission UI
- Squad portraits with health/status
- Minimap with fog of war
- Threat level indicator
- Resource counts
- Building quick-menu (implemented as category tabs)
- Evac timer when extraction called

### Between-Mission UI
- Squad roster and loadout
- Base overview
- Tech tree / upgrades
- Mission select map
- Story/dialogue system

---

## Current Implementation Status

### Done ✓
- Building system with grid snapping
- BuildingPiece ScriptableObject structure
- Categories: Floors, Walls, Roofs, Props, Machines
- Ghost preview with valid/invalid materials (URP compatible)
- Building UI with category tabs (UI Toolkit)
- First-person controller (for story scenes)
- Squad preview scene (XCOM-style walk-in)
- URP material upgrade tooling

### In Progress
- Squad animation setup (Protofactor soldiers working)
- Scene structure

### Not Started
- RTS camera and controls
- Unit selection and movement
- Alien AI and spawning
- Mining mechanics
- Resource collection
- Threat system
- Mission structure
- Save/load
- Story integration

---

## Technical Notes

### Platform
- Unity 6 (6000.0.23f1)
- Universal Render Pipeline (URP)
- New Input System

### Asset Packages in Use
- **Protofactor Sci-Fi Mega Pack Vol 3** - Soldiers, creatures, robots, animations
- **Elite Soldiers** - Additional soldier meshes (need Mixamo for animations)
- **3D SciFi Kit Vol 3 (Creepy Cat)** - Building pieces, structures
- **Exo-planets NovaShade** - Planet visuals, atmospheres

### Code Architecture
- ScriptableObjects for data (BuildingPiece)
- MonoBehaviours for runtime logic
- Editor scripts for tooling
- UI Toolkit for runtime UI

---

## Design Pillars

### 1. Tension Through Choice
Every decision should matter. Stay or go? Fight or flee? Save the soldier or the resources?

### 2. Earned Resources
Nothing comes free. Risk scales with reward. Players should feel the cost.

### 3. Squad as Family
These aren't disposable units. Each loss should sting. The father/daughter bond is the emotional anchor.

### 4. Moment-to-Moment Engagement
Even mining should be tense. The threat meter ticking up. The distant alien screech. The question of "one more deposit?"

---

## Open Design Questions

- Turn-based vs real-time combat? (Leaning real-time like Alien: Dark Descent)
- Permadeath for all soldiers or just narrative consequences?
- How punishing should mission failure be?
- Base building between missions or just upgrades?
- How do father/daughter differ mechanically from other squad?
- Multiplayer potential? Co-op?

---

*This document evolves with development. Update as systems are implemented.*
