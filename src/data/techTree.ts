import { TechNode } from '../types/engine';

export const TECH_TREE: TechNode[] = [
  // GRAPHICS
  {
    id: 'gfx_sprites',
    name: '2D Sprite Rendering Engine',
    category: 'graphics',
    description: 'Hardware sprite multiplexing and multi-layer parallax scrolling.',
    unlockYear: 1985,
    researchCost: 5000,
    workPointsRequired: 400,
    progress: 0,
    unlocked: true, // starter
    prerequisites: [],
    statBonus: { qualityBoost: 5 }
  },
  {
    id: 'gfx_isometric',
    name: 'Isometric Tile Projection',
    category: 'graphics',
    description: '2.5D pseudo-3D tile rendering for strategy and RPG worlds.',
    unlockYear: 1988,
    researchCost: 12000,
    workPointsRequired: 800,
    progress: 0,
    unlocked: false,
    prerequisites: ['gfx_sprites'],
    statBonus: { qualityBoost: 8, hypeBonus: 5 }
  },
  {
    id: 'gfx_3d_raster',
    name: '3D Polygon Rasterizer',
    category: 'graphics',
    description: 'Real-time 3D triangle rendering with affine texture mapping.',
    unlockYear: 1993,
    researchCost: 35000,
    workPointsRequired: 1800,
    progress: 0,
    unlocked: false,
    prerequisites: ['gfx_sprites'],
    statBonus: { qualityBoost: 15, hypeBonus: 15 }
  },
  {
    id: 'gfx_shaders',
    name: 'Programmable GPU Shaders',
    category: 'graphics',
    description: 'Dynamic per-pixel lighting, normal mapping, and atmospheric fog.',
    unlockYear: 2003,
    researchCost: 85000,
    workPointsRequired: 3200,
    progress: 0,
    unlocked: false,
    prerequisites: ['gfx_3d_raster'],
    statBonus: { qualityBoost: 22, hypeBonus: 18 }
  },
  {
    id: 'gfx_pbr',
    name: 'Physically Based Rendering (PBR)',
    category: 'graphics',
    description: 'Accurate surface roughness, metallic reflection, and ambient occlusion.',
    unlockYear: 2013,
    researchCost: 180000,
    workPointsRequired: 5500,
    progress: 0,
    unlocked: false,
    prerequisites: ['gfx_shaders'],
    statBonus: { qualityBoost: 30, hypeBonus: 25 }
  },
  {
    id: 'gfx_raytracing',
    name: 'Real-Time Raytracing Engine',
    category: 'graphics',
    description: 'Hardware-accelerated raytraced reflections, shadows, and global illumination.',
    unlockYear: 2019,
    researchCost: 350000,
    workPointsRequired: 9000,
    progress: 0,
    unlocked: false,
    prerequisites: ['gfx_pbr'],
    statBonus: { qualityBoost: 40, hypeBonus: 40 }
  },

  // PHYSICS
  {
    id: 'phys_basic',
    name: 'Bounding Box Collision',
    category: 'physics',
    description: 'Simple axis-aligned bounding box collision detection.',
    unlockYear: 1985,
    researchCost: 3000,
    workPointsRequired: 300,
    progress: 0,
    unlocked: true,
    prerequisites: [],
    statBonus: { qualityBoost: 4 }
  },
  {
    id: 'phys_rigid',
    name: 'Rigid Body Physics Engine',
    category: 'physics',
    description: 'Realistic momentum, friction, gravity, and object stacking.',
    unlockYear: 1997,
    researchCost: 28000,
    workPointsRequired: 1500,
    progress: 0,
    unlocked: false,
    prerequisites: ['phys_basic'],
    statBonus: { qualityBoost: 12, bugReduction: 10 }
  },
  {
    id: 'phys_ragdoll',
    name: 'Ragdoll & IK Animation Dynamics',
    category: 'physics',
    description: 'Procedural character limb physics and inverse kinematics foot placement.',
    unlockYear: 2004,
    researchCost: 65000,
    workPointsRequired: 2800,
    progress: 0,
    unlocked: false,
    prerequisites: ['phys_rigid'],
    statBonus: { qualityBoost: 18, hypeBonus: 10 }
  },
  {
    id: 'phys_destruction',
    name: 'Procedural Destruction Physics',
    category: 'physics',
    description: 'Dynamic wall shattering, debris physics, and destructible terrain.',
    unlockYear: 2012,
    researchCost: 150000,
    workPointsRequired: 5000,
    progress: 0,
    unlocked: false,
    prerequisites: ['phys_ragdoll'],
    statBonus: { qualityBoost: 25, hypeBonus: 20 }
  },

  // AI
  {
    id: 'ai_fsm',
    name: 'Finite State Machine AI',
    category: 'ai',
    description: 'Basic enemy states: patrol, alert, attack, and flee.',
    unlockYear: 1985,
    researchCost: 4000,
    workPointsRequired: 350,
    progress: 0,
    unlocked: true,
    prerequisites: [],
    statBonus: { qualityBoost: 5 }
  },
  {
    id: 'ai_pathfinding',
    name: 'A* Grid Pathfinding',
    category: 'ai',
    description: 'Efficient obstacle avoidance and unit routing across complex maps.',
    unlockYear: 1992,
    researchCost: 18000,
    workPointsRequired: 1100,
    progress: 0,
    unlocked: false,
    prerequisites: ['ai_fsm'],
    statBonus: { qualityBoost: 10, bugReduction: 15 }
  },
  {
    id: 'ai_behavior_trees',
    name: 'Hierarchical Behavior Trees',
    category: 'ai',
    description: 'Modular, highly reactive AI decisions for squads and NPCs.',
    unlockYear: 2005,
    researchCost: 75000,
    workPointsRequired: 3000,
    progress: 0,
    unlocked: false,
    prerequisites: ['ai_pathfinding'],
    statBonus: { qualityBoost: 20, hypeBonus: 12 }
  },
  {
    id: 'ai_neural',
    name: 'Neural Network NPC Memory',
    category: 'ai',
    description: 'Adaptive NPC personalities that remember player actions and evolve tactics.',
    unlockYear: 2021,
    researchCost: 280000,
    workPointsRequired: 8000,
    progress: 0,
    unlocked: false,
    prerequisites: ['ai_behavior_trees'],
    statBonus: { qualityBoost: 35, hypeBonus: 30 }
  },

  // AUDIO
  {
    id: 'aud_chiptune',
    name: '8-Bit Chiptune Synthesizer',
    category: 'audio',
    description: 'Square wave, triangle wave, and noise channel music synthesis.',
    unlockYear: 1985,
    researchCost: 2500,
    workPointsRequired: 250,
    progress: 0,
    unlocked: true,
    prerequisites: [],
    statBonus: { qualityBoost: 5 }
  },
  {
    id: 'aud_cd_stereo',
    name: 'CD-Quality Stereo Audio',
    category: 'audio',
    description: '16-bit 44.1kHz digital audio streaming and voice acting support.',
    unlockYear: 1994,
    researchCost: 22000,
    workPointsRequired: 1200,
    progress: 0,
    unlocked: false,
    prerequisites: ['aud_chiptune'],
    statBonus: { qualityBoost: 12, hypeBonus: 8 }
  },
  {
    id: 'aud_dynamic',
    name: 'Dynamic Adaptive Soundtracks',
    category: 'audio',
    description: 'Music that seamlessly transitions between exploration and intense combat.',
    unlockYear: 2005,
    researchCost: 55000,
    workPointsRequired: 2400,
    progress: 0,
    unlocked: false,
    prerequisites: ['aud_cd_stereo'],
    statBonus: { qualityBoost: 18 }
  },
  {
    id: 'aud_spatial',
    name: '3D Spatial Audio & Acoustic Tracing',
    category: 'audio',
    description: 'Binaural positional sound, reverb occlusion, and raytraced echoes.',
    unlockYear: 2018,
    researchCost: 160000,
    workPointsRequired: 5200,
    progress: 0,
    unlocked: false,
    prerequisites: ['aud_dynamic'],
    statBonus: { qualityBoost: 26, hypeBonus: 15 }
  },

  // NETWORKING
  {
    id: 'net_local',
    name: 'Split-Screen & Local Multiplayer',
    category: 'networking',
    description: 'Up to 4-player couch competitive and cooperative play.',
    unlockYear: 1988,
    researchCost: 8000,
    workPointsRequired: 600,
    progress: 0,
    unlocked: false,
    prerequisites: [],
    statBonus: { qualityBoost: 8, hypeBonus: 10 }
  },
  {
    id: 'net_p2p',
    name: 'Peer-to-Peer Online Netcode',
    category: 'networking',
    description: 'Modem and LAN multiplayer connectivity for online battles.',
    unlockYear: 1996,
    researchCost: 35000,
    workPointsRequired: 1900,
    progress: 0,
    unlocked: false,
    prerequisites: ['net_local'],
    statBonus: { qualityBoost: 15, hypeBonus: 20 }
  },
  {
    id: 'net_dedicated',
    name: 'Dedicated Server & MMO Architecture',
    category: 'networking',
    description: 'Scalable server clusters, database persistence, and rollback netcode.',
    unlockYear: 2004,
    researchCost: 120000,
    workPointsRequired: 4200,
    progress: 0,
    unlocked: false,
    prerequisites: ['net_p2p'],
    statBonus: { qualityBoost: 25, hypeBonus: 30 }
  },

  // WORKFLOW & TOOLS
  {
    id: 'tool_qa_automation',
    name: 'Automated Unit & Bot Testing',
    category: 'workflow',
    description: 'Simulated AI bot playtesters that automatically find bugs during build.',
    unlockYear: 2000,
    researchCost: 45000,
    workPointsRequired: 2000,
    progress: 0,
    unlocked: false,
    prerequisites: [],
    statBonus: { qualityBoost: 10, bugReduction: 40, devSpeedBonus: 15 }
  },
  {
    id: 'tool_hot_reload',
    name: 'Hot-Reload Scripting & Level Editor',
    category: 'workflow',
    description: 'Designers can edit game balance and maps without restarting the engine.',
    unlockYear: 2010,
    researchCost: 95000,
    workPointsRequired: 3500,
    progress: 0,
    unlocked: false,
    prerequisites: ['tool_qa_automation'],
    statBonus: { qualityBoost: 15, devSpeedBonus: 25 }
  }
];

export function getTechNodeById(id: string): TechNode | undefined {
  return TECH_TREE.find(t => t.id === id);
}
