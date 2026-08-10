import { ThemeConfig } from '../types/game';

export const GAME_THEMES: ThemeConfig[] = [
  {
    id: 'scifi',
    name: 'Science Fiction',
    description: 'Futuristic technologies, starships, laser weaponry, and interstellar exploration.',
    preferredGenres: ['rpg', 'fps', 'rts', 'mmorpg', 'puzzle', 'city_builder']
  },
  {
    id: 'cyberpunk',
    name: 'Cyberpunk & Dystopia',
    description: 'High tech, low life: neon-lit megacities, cybernetic implants, and megacorporation rebellion.',
    preferredGenres: ['rpg', 'fps', 'action_adv', 'mmorpg', 'fighting', 'city_builder']
  },
  {
    id: 'fantasy',
    name: 'High Fantasy',
    description: 'Wizards, dragons, ancient kingdoms, magical relics, and epic quests.',
    preferredGenres: ['rpg', 'rts', 'mmorpg', 'action_adv', 'puzzle', 'fighting']
  },
  {
    id: 'medieval',
    name: 'Medieval & Feudal',
    description: 'Castles, knights, sieges, historical dynastic warfare, and agrarian survival.',
    preferredGenres: ['rts', 'city_builder', 'rpg', 'sim', 'action_adv']
  },
  {
    id: 'post_apoc',
    name: 'Post-Apocalyptic Wasteland',
    description: 'Ruined civilizations, makeshift weapons, wasteland scavengers, and survival instinct.',
    preferredGenres: ['rpg', 'fps', 'horror', 'city_builder', 'sim']
  },
  {
    id: 'zombie',
    name: 'Zombie Outbreak',
    description: 'Relentless undead hordes, barricaded safe houses, and desperate quarantine zones.',
    preferredGenres: ['horror', 'fps', 'action_adv', 'sim']
  },
  {
    id: 'cozy',
    name: 'Cozy Pastoral Farm',
    description: 'Relaxing village life, crop cultivation, animal raising, and heartwarming friendship.',
    preferredGenres: ['sim', 'city_builder', 'puzzle', 'rpg']
  },
  {
    id: 'space',
    name: 'Space Odyssey',
    description: 'Zero-gravity physics, asteroid mining, orbital stations, and galactic trade.',
    preferredGenres: ['sim', 'rts', 'city_builder', 'rpg', 'fps']
  },
  {
    id: 'noir',
    name: 'Noir Detective',
    description: 'Rainy cobblestone streets, jazz saxophones, trench coats, and moral ambiguity.',
    preferredGenres: ['action_adv', 'puzzle', 'rpg', 'horror']
  },
  {
    id: 'mythology',
    name: 'Ancient Mythology',
    description: 'Gods of Olympus, Norse valkyries, legendary titans, and heroic trials.',
    preferredGenres: ['action_adv', 'rpg', 'fighting', 'rts', 'mmorpg']
  },
  {
    id: 'martial_arts',
    name: 'Martial Arts Tournament',
    description: 'Ancient monasteries, secret fight schools, chi energy, and tournament glory.',
    preferredGenres: ['fighting', 'action_adv', 'mmorpg', 'rpg']
  },
  {
    id: 'mecha',
    name: 'Giant Mecha & Anime',
    description: 'Towering robotic suits, anime pilots, orbital defense cannons, and dramatic duels.',
    preferredGenres: ['fighting', 'rts', 'fps', 'action_adv']
  },
  {
    id: 'ww2',
    name: 'World War II',
    description: 'Historical battlefields, authentic tanks, strategic theater planning, and heroic squads.',
    preferredGenres: ['fps', 'rts', 'sim', 'action_adv']
  },
  {
    id: 'crime',
    name: 'Urban Crime Spree',
    description: 'Heists, getaway cars, underworld syndicates, and open-world metropolis chaos.',
    preferredGenres: ['action_adv', 'fps', 'sim', 'city_builder']
  },
  {
    id: 'pirates',
    name: 'High Seas Pirates',
    description: 'Tall ships, naval cannonades, buried treasure islands, and rum-soaked shanties.',
    preferredGenres: ['action_adv', 'rpg', 'sim', 'puzzle']
  },
  {
    id: 'supernatural',
    name: 'Supernatural Occult',
    description: 'Paranormal investigations, ghost hunting, cursed mansions, and occult rituals.',
    preferredGenres: ['horror', 'action_adv', 'puzzle', 'sim']
  }
];

export function getThemeById(id: string): ThemeConfig {
  return GAME_THEMES.find(t => t.id === id) || GAME_THEMES[0];
}
