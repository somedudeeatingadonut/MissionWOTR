import { GenreConfig } from '../types/game';

export const GAME_GENRES: GenreConfig[] = [
  {
    id: 'rpg',
    name: 'Role-Playing (RPG)',
    description: 'Deep storytelling, character progression, expansive worlds, and dialogue trees.',
    baseCostMultiplier: 1.4,
    workDistribution: {
      code: 30,
      design: 35,
      art: 20,
      audio: 10,
      qa: 5
    },
    preferredAudience: ['teen', 'mature', 'hardcore'],
    synergyThemes: ['fantasy', 'scifi', 'cyberpunk', 'post_apoc', 'mythology', 'space']
  },
  {
    id: 'fps',
    name: 'First-Person Shooter (FPS)',
    description: 'High-octane gunplay, cutting-edge 3D graphics, and competitive multiplayer.',
    baseCostMultiplier: 1.35,
    workDistribution: {
      code: 35,
      design: 15,
      art: 35,
      audio: 10,
      qa: 5
    },
    preferredAudience: ['teen', 'mature', 'hardcore'],
    synergyThemes: ['scifi', 'cyberpunk', 'ww2', 'zombie', 'crime', 'space']
  },
  {
    id: 'rts',
    name: 'Real-Time Strategy (RTS)',
    description: 'Base building, unit micromanagement, tactical combat, and complex pathfinding.',
    baseCostMultiplier: 1.25,
    workDistribution: {
      code: 40,
      design: 30,
      art: 15,
      audio: 10,
      qa: 5
    },
    preferredAudience: ['teen', 'hardcore'],
    synergyThemes: ['scifi', 'fantasy', 'medieval', 'ww2', 'space', 'mecha']
  },
  {
    id: 'action_adv',
    name: 'Action-Adventure',
    description: 'Cinematic set pieces, environmental traversal, combat, and immersive storytelling.',
    baseCostMultiplier: 1.3,
    workDistribution: {
      code: 25,
      design: 25,
      art: 35,
      audio: 10,
      qa: 5
    },
    preferredAudience: ['everyone', 'teen', 'mature'],
    synergyThemes: ['pirates', 'mythology', 'cyberpunk', 'supernatural', 'martial_arts', 'crime']
  },
  {
    id: 'sim',
    name: 'Simulation & Sandbox',
    description: 'Realistic physics, open-ended systems, emergent gameplay, and detailed mechanics.',
    baseCostMultiplier: 1.15,
    workDistribution: {
      code: 45,
      design: 25,
      art: 20,
      audio: 5,
      qa: 5
    },
    preferredAudience: ['everyone', 'teen', 'hardcore'],
    synergyThemes: ['cozy', 'space', 'pirates', 'medieval', 'supernatural', 'crime']
  },
  {
    id: 'city_builder',
    name: 'City Builder & Tycoon',
    description: 'Economic simulations, traffic flow algorithms, infrastructure planning, and optimization.',
    baseCostMultiplier: 1.2,
    workDistribution: {
      code: 45,
      design: 30,
      art: 15,
      audio: 5,
      qa: 5
    },
    preferredAudience: ['everyone', 'teen', 'hardcore'],
    synergyThemes: ['cozy', 'scifi', 'medieval', 'post_apoc', 'space', 'cyberpunk']
  },
  {
    id: 'mmorpg',
    name: 'MMORPG / Live Service',
    description: 'Persistent online server worlds, raids, guilds, server economies, and live updates.',
    baseCostMultiplier: 2.2,
    workDistribution: {
      code: 40,
      design: 25,
      art: 25,
      audio: 5,
      qa: 5
    },
    preferredAudience: ['teen', 'mature', 'hardcore'],
    synergyThemes: ['fantasy', 'scifi', 'mythology', 'cyberpunk', 'martial_arts', 'space']
  },
  {
    id: 'horror',
    name: 'Survival Horror',
    description: 'Tense atmosphere, scarce ammunition, terrifying soundscapes, and psychological dread.',
    baseCostMultiplier: 1.1,
    workDistribution: {
      code: 20,
      design: 25,
      art: 30,
      audio: 20,
      qa: 5
    },
    preferredAudience: ['mature', 'hardcore'],
    synergyThemes: ['zombie', 'supernatural', 'post_apoc', 'scifi', 'noir', 'mythology']
  },
  {
    id: 'puzzle',
    name: 'Puzzle & Platformer',
    description: 'Tight controls, clever mechanical brain teasers, colorful art, and addictive loops.',
    baseCostMultiplier: 0.85,
    workDistribution: {
      code: 25,
      design: 40,
      art: 25,
      audio: 5,
      qa: 5
    },
    preferredAudience: ['everyone', 'teen'],
    synergyThemes: ['cozy', 'fantasy', 'scifi', 'supernatural', 'pirates', 'mythology']
  },
  {
    id: 'fighting',
    name: 'Fighting Game',
    description: 'Frame-perfect animation, balanced combat rosters, and competitive netcode.',
    baseCostMultiplier: 1.15,
    workDistribution: {
      code: 30,
      design: 20,
      art: 35,
      audio: 10,
      qa: 5
    },
    preferredAudience: ['teen', 'mature', 'hardcore'],
    synergyThemes: ['martial_arts', 'mecha', 'fantasy', 'cyberpunk', 'mythology', 'scifi']
  }
];

export function getGenreById(id: string): GenreConfig {
  return GAME_GENRES.find(g => g.id === id) || GAME_GENRES[0];
}
