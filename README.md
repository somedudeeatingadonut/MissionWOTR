# DevTycoon Studio — Complex Game Development Simulation

> **Note on Pivot:** While this repository originally started as `MissionWOTR` (a Pathfinder: Wrath of the Righteous vibe-coded mod concept), that idea was scrapped in favor of building a **foundation for a complicated, complex game development tycoon game**.

`DevTycoon Studio` is a deep, immersive, and fully interactive game studio management simulator built with **React**, **TypeScript**, **Tailwind CSS**, and **Zustand**, featuring a multi-phase development pipeline, proprietary engine technology research, live-service MMORPG server operations, and simulated competitor studios.

---

## ✨ Features & Complexity Deep Dive

### 1. 🕒 Deterministic Simulation Engine & Calendar
- **Multi-Speed Game Clock:** Pause (`0x`), Normal (`1x`), Fast (`2x`), or Ultra (`5x`) speed with day/week/month/year ticks.
- **Monthly Financial Ledgers:** Automated calculations for employee payroll, office rent, server maintenance, bank credit lines, and share valuation.
- **Interactive Studio Dilemmas:** Dynamic events and industry crises (e.g., leaked alpha gameplay, publisher acquisition offers, energy drink sponsorships) with multi-choice trade-offs.

### 2. 🏢 Office Tiers & Facility Management
- **5 Progressive Studio Tiers:**
  1. *Founder's Garage* ($300/mo, 4 desks)
  2. *Downtown Indie Loft* ($2,800/mo, 10 desks)
  3. *Midtown Game Studio* ($9,500/mo, 25 desks)
  4. *High-Tech Studio Campus* ($35,000/mo, 60 desks)
  5. *AAA Enterprise Skyscraper HQ* ($120,000/mo, 150 desks)
- **Built-in Amenities:** Employee Break Rooms (stamina & energy regeneration), QA Labs (bug reduction), and Server Racks (MMO player capacity).

### 3. 👥 Deep Employee & Team Mechanics
- **7 Roles:** Programmer, Designer, 3D/2D Artist, Audio Engineer, QA Tester, Marketing Specialist, Producer.
- **7 Core Skills & 5 Personal Attributes:** Coding, Design, Art, Audio, QA, Marketing, Management, Intelligence, Creativity, Work Ethic, Stress Tolerance, Speed.
- **Dynamic State:** Energy depletion during work hours, stress accumulation, and morale tracking.
- **Unique Traits:** *"Spaghetti Coder"* (+speed, +bugs/tech debt), *"Perfectionist"* (-speed, +polish, -bugs), *"Bug Hunter"*, *"Hit Maker"*, *"Crunch Resistant"*, *"Speed Demon"*, *"Coffee Addict"*, and *"Lore Master"*.
- **Training Academy:** Invest capital to upskill employees in coding, design, art, and testing.

### 4. 🎮 Multi-Phase Development Pipeline & Crunch Policy
- **10 Distinct Game Genres & 16 Themes:** Each genre has tailored work requirement distributions (e.g., RPG requires heavy coding and design; FPS requires heavy coding and art) and theme synergy matrices.
- **Tech Debt & Technical Bugs:** Rushing features generates tech debt, which degrades future coding speed and increases bug frequency. QA testers fix bugs and remove tech debt while generating Polish points.
- **Crunch Mode Toggle:**
  - *Normal:* Sustainable 1x development pace.
  - *Mild Overtime:* +25% work speed, +40% stress, +25% bug chance.
  - *Heavy Crunch:* +60% work speed, +120% stress, +60% bug chance, and morale penalties.
- **Feature Creep:** Dynamically expand project scope mid-development to boost potential hype at the cost of budget and schedule.

### 5. 📰 Magazine Critic Review Engine & Post-Launch Economics
- **Magazine Outlets:** *"IGNited"*, *"PC Gamerz"*, *"Poly-gone"*, and *"GameInformer-ish"*.
- **Review Reveal Modal:** Evaluates genre-theme synergy, polish points, unresolved bugs, and tech debt to generate a final average score, pros/cons, and critic quotes.
- **Sales Curve Simulation:** Strong launch week spike driven by Hype and Fanbase, followed by a word-of-mouth tail influenced by review scores, platform hardware market share, and retail price.

### 6. 🌐 Live-Service MMORPG Operations & DLC Expansions
- **Persistent Server Capacity:** Manage server costs and concurrent players for live-service MMO titles.
- **Expansion Packs:** Release downloadable content (DLC) packages for released titles to re-engage fans and generate additional revenue.

### 7. 🔬 Research Tech Tree & Proprietary Game Engine Licensing
- **Over 20 Unlockable Technologies** across 6 categories:
  - *Graphics:* 2D Sprites, Isometric, 3D Rasterization, Shaders, PBR, Real-Time Raytracing.
  - *Physics:* Bounding Box, Rigid Body, Ragdoll & IK, Procedural Destruction.
  - *AI:* FSM, A* Pathfinding, Hierarchical Behavior Trees, Neural Net NPCs.
  - *Audio:* 8-Bit Chiptune, CD Stereo, Dynamic Adaptive Soundtracks, 3D Spatial Audio.
  - *Networking:* Split-Screen, P2P Netcode, Dedicated Server MMO Architecture.
  - *Workflow:* Automated Bot Testing, Hot-Reload Scripting.
- **Custom Engine Creator:** Bundle unlocked features into proprietary engines and charge AI rival studios upfront fees and royalty percentages.

### 8. 🏆 Industry Ecosystem & 10 Simulated Rival Studios
- **Simulated Competitors:** Compare market share, cash reserves, reputation, and latest game scores against *"Electronic Artsy"*, *"Blizzarding Entertainment"*, *"iD Softwork"*, *"Valved Corporation"*, *"Square-Enixy"*, *"Rockstarve North"*, *"Nintentoast EAD"*, and *"Capcoming Games"*.
- **Hardware Eras:** Platforms range from 1985 PC DOS and 8-Bit consoles to modern 3D raytracing hardware and VR goggles.

---

## 🚀 Getting Started

### Installation & Development Server
```bash
# Install dependencies
npm install

# Start the Vite development server (binds to 0.0.0.0:3000)
npm run dev
```

### Build for Production
```bash
# Typecheck & bundle for production
npm run build

# Preview the built production bundle
npm run preview
```

---

## 📁 Codebase Architecture

```
src/
  ├── types/
  │    ├── game.ts            # Project types, phases, genres, themes, platforms, reviews, DLC, live ops
  │    ├── employee.ts        # Employee skills, traits, roles, attributes, morale, salaries
  │    ├── engine.ts          # Tech tree nodes, research progress, proprietary engine licensing
  │    ├── company.ts         # Company stats, financial ledger, office tiers, bank loans, share value
  │    ├── market.ts          # Market trends, rival studios, industry news, awards
  │    └── simulation.ts      # Time controls, interactive dilemmas, toast notifications
  ├── data/
  │    ├── genres.ts          # 10 game genres with work distributions & synergy themes
  │    ├── themes.ts          # 16 themes with descriptions and genre affinities
  │    ├── platforms.ts       # Hardware platforms (1985–2030) with specs & market shares
  │    ├── techTree.ts        # Comprehensive R&D tech tree across Graphics, Physics, AI, Audio, Net, Tools
  │    ├── employeeTraits.ts  # 10 unique traits with gameplay stat modifiers
  │    ├── rivalStudios.ts    # 10 simulated competitor studios
  │    ├── randomEvents.ts    # Interactive studio dilemmas and choices
  │    ├── offices.ts         # 5 office tiers from Garage to Enterprise HQ
  │    └── presets.ts         # 3 historical starting scenarios (1990 Garage, 2002 AA Studio, 2024 AAA Empire)
  ├── engine/
  │    ├── simulationEngine.ts  # Master daily simulation tick loop & time advancement
  │    ├── devPipeline.ts       # Daily work progress, bug generation, QA fixing, polish calculation
  │    ├── reviewEngine.ts      # Critic review generator with pros/cons & quotes
  │    ├── salesEngine.ts       # Weekly sales demand curves, hype decay, MMO retention
  │    ├── financeEngine.ts     # Monthly P&L ledger, payroll, rent, server overhead, valuation
  │    └── soundEngine.ts       # Web Audio API sound synthesizer (cash, click, award, alert, bug)
  ├── store/
  │    └── useGameStore.ts      # Zustand state store with persistence & JSON export/import
  └── components/
       ├── layout/              # Navbar, Sidebar, Notification Toast stack
       ├── studio/              # StudioFloorView (interactive desks & employees), EventModal
       ├── projects/            # ProjectListView, NewGameWizard, ReviewModal
       ├── staff/               # EmployeeListView, HiringModal
       ├── tech/                # TechTreeView, EngineCreatorModal
       ├── market/              # MarketTrendsView (platforms, trends, rival studios leaderboard)
       ├── finance/             # FinanceDashboard (P&L tables, bank loans, share valuation)
       └── common/              # ScenarioModal (switch 1990/2002/2024), SaveLoadModal
```

---

## 🎮 Playing the Scenarios

You can switch between three built-in starting scenarios at any time via the **Scenarios** button in the top navigation bar:
1. **1990: Humble Garage Beginnings** — Start with $60,000, two founders in a suburban garage, and compete in the 8-Bit and 16-Bit console wars.
2. **2002: Midtown 3D Boom** — Manage an established studio with 8 developers, $350,000 in cash, and an active 3D RPG in production for the 128-Bit PolyStation.
3. **2024: AAA Global Empire** — Lead a massive enterprise campus with $18M in cash, proprietary raytracing engines, and a live MMORPG world generating millions per month.
