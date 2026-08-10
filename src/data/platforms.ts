import { Platform } from '../types/game';

export const PLATFORMS: Platform[] = [
  {
    id: 'pc_dos',
    name: 'PC DOS / Basic 86',
    manufacturer: 'IBM & Clones',
    startYear: 1985,
    endYear: 1996,
    marketShare: 35.0,
    devKitCost: 2500,
    techTier: 1,
    category: 'pc',
    icon: 'Terminal'
  },
  {
    id: 'nes_8bit',
    name: 'Fami-Box 8-Bit',
    manufacturer: 'Nintentoast',
    startYear: 1985,
    endYear: 1994,
    marketShare: 55.0,
    devKitCost: 8000,
    techTier: 1,
    category: 'console',
    icon: 'Gamepad2'
  },
  {
    id: 'snes_16bit',
    name: 'Super Fami-Box 16-Bit',
    manufacturer: 'Nintentoast',
    startYear: 1990,
    endYear: 1999,
    marketShare: 48.0,
    devKitCost: 15000,
    techTier: 2,
    category: 'console',
    icon: 'Gamepad2'
  },
  {
    id: 'genesis_16bit',
    name: 'MegaDrive Ultra',
    manufacturer: 'Segata',
    startYear: 1989,
    endYear: 1998,
    marketShare: 38.0,
    devKitCost: 12000,
    techTier: 2,
    category: 'console',
    icon: 'Gamepad2'
  },
  {
    id: 'ps1_32bit',
    name: 'PolyStation 1',
    manufacturer: 'Soniq',
    startYear: 1994,
    endYear: 2005,
    marketShare: 62.0,
    devKitCost: 25000,
    techTier: 3,
    category: 'console',
    icon: 'Disc'
  },
  {
    id: 'n64_64bit',
    name: 'Ultra-64 Console',
    manufacturer: 'Nintentoast',
    startYear: 1996,
    endYear: 2003,
    marketShare: 28.0,
    devKitCost: 35000,
    techTier: 3,
    category: 'console',
    icon: 'Gamepad2'
  },
  {
    id: 'pc_win',
    name: 'PC Desktop (Win/Linux)',
    manufacturer: 'Open Standard',
    startYear: 1995,
    endYear: 2035,
    marketShare: 45.0,
    devKitCost: 1000,
    techTier: 3,
    category: 'pc',
    icon: 'Monitor'
  },
  {
    id: 'ps2_128bit',
    name: 'PolyStation 2',
    manufacturer: 'Soniq',
    startYear: 2000,
    endYear: 2012,
    marketShare: 70.0,
    devKitCost: 45000,
    techTier: 3,
    category: 'console',
    icon: 'Disc'
  },
  {
    id: 'xbox_original',
    name: 'X-Cube Direct',
    manufacturer: 'Micro-soften',
    startYear: 2001,
    endYear: 2009,
    marketShare: 22.0,
    devKitCost: 30000,
    techTier: 3,
    category: 'console',
    icon: 'Box'
  },
  {
    id: 'ps3_hd',
    name: 'PolyStation 3 HD',
    manufacturer: 'Soniq',
    startYear: 2006,
    endYear: 2016,
    marketShare: 42.0,
    devKitCost: 75000,
    techTier: 4,
    category: 'console',
    icon: 'Disc'
  },
  {
    id: 'xbox_360',
    name: 'X-Cube 360',
    manufacturer: 'Micro-soften',
    startYear: 2005,
    endYear: 2016,
    marketShare: 45.0,
    devKitCost: 65000,
    techTier: 4,
    category: 'console',
    icon: 'Box'
  },
  {
    id: 'mobile_smartphone',
    name: 'Smartphones & Tablets',
    manufacturer: 'Appy & Googol',
    startYear: 2008,
    endYear: 2035,
    marketShare: 85.0,
    devKitCost: 500,
    techTier: 3,
    category: 'mobile',
    icon: 'Smartphone'
  },
  {
    id: 'ps4_gen',
    name: 'PolyStation 4 Pro',
    manufacturer: 'Soniq',
    startYear: 2013,
    endYear: 2024,
    marketShare: 58.0,
    devKitCost: 50000,
    techTier: 4,
    category: 'console',
    icon: 'Disc'
  },
  {
    id: 'switch_hybrid',
    name: 'Nintentoast Switchblade',
    manufacturer: 'Nintentoast',
    startYear: 2017,
    endYear: 2030,
    marketShare: 52.0,
    devKitCost: 20000,
    techTier: 4,
    category: 'handheld',
    icon: 'Gamepad'
  },
  {
    id: 'ps5_gen',
    name: 'PolyStation 5 Ultra',
    manufacturer: 'Soniq',
    startYear: 2020,
    endYear: 2030,
    marketShare: 60.0,
    devKitCost: 80000,
    techTier: 5,
    category: 'console',
    icon: 'Cpu'
  },
  {
    id: 'xbox_series',
    name: 'X-Cube Series X/S',
    manufacturer: 'Micro-soften',
    startYear: 2020,
    endYear: 2030,
    marketShare: 40.0,
    devKitCost: 75000,
    techTier: 5,
    category: 'console',
    icon: 'Box'
  },
  {
    id: 'vr_goggles',
    name: 'Virtual Reality Headset VRX',
    manufacturer: 'Meta-Verse Systems',
    startYear: 2016,
    endYear: 2035,
    marketShare: 18.0,
    devKitCost: 35000,
    techTier: 5,
    category: 'vr',
    icon: 'Glasses'
  }
];

export function getAvailablePlatforms(year: number): Platform[] {
  return PLATFORMS.filter(p => year >= p.startYear && year <= p.endYear);
}

export function getPlatformById(id: string): Platform {
  return PLATFORMS.find(p => p.id === id) || PLATFORMS[6]; // default to PC Win
}
