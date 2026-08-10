import { Company } from '../types/company';
import { Employee } from '../types/employee';
import { GameProject } from '../types/game';
import { CustomEngine } from '../types/engine';

export interface ScenarioPreset {
  id: string;
  title: string;
  subtitle: string;
  description: string;
  difficulty: 'Easy' | 'Normal' | 'Hard' | 'Sandbox';
  startYear: number;
  startMonth: number;
  company: Company;
  employees: Employee[];
  projects: GameProject[];
  engines: CustomEngine[];
  unlockedTechIds: string[];
}

export const SCENARIO_PRESETS: ScenarioPreset[] = [
  {
    id: 'garage_1990',
    title: '1990: Humble Garage Beginnings',
    subtitle: '16-Bit Golden Era Startup',
    description: 'Start in a cozy suburban garage with $60,000 and a dream. Build classic 2D sprites and compete in the 8-bit and 16-bit console wars!',
    difficulty: 'Normal',
    startYear: 1990,
    startMonth: 3,
    company: {
      name: 'Pixelcraft Studios',
      ceoName: 'Alex Vance',
      ceoSpecialty: 'programmer',
      reputation: 25,
      fanBase: 500,
      cash: 60000,
      officeTierId: 'garage',
      loans: [],
      ledgerHistory: [],
      companyValuation: 120000,
      sharePrice: 1.20,
      totalShares: 100000,
      playerOwnedShares: 100,
      totalGamesReleased: 0,
      gameOfTheYearAwards: 0
    },
    employees: [
      {
        id: 'emp_1',
        name: 'Alex Vance',
        avatarSeed: 'alex',
        role: 'programmer',
        level: 3,
        skills: {
          code: 78,
          design: 65,
          art: 40,
          audio: 45,
          qa: 60,
          marketing: 40,
          management: 70
        },
        intelligence: 85,
        creativity: 80,
        workEthic: 90,
        stressTolerance: 85,
        speed: 80,
        morale: 95,
        stress: 10,
        energy: 100,
        monthlySalary: 1800,
        hiredDate: '1990-01-01',
        traits: ['perfectionist'],
        assignedProjectId: null
      },
      {
        id: 'emp_2',
        name: 'Samantha Lee',
        avatarSeed: 'samantha',
        role: 'artist',
        level: 2,
        skills: {
          code: 35,
          design: 72,
          art: 82,
          audio: 50,
          qa: 55,
          marketing: 60,
          management: 50
        },
        intelligence: 75,
        creativity: 88,
        workEthic: 85,
        stressTolerance: 80,
        speed: 75,
        morale: 90,
        stress: 15,
        energy: 100,
        monthlySalary: 1600,
        hiredDate: '1990-02-01',
        traits: ['hit_maker'],
        assignedProjectId: null
      }
    ],
    projects: [],
    engines: [
      {
        id: 'eng_starter',
        name: 'PixelEngine 1.0',
        version: '1.0',
        unlockedFeatureIds: ['gfx_sprites', 'phys_basic', 'ai_fsm', 'aud_chiptune'],
        platformIds: ['pc_dos', 'nes_8bit'],
        developmentCost: 15000,
        isLicensedToRivals: false,
        licensingPrice: 20000,
        royaltyPercentage: 5,
        totalRoyaltyEarned: 0,
        activeLicensingStudios: [],
        createdAt: '1990-01-15'
      }
    ],
    unlockedTechIds: ['gfx_sprites', 'phys_basic', 'ai_fsm', 'aud_chiptune']
  },
  {
    id: 'studio_2002',
    title: '2002: Midtown 3D Boom',
    subtitle: 'The 128-Bit PolyStation Era',
    description: 'You run an established studio with 8 talented developers in a Midtown office. You have $350,000 in cash and an ambitious 3D Action RPG in active production!',
    difficulty: 'Normal',
    startYear: 2002,
    startMonth: 6,
    company: {
      name: 'Vortex Entertainment',
      ceoName: 'Marcus Thorne',
      ceoSpecialty: 'designer',
      reputation: 68,
      fanBase: 42000,
      cash: 350000,
      officeTierId: 'midtown_studio',
      loans: [
        {
          id: 'loan_1',
          name: 'Studio Expansion Loan',
          principal: 100000,
          remainingAmount: 65000,
          interestRate: 0.07,
          monthlyPayment: 2200,
          remainingMonths: 30
        }
      ],
      ledgerHistory: [],
      companyValuation: 2800000,
      sharePrice: 28.00,
      totalShares: 100000,
      playerOwnedShares: 80,
      totalGamesReleased: 3,
      gameOfTheYearAwards: 0
    },
    employees: [
      {
        id: 'emp_201',
        name: 'Marcus Thorne',
        avatarSeed: 'marcus',
        role: 'designer',
        level: 4,
        skills: { code: 60, design: 92, art: 65, audio: 70, qa: 75, marketing: 80, management: 85 },
        intelligence: 88, creativity: 94, workEthic: 85, stressTolerance: 80, speed: 82,
        morale: 90, stress: 20, energy: 100, monthlySalary: 4500, hiredDate: '1998-05-01',
        traits: ['hit_maker', 'lore_master'], assignedProjectId: 'proj_2002_rpg'
      },
      {
        id: 'emp_202',
        name: 'Elena Rostova',
        avatarSeed: 'elena',
        role: 'programmer',
        level: 4,
        skills: { code: 94, design: 60, art: 45, audio: 50, qa: 80, marketing: 40, management: 70 },
        intelligence: 92, creativity: 75, workEthic: 90, stressTolerance: 85, speed: 88,
        morale: 88, stress: 25, energy: 100, monthlySalary: 4800, hiredDate: '1999-01-10',
        traits: ['speed_demon'], assignedProjectId: 'proj_2002_rpg'
      },
      {
        id: 'emp_203',
        name: 'David Chen',
        avatarSeed: 'david',
        role: 'artist',
        level: 3,
        skills: { code: 40, design: 70, art: 88, audio: 55, qa: 60, marketing: 50, management: 55 },
        intelligence: 80, creativity: 90, workEthic: 85, stressTolerance: 80, speed: 78,
        morale: 92, stress: 15, energy: 100, monthlySalary: 3800, hiredDate: '2000-03-15',
        traits: ['perfectionist'], assignedProjectId: 'proj_2002_rpg'
      },
      {
        id: 'emp_204',
        name: 'Fiona Kelly',
        avatarSeed: 'fiona',
        role: 'audio_engineer',
        level: 3,
        skills: { code: 45, design: 60, art: 50, audio: 91, qa: 70, marketing: 45, management: 50 },
        intelligence: 82, creativity: 88, workEthic: 80, stressTolerance: 75, speed: 75,
        morale: 94, stress: 10, energy: 100, monthlySalary: 3600, hiredDate: '2000-08-01',
        traits: ['team_player'], assignedProjectId: 'proj_2002_rpg'
      },
      {
        id: 'emp_205',
        name: 'Kaito Tanaka',
        avatarSeed: 'kaito',
        role: 'qa_tester',
        level: 3,
        skills: { code: 65, design: 65, art: 55, audio: 60, qa: 93, marketing: 50, management: 60 },
        intelligence: 85, creativity: 70, workEthic: 92, stressTolerance: 88, speed: 85,
        morale: 90, stress: 20, energy: 100, monthlySalary: 3200, hiredDate: '2001-02-14',
        traits: ['bug_hunter'], assignedProjectId: 'proj_2002_rpg'
      },
      {
        id: 'emp_206',
        name: 'Chloe Bennett',
        avatarSeed: 'chloe',
        role: 'marketing',
        level: 3,
        skills: { code: 30, design: 65, art: 60, audio: 50, qa: 55, marketing: 90, management: 75 },
        intelligence: 82, creativity: 85, workEthic: 80, stressTolerance: 82, speed: 80,
        morale: 95, stress: 15, energy: 100, monthlySalary: 3500, hiredDate: '2001-09-01',
        traits: ['coffee_addict'], assignedProjectId: 'proj_2002_rpg'
      }
    ],
    projects: [
      {
        id: 'proj_2002_rpg',
        title: 'Shadows of Aethelgard',
        genreId: 'rpg',
        themeId: 'fantasy',
        audience: 'teen',
        scale: 'aa',
        platformIds: ['ps2_128bit', 'pc_win'],
        targetEngineId: 'eng_vortex3d',
        phase: 'production',
        progress: {
          code: 1400,
          design: 1600,
          art: 1200,
          audio: 600,
          qa: 300
        },
        totalRequirements: {
          code: 2400,
          design: 2800,
          art: 2200,
          audio: 1000,
          qa: 800
        },
        bugs: 42,
        techDebt: 28,
        polishPoints: 110,
        hype: 64,
        crunchMode: 'normal',
        featureCreepCount: 0,
        qualityScore: 78,
        reviewScore: null,
        criticReviews: [],
        developmentCost: 280000,
        revenue: 0,
        unitsSold: 0,
        weeklySalesHistory: [],
        currentPrice: 49.99,
        basePrice: 49.99,
        isLiveService: false,
        serverCapacity: 0,
        activePlayers: 0,
        serverMonthlyCost: 0,
        dlcList: [],
        patchLevel: 0,
        createdAtYear: 2001,
        createdAtMonth: 8,
        createdAtDay: 15
      }
    ],
    engines: [
      {
        id: 'eng_vortex3d',
        name: 'Vortex 3D Engine 2.0',
        version: '2.0',
        unlockedFeatureIds: ['gfx_sprites', 'gfx_3d_raster', 'phys_basic', 'phys_rigid', 'ai_fsm', 'ai_pathfinding', 'aud_chiptune', 'aud_cd_stereo'],
        platformIds: ['pc_win', 'ps2_128bit'],
        developmentCost: 180000,
        isLicensedToRivals: true,
        licensingPrice: 75000,
        royaltyPercentage: 8,
        totalRoyaltyEarned: 140000,
        activeLicensingStudios: ['Capcoming Games'],
        createdAt: '2001-01-10'
      }
    ],
    unlockedTechIds: [
      'gfx_sprites', 'gfx_3d_raster', 'phys_basic', 'phys_rigid',
      'ai_fsm', 'ai_pathfinding', 'aud_chiptune', 'aud_cd_stereo',
      'net_local', 'tool_qa_automation'
    ]
  },
  {
    id: 'enterprise_2024',
    title: '2024: AAA Global Empire',
    subtitle: 'Raytracing, AI, & Live Operations',
    description: 'You command a global game powerhouse with $18,000,000 in cash, 12 elite developers, a custom PBR Raytracing engine, and a live MMORPG bringing in millions per month!',
    difficulty: 'Sandbox',
    startYear: 2024,
    startMonth: 10,
    company: {
      name: 'Aetherion Entertainment',
      ceoName: 'Sophia Sterling',
      ceoSpecialty: 'producer',
      reputation: 94,
      fanBase: 850000,
      cash: 18000000,
      officeTierId: 'campus',
      loans: [],
      ledgerHistory: [],
      companyValuation: 125000000,
      sharePrice: 125.00,
      totalShares: 1000000,
      playerOwnedShares: 65,
      totalGamesReleased: 14,
      gameOfTheYearAwards: 4
    },
    employees: [
      {
        id: 'emp_301',
        name: 'Sophia Sterling',
        avatarSeed: 'sophia',
        role: 'producer',
        level: 5,
        skills: { code: 75, design: 85, art: 80, audio: 80, qa: 85, marketing: 95, management: 98 },
        intelligence: 94, creativity: 88, workEthic: 95, stressTolerance: 92, speed: 90,
        morale: 98, stress: 15, energy: 100, monthlySalary: 12000, hiredDate: '2012-04-01',
        traits: ['team_player', 'hit_maker'], assignedProjectId: 'proj_live_mmo'
      },
      {
        id: 'emp_302',
        name: 'Dr. Viktor Vance',
        avatarSeed: 'viktor',
        role: 'programmer',
        level: 5,
        skills: { code: 99, design: 75, art: 60, audio: 65, qa: 90, marketing: 50, management: 80 },
        intelligence: 98, creativity: 85, workEthic: 92, stressTolerance: 88, speed: 94,
        morale: 94, stress: 20, energy: 100, monthlySalary: 14000, hiredDate: '2014-06-15',
        traits: ['perfectionist', 'speed_demon'], assignedProjectId: 'proj_live_mmo'
      },
      {
        id: 'emp_303',
        name: 'Aria Montgomery',
        avatarSeed: 'aria',
        role: 'artist',
        level: 5,
        skills: { code: 60, design: 85, art: 98, audio: 70, qa: 75, marketing: 65, management: 78 },
        intelligence: 90, creativity: 99, workEthic: 90, stressTolerance: 85, speed: 88,
        morale: 96, stress: 18, energy: 100, monthlySalary: 11000, hiredDate: '2015-02-10',
        traits: ['hit_maker'], assignedProjectId: 'proj_live_mmo'
      }
    ],
    projects: [
      {
        id: 'proj_live_mmo',
        title: 'Chronicles of Aetheria Online',
        genreId: 'mmorpg',
        themeId: 'fantasy',
        audience: 'mature',
        scale: 'aaa',
        platformIds: ['pc_win', 'ps5_gen', 'xbox_series'],
        targetEngineId: 'eng_aether_pbr',
        phase: 'live_service',
        progress: { code: 5000, design: 5000, art: 5000, audio: 3000, qa: 3000 },
        totalRequirements: { code: 5000, design: 5000, art: 5000, audio: 3000, qa: 3000 },
        bugs: 12,
        techDebt: 45,
        polishPoints: 450,
        hype: 92,
        crunchMode: 'normal',
        featureCreepCount: 2,
        qualityScore: 94,
        reviewScore: 93,
        criticReviews: [
          {
            outlet: 'IGNited',
            score: 95,
            summary: 'A breathtaking live world with endless depth, responsive netcode, and stunning raytracing.',
            pros: ['Unmatched graphical fidelity', 'Rich endgame raid design', 'Smooth crossplay netcode'],
            cons: ['High server queue times during launch']
          },
          {
            outlet: 'PC Gamerz',
            score: 92,
            summary: 'The new gold standard for fantasy MMORPGs.',
            pros: ['Incredible soundtrack', 'Rewarding crafting systems'],
            cons: ['Steep learning curve for newcomers']
          }
        ],
        developmentCost: 14500000,
        revenue: 42000000,
        unitsSold: 2800000,
        weeklySalesHistory: [350000, 220000, 180000, 150000, 140000, 135000],
        currentPrice: 59.99,
        basePrice: 59.99,
        isLiveService: true,
        serverCapacity: 500000,
        activePlayers: 320000,
        serverMonthlyCost: 45000,
        dlcList: [
          {
            id: 'dlc_1',
            title: 'Wrath of the Righteous Expansion',
            price: 29.99,
            releaseDate: '2023-11-15',
            unitsSold: 950000,
            revenue: 28490500
          }
        ],
        patchLevel: 14,
        createdAtYear: 2020,
        createdAtMonth: 1,
        createdAtDay: 15,
        releasedAtDate: '2022-10-01'
      }
    ],
    engines: [
      {
        id: 'eng_aether_pbr',
        name: 'Aetherion Unreal-Like 5.0',
        version: '5.0',
        unlockedFeatureIds: [
          'gfx_sprites', 'gfx_3d_raster', 'gfx_shaders', 'gfx_pbr', 'gfx_raytracing',
          'phys_basic', 'phys_rigid', 'phys_ragdoll', 'phys_destruction',
          'ai_fsm', 'ai_pathfinding', 'ai_behavior_trees', 'ai_neural',
          'aud_chiptune', 'aud_cd_stereo', 'aud_dynamic', 'aud_spatial',
          'net_local', 'net_p2p', 'net_dedicated',
          'tool_qa_automation', 'tool_hot_reload'
        ],
        platformIds: ['pc_win', 'ps5_gen', 'xbox_series', 'vr_goggles'],
        developmentCost: 3500000,
        isLicensedToRivals: true,
        licensingPrice: 250000,
        royaltyPercentage: 10,
        totalRoyaltyEarned: 6400000,
        activeLicensingStudios: ['Valved Corporation', 'Square-Enixy Studios'],
        createdAt: '2021-05-20'
      }
    ],
    unlockedTechIds: [
      'gfx_sprites', 'gfx_3d_raster', 'gfx_shaders', 'gfx_pbr', 'gfx_raytracing',
      'phys_basic', 'phys_rigid', 'phys_ragdoll', 'phys_destruction',
      'ai_fsm', 'ai_pathfinding', 'ai_behavior_trees', 'ai_neural',
      'aud_chiptune', 'aud_cd_stereo', 'aud_dynamic', 'aud_spatial',
      'net_local', 'net_p2p', 'net_dedicated',
      'tool_qa_automation', 'tool_hot_reload'
    ]
  }
];

export function getScenarioPreset(id: string): ScenarioPreset {
  return SCENARIO_PRESETS.find(s => s.id === id) || SCENARIO_PRESETS[0];
}
