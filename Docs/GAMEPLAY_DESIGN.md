# Gameplay Design Document

> *"I wanted it to be focused on mining while being chased by aliens. I was inspired by Alien: Dark Descent's fast-paced atmosphere so I wanted it to be real-time strategy."*
> — The Creator's Vision

---

## Table of Contents
1. [Genre & Identity](#genre--identity)
2. [Core Gameplay Loop](#core-gameplay-loop)
3. [Moment-to-Moment Gameplay](#moment-to-moment-gameplay)
4. [The Mining System](#the-mining-system)
5. [Combat & Threat System](#combat--threat-system)
6. [The Daughter as Companion](#the-daughter-as-companion)
7. [Squad Management](#squad-management)
8. [Building System](#building-system)
9. [Progression Structure](#progression-structure)
10. [Available Assets](#available-assets)
11. [Technical Implementation](#technical-implementation)
12. [Design Pillars](#design-pillars)
13. [Open Questions](#open-questions)

---

## Genre & Identity

### Genre Classification
**Real-Time Strategy / Squad Management / Survival**

### Elevator Pitch
Command a squad of miners and soldiers on hostile alien planets. Extract resources while evading or fighting alien threats. Every mission is a tense balance between greed and survival. A father-daughter story that plays out through gameplay, not just cutscenes.

### Primary Gameplay Inspirations

**Alien: Dark Descent**
- Real-time squad tactics against overwhelming threat
- The feeling of being hunted, not hunting
- Tension through resource management and time pressure
- "Fast-paced atmosphere" - always moving, never safe

**Satisfactory**
- Base building and resource extraction
- Production chains and logistics
- Modular construction systems
- The satisfaction of building something permanent in hostile territory

**XCOM**
- Squad management with permanent consequences
- Pre-mission preparation rituals
- High stakes decision making
- The attachment you develop to soldiers

**The Last of Us**
- AI companion that feels present and real
- Relationship that develops through gameplay
- Violence with weight and consequence
- Quiet moments between action

---

## Core Gameplay Loop

```
┌──────────────────────────────────────────────────────────────┐
│                      MISSION SELECT                          │
│   Choose planet, review intel, see danger level              │
└──────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────┐
│                    SQUAD LOADOUT                             │
│   XCOM-style prep: soldiers, gear, daughter's role           │
│   This is where the pre-mission walk-in scene plays          │
└──────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────┐
│                    PLANETARY DROP                            │
│   Dropship lands, squad deploys, fog of war                  │
└──────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────┐
│                      CORE LOOP                               │
│                                                              │
│   1. EXPLORE / SCOUT                                         │
│      - Send scouts to find resource deposits                 │
│      - Discover alien nest locations                         │
│      - Map defensible positions                              │
│                                                              │
│   2. ESTABLISH MINING OPERATION                              │
│      - Deploy mining equipment on resource nodes             │
│      - Set up defensive perimeter                            │
│      - Build structures for protection                       │
│                                                              │
│   3. DEFEND AGAINST WAVES                                    │
│      - Mining creates NOISE → attracts aliens                │
│      - Threat meter builds the longer you mine               │
│      - Real-time tactical combat                             │
│                                                              │
│   4. THE DECISION                                            │
│      - EVAC NOW: Safe, less resources                        │
│      - PUSH FURTHER: More resources, higher risk             │
│      - Every minute is a gamble                              │
│                                                              │
└──────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────┐
│                      EXTRACTION                              │
│   - Call dropship (takes time to arrive)                     │
│   - Survive final wave                                       │
│   - Anyone left behind is LOST                               │
│   - Father + Daughter = essential (game over if lost)        │
└──────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────┐
│                 DEBRIEF / UPGRADE / STORY                    │
│   - Resource tally                                           │
│   - Squad status (injuries, deaths, morale)                  │
│   - Story progression based on choices made                  │
│   - Daughter's reaction to what happened                     │
│   - Upgrade equipment, heal soldiers                         │
└──────────────────────────────────────────────────────────────┘
                              ↓
                         (REPEAT)
```

---

## Moment-to-Moment Gameplay

### Phase 1: Deployment
- Squad drops onto planet surface via dropship
- Limited visibility - fog of war system
- Initial safe zone near drop ship
- Daughter makes a comment about the planet
- Player chooses direction to scout

### Phase 2: Exploration & Setup
- Send scouts ahead to find resource deposits
- Discover alien nest locations (mark to avoid or note for destruction)
- Find defensible positions for mining outpost
- Environmental hazards to navigate (terrain, weather, toxic zones)
- Daughter might notice things soldiers miss

### Phase 3: Mining Operations
- Deploy mining equipment on resource nodes (drills, extractors)
- Mining creates NOISE - core tension mechanic
- **Threat Meter** builds as you mine:
  - Low: Occasional scouts
  - Medium: Patrol groups
  - High: Swarm attacks
  - Critical: Boss creatures emerge
- More resources = more danger = harder decisions
- Must protect miners while they work

### Phase 4: Defense
- Aliens attack in waves based on threat level
- Real-time tactical combat (NOT turn-based)
- Position soldiers at chokepoints
- Use building system for defensive structures
- Father and daughter can have special synergy abilities

### Phase 5: The Decision (Core Tension)
Every mission presents THE choice:
- **EVAC NOW:** Safe extraction, but resources below quota
- **PUSH FURTHER:** Hit quota (or exceed), but risk everything

This creates natural tension. The game never tells you what to do. You feel the weight.

**Consequences of pushing too far:**
- Squad members die (permadeath)
- Daughter witnesses more violence
- Daughter's trust affected
- Story branches based on pattern of choices

**Consequences of playing safe:**
- Fall behind on quotas
- Earth's reconstruction slows
- Aliens/command lose faith
- Different story branches

### Phase 6: Extraction
- Call for evac (dropship takes 2-3 minutes to arrive)
- Survive the extraction wave (creatures make final push)
- Get everyone to the LZ
- **Anyone left behind is GONE**
- Daughter's safety is always the priority

---

## The Mining System

### Resource Types
| Resource | Use | Rarity | Location |
|----------|-----|--------|----------|
| **Common Ore** | Basic construction | Abundant | Surface |
| **Energy Crystals** | Power systems | Medium | Caves |
| **Alien Compounds** | Advanced tech | Rare | Nest proximity |
| **Exotic Matter** | Story progression | Very Rare | Dangerous zones |

### Mining Equipment

**Portable Drill**
- Quick to deploy
- Low yield
- Low noise
- Best for hit-and-run

**Heavy Extractor**
- Slow to deploy
- High yield
- High noise
- Needs defense perimeter

**Automated Harvester**
- Expensive
- Can be left running
- Very high noise
- Draws attacks while you're elsewhere

### The Noise Mechanic

Mining generates noise. Noise fills the threat meter.

```
NOISE SOURCES:
├── Mining Equipment (Primary)
├── Combat (Gunfire attracts more)
├── Building/Construction
├── Generator/Power Systems
└── Movement (Vehicles > Walking > Crouching)

THREAT METER STAGES:
├── CLEAR      [░░░░░░░░░░] Safe... for now
├── DETECTED   [▓▓░░░░░░░░] Scouts spotted
├── ALERTED    [▓▓▓▓░░░░░░] Patrols inbound
├── SWARM      [▓▓▓▓▓▓░░░░] Full assault
└── CRITICAL   [▓▓▓▓▓▓▓▓▓▓] Boss emergence
```

**Design Philosophy:** The player should NEVER feel safe. Even in "CLEAR" state, they know noise is building. Every moment mining is a moment closer to chaos.

---

## Combat & Threat System

### Creature Types (From Protofactor Assets)

**Swarmers (Pistripod)**
- 4 variants available
- Fast, weak individually
- Attack in groups
- Easy to kill, hard to stop all of them

**Flyers (Skorptere)**
- Flying scorpion creature
- Ignores ground defenses
- Need dedicated anti-air
- Swoops in, grabs soldiers if not stopped

**Burrowers (Fugethrox, QuickSandWorm)**
- Tunnel underground
- Bypass walls and barricades
- Pop up inside your perimeter
- Need sensor equipment to detect

**Heavy (Trunckarce, Karcicodus)**
- Large claw monsters
- Soak damage
- Break through walls
- Priority targets

**Bosses**
- **Megaspikan:** Giant spider, spawns larvae during fight
- **Kolossoxenophis:** Giant worm/snake, spawns larvae
- **Pagurolithe:** Hermit crab, shell can be destroyed for faster kill
- **QuickSandWorm:** Burrows, ambushes, environmental hazard

### Combat Mechanics

**Unit Control:**
- Click to select
- Right-click to move/attack
- Drag box for multi-select
- Hotkeys for abilities

**Positioning Matters:**
- High ground advantage
- Cover system (walls, rocks, structures)
- Chokepoints for defense
- Line of sight for ranged

**Weapon Types:**
- Assault rifles (standard)
- Shotguns (close range, swarmers)
- Sniper (priority targets)
- Heavy weapons (bosses, heavies)
- Explosives (area denial)

### The Daughter in Combat
Options (to be decided):
1. **Non-combatant:** Stays back, player must protect
2. **Support:** Healing, buffs, tactical info
3. **Growing combatant:** Starts helpless, becomes capable
4. **Special abilities:** Things only she can do

---

## The Daughter as Companion

### Design Philosophy
She's not an escort mission. She's not a liability. She's a PRESENCE that makes every decision feel witnessed.

### Interaction Types

**Dialogue During Missions:**
- Comments on environment
- Reacts to your tactical choices
- Asks questions about the creatures
- Remembers things from previous missions

**Witnessing:**
- She sees when you leave someone behind
- She sees when you push too hard
- She sees when you play it safe
- Her trust/relationship is affected

**Mechanical Contributions:**
- Can access small spaces (vents, crawlspaces)
- Can hack terminals (if trained)
- Can distract enemies (risky)
- Can heal/stabilize wounded (if trained)
- Special perception (notices things soldiers miss)

### Trust System
Her trust isn't a simple bar. It's tracked through specific moments:

**Trust Increases When:**
- You prioritize her safety
- You show mercy to creatures (if she asks)
- You save squad members
- You're honest about the situation

**Trust Decreases When:**
- You sacrifice others for resources
- You ignore her input
- You're excessively violent
- You lie to her

**Trust Affects:**
- Dialogue options
- Her willingness to take risks
- Story branches
- The ending

### The Forest Question
From Sasha's father in AoT: "Someone has to pull the children out of the forest."

The father is teaching her to survive through violence. Is he saving her or damaging her? This tension should be felt in gameplay.

**Potential Moment:**
She befriends a creature. Refuses to let you kill it. What do you do?

---

## Squad Management

### Squad Composition
- Father (always present, player-controlled lead)
- Daughter (always present, AI-controlled)
- 4-6 additional soldiers (customizable roster)

### Soldier Types
| Class | Role | Weapons | Notes |
|-------|------|---------|-------|
| **Assault** | Front line | Rifles, shotguns | General purpose |
| **Heavy** | Suppression | LMGs, heavy weapons | Slow but devastating |
| **Engineer** | Building/Mining | SMG, tools | Sets up equipment |
| **Medic** | Support | Pistol, med kit | Keeps squad alive |
| **Scout** | Recon | Sniper, pistol | Spots threats early |
| **Mech Pilot** | Vehicle | Mech weapons | If mechs are included |

### Permadeath
When a soldier dies, they're GONE. This creates:
- Attachment through risk
- Weight to every decision
- Stories emerging from loss
- The XCOM feel

### Soldier Progression
- XP from missions
- Unlock abilities
- Upgrade gear
- Personal stories (their "Murphs")

### Morale System
Squad morale affects performance:
- High morale: Bonuses to accuracy, speed
- Low morale: Penalties, chance to panic
- Affected by: Deaths, victories, rest, leadership

---

## Building System

### Current Implementation (Done)

**BuildingPiece ScriptableObject:**
```csharp
- pieceName: string
- prefab: GameObject
- icon: Sprite
- category: BuildingCategory (Floors, Walls, Roofs, Props, Machines)
- snapOffset: Vector3
- canRotate: bool
```

**BuildingSystem Features:**
- Ghost preview (green = valid, red = invalid)
- Grid snapping (configurable size)
- Rotation (R/T keys)
- URP-compatible transparent materials
- Category-based UI

### Building Categories

**Floors/Foundations:**
- Base surfaces for structures
- Ground stabilization
- Floor pieces from SciFi Kit

**Walls:**
- Solid walls
- Window walls (visibility)
- Door walls (access)
- Corner pieces

**Roofs:**
- Cover from aerial attacks
- Weather protection
- Concealment

**Props:**
- Stairs
- Decorative elements
- Functional objects

**Machines (To Implement):**
- Mining equipment (drills, extractors)
- Power generators
- Storage containers
- Turrets/defenses
- Medical stations

### Defensive Structures (To Implement)
- **Barricades:** Quick to build, low HP
- **Walls:** Solid protection, takes time
- **Turrets:** Automated defense, needs power
- **Sensors:** Detect burrowers, early warning
- **Lights:** Improve accuracy, deter some creatures

---

## Progression Structure

### Mission-Level Progression
1. Complete mission objectives
2. Extract with resources
3. Bonus for optional objectives (no casualties, speed, exploration)
4. Penalty for losses (squad deaths, failed objectives)

### Campaign Progression
```
TIER 1: "The Testing"
├── Tutorial missions
├── Learn mechanics
├── Establish squad
└── First hints of larger story

TIER 2: "The Grind"
├── Regular operations
├── Harder planets
├── Squad develops
├── Daughter grows
└── Moral questions emerge

TIER 3: "The Revelation"
├── Edge of Universe discovery
├── Truth about aliens
├── Major story beats
└── Everything changes

TIER 4: "The Reckoning"
├── Final missions
├── The choice
├── Multiple endings
└── Consequences of all decisions
```

### Upgrade Trees

**Squad Upgrades:**
- Better weapons
- Improved armor
- New abilities
- Specializations

**Equipment Upgrades:**
- Faster mining
- Better sensors
- Stronger defenses
- Vehicle access

**Daughter Development:**
- New skills she can learn
- Mechanical contributions
- Story-based growth

**Base/Ship Upgrades:**
- Faster dropship
- More squad slots
- Better intel
- Medical facilities

### Resource Economy
Resources used for:
- Equipment upgrades
- Squad gear
- Base improvements
- Story progression (Earth needs X to rebuild Y)
- Unlocking new regions/planets

---

## Available Assets

### From Protofactor (Characters/Creatures)

**Robots:**
| Robot | Use Case |
|-------|----------|
| AntiRiotDroid | Allied unit with shield |
| Droid-0111 | Scout/support drone |
| LightAssaultMech | Player-controlled vehicle |
| LightMeleeMech | Melee combat |
| LightMiningMech | Mining automation |
| LightQuadrupedMech | Heavy support |
| Quadroid | Combat robot |
| ScoutDroid | Recon drone |

**Creatures (Enemies):**
| Creature | Role |
|----------|------|
| Pistripod (4 variants) | Swarmers |
| Skorptere | Flyer |
| Fugethrox | Burrower |
| Karcarane | Burrower with ranged |
| Karcicodus | Heavy with projectiles |
| Karciniactus | Explosive projectiles |
| Entomochelon | Tank with attacks |
| Trunckarce | Heavy melee |

**Bosses:**
| Boss | Mechanics |
|------|-----------|
| Megaspikan | Spawns larvae |
| Kolossoxenophis | Giant worm, spawns larvae |
| Pagurolithe | Destroyable shell |
| QuickSandWorm | Ambush predator |

**Humanoid Characters:**
| Character | Use |
|-----------|-----|
| SciFi Soldiers (3 camos) | Squad members |
| Heavy Battle Armor (3 camos) | Heavy class |
| Combat Suits (M/F) | Various roles |
| Maintenance Workers (10+ races) | NPCs, civilians |

**Animations Available:**
- Walk, Run, Idle (multiple variants)
- Combat poses (various weapons)
- Crouch variants
- Death animations

### From SciFi Kit (Environment/Props)

**Structures:**
- Walls (plain, door, window, corner)
- Floors (multiple styles)
- Roofs (various)
- Corridors, rooms, modules

**Vehicles:**
- Spaceships (8 types)
- Rovers, Tanks
- Dropship, Escape pods

**Props:**
- Crates, containers, barrels
- Computers, consoles, servers
- Cryo pods, health pods
- Furniture (chairs, tables, beds)
- Holo displays
- Turrets, weapons
- Mining/industrial equipment
- Stargates (!)

**Environment:**
- Mars surface tiles
- Asteroids
- Planets
- Skyboxes

### From Elite Soldiers
- 4 soldier variants (static mesh, needs animation)
- Dog with animations (Idle, Walk, Run, Get_Hit, Dead)
- Drone
- Backpacks, weapons

### From Exo-planets
- Planet rendering
- Atmosphere effects
- Use for mission select screen, space views

---

## Technical Implementation

### Platform & Engine
- **Engine:** Unity 6 (6000.0.23f1)
- **Render Pipeline:** Universal Render Pipeline (URP)
- **Input:** New Input System

### Current Implementation Status

**Done ✓**
- Building system with grid snapping
- BuildingPiece ScriptableObject structure
- Categories: Floors, Walls, Roofs, Props, Machines
- Ghost preview with valid/invalid materials (URP)
- Building UI with category tabs (UI Toolkit)
- First-person controller (for story scenes)
- Squad preview scene (XCOM-style walk-in)
- URP material upgrade tooling
- Git version control setup

**In Progress:**
- Squad animation integration
- Scene structure

**Not Started:**
- RTS camera and controls
- Unit selection and movement
- Alien AI and spawning
- Mining mechanics
- Resource collection and storage
- Threat/noise system
- Mission structure and objectives
- Save/load system
- Story integration and dialogue
- Daughter companion system
- Permadeath and squad management
- Between-mission hub
- Audio/music

### Code Architecture
- **ScriptableObjects** for data (BuildingPiece, SoldierData, ResourceType)
- **MonoBehaviours** for runtime logic (BuildingSystem, SquadController)
- **Editor scripts** for tooling (BuildingPieceGenerator, SquadPreviewSetup)
- **UI Toolkit** for runtime UI

### Key Scripts

| Script | Purpose | Location |
|--------|---------|----------|
| BuildingSystem.cs | Ghost preview, placement | Scripts/Building/ |
| BuildingPiece.cs | ScriptableObject for pieces | Scripts/Building/ |
| BuildingUI.cs | Building menu UI | Scripts/Building/ |
| BuildingUI_Toolkit.cs | UI Toolkit version | Scripts/Building/ |
| SquadPreviewScene.cs | XCOM walk-in scene | Scripts/UI/ |
| FirstPersonController.cs | Story scene camera | Scripts/ |

---

## Design Pillars

### 1. Tension Through Choice
Every decision should matter. Stay or go? Fight or flee? Save the soldier or the resources? The game never tells you what's right.

### 2. Earned Resources
Nothing comes free. Risk scales with reward. Players should FEEL the cost of every crystal extracted.

### 3. Squad as Family
These aren't disposable units. Each loss should sting. Names, personalities, "Murphs" - we care because we know them.

### 4. The Daughter is the Moral Weight
She's not a mechanic. She's a WITNESS. Every choice is filtered through "what will she see? what will she think?"

### 5. Moment-to-Moment Engagement
Even mining should be tense. The threat meter ticking up. The distant alien screech. The question of "one more deposit?"

### 6. Cool Factor Matters
"Cart Titan with armor" energy. Dark Descent atmosphere. Moments that make you go "that was sick."

---

## Control Scheme

### Mouse + Keyboard (Primary)

**Camera:**
- WASD or screen edge for pan
- Mouse wheel for zoom
- Middle mouse for rotate
- Space to center on selection

**Unit Control:**
- Left click: Select
- Right click: Move/Attack
- Shift+click: Add to selection
- Ctrl+click: Remove from selection
- Drag box: Area select

**Building:**
- B: Toggle build menu
- R/T: Rotate piece
- Left click: Place
- Right click: Cancel
- Tab: Cycle categories

**Hotkeys:**
- 1-6: Select squad members
- F: Focus on daughter
- E: Extraction call
- Q: Quick ability
- Tab: Pause/tactical view

---

## Open Design Questions

### Core
- Turn-based vs real-time combat? (Leaning real-time)
- Permadeath for all soldiers or narrative consequences only?
- How punishing should mission failure be?
- Base building between missions or just upgrades?

### Daughter
- How does she differ mechanically from squad?
- Is she controllable or AI-only?
- What happens if she dies? (Game over? Major consequence?)
- How explicit is the trust system?

### Scope
- How many planets/missions?
- How long is the campaign?
- Replayability through choices or roguelike elements?
- Multiplayer potential? Co-op?

### Tone
- How violent/graphic?
- How dark can story moments get?
- Does the dog from Elite Soldiers appear? (emotional stakes!)

---

## Next Implementation Steps

1. **RTS Camera** - Top-down camera with pan/zoom
2. **Unit Selection** - Click/drag select for squads
3. **Basic Movement** - Point-and-click navigation
4. **Creature Spawning** - Basic wave system
5. **Mining Prototype** - Place drill, generate resources, create noise
6. **Threat Meter** - Visual indicator of danger level
7. **Extraction** - Call dropship, survive, escape
8. **First Playable Loop** - One complete mission start to finish

---

*This document evolves with development. Update as systems are implemented.*

*"I wanted it to be focused on mining while being chased by aliens. Real-time strategy. Something that makes you feel the tension of Alien: Dark Descent."*
