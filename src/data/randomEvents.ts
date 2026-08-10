import { DynamicEvent } from '../types/simulation';

export const DYNAMIC_EVENTS: DynamicEvent[] = [
  {
    id: 'e3_leak',
    title: 'Unfinished Game Gameplay Leaked Online!',
    description: 'An anonymous leaker has posted a blurry video of your game in alpha state! Fans are debating whether the graphics look "dated" or if it is just an early build.',
    category: 'crisis',
    createdAtDate: '1992-06-15',
    choices: [
      {
        label: 'Embrace the Leak & Drop an Official Trailer',
        description: 'Spend $10,000 on high-res rendering and marketing to turn the narrative around.',
        cost: 10000,
        effect: {
          hypeChange: 25,
          moraleChange: 5,
          customMessage: 'The official trailer went viral! Hype is through the roof.'
        }
      },
      {
        label: 'Issue a Legal Takedown & Stay Silent',
        description: 'Costless, but may annoy hardcore fans who dislike censorship.',
        effect: {
          hypeChange: -5,
          reputationChange: -3,
          customMessage: 'The video was removed, but fans grumbled about the secrecy.'
        }
      },
      {
        label: 'Release a Playable Alpha Demo to Proving Ground',
        description: 'Let gamers try it! Boosts Hype significantly if bugs are low, but risks exposure.',
        effect: {
          hypeChange: 35,
          bugChange: 5,
          customMessage: 'Players loved testing the early demo and reported helpful feedback!'
        }
      }
    ]
  },
  {
    id: 'publisher_offer',
    title: 'Major Publisher Offers Funding & Distribution',
    description: 'A global game publisher offers an upfront $150,000 cash injection for your current project, but demands 30% of your future royalties and stricter crunch deadlines.',
    category: 'opportunity',
    createdAtDate: '1993-04-10',
    choices: [
      {
        label: 'Accept the Deal & Take the Money',
        description: 'Receive $150,000 immediately, but lose some reputation for selling out.',
        effect: {
          cashChange: 150000,
          reputationChange: -5,
          hypeChange: 15,
          customMessage: 'Your bank account is overflowing, and the publisher launched an ad campaign!'
        }
      },
      {
        label: 'Politely Decline and Remain Independent',
        description: 'Keep 100% of your royalties and gain indie street credibility.',
        effect: {
          reputationChange: 10,
          moraleChange: 8,
          customMessage: 'Your studio cheered! Independence is priceless.'
        }
      }
    ]
  },
  {
    id: 'engine_breakthrough',
    title: 'Lead Programmer Discovers Rendering Optimization',
    description: 'During a late-night coding session, your lead programmer found a way to compress textures without quality loss!',
    category: 'opportunity',
    createdAtDate: '1994-08-20',
    choices: [
      {
        label: 'Apply it to Reduce Tech Debt & Bugs',
        description: 'Refactor the engine codebase to make development smoother.',
        effect: {
          techDebtChange: -30,
          bugChange: -20,
          customMessage: 'The codebase has never been cleaner!'
        }
      },
      {
        label: 'Push the Graphics to the Absolute Limit',
        description: 'Use the headroom to add extra visual flare and gain Hype.',
        effect: {
          hypeChange: 20,
          moraleChange: 5,
          customMessage: 'The visuals look stunning! Gamers are taking notice.'
        }
      }
    ]
  },
  {
    id: 'energy_drink_sponsor',
    title: 'Energy Drink Sponsorship Offer',
    description: 'A popular beverage company wants to put a vending machine in your studio and sponsor your next launch stream.',
    category: 'opportunity',
    createdAtDate: '1995-02-14',
    choices: [
      {
        label: 'Accept the Sponsorship ($25,000)',
        description: 'Gain instant cash and a morale boost from free caffeine.',
        effect: {
          cashChange: 25000,
          moraleChange: 10,
          customMessage: 'Free energy drinks for everyone! Team energy recovery boosted.'
        }
      },
      {
        label: 'Reject Corporate Endorsements',
        description: 'Keep the office clean of ads.',
        effect: {
          reputationChange: 4,
          customMessage: 'You stayed true to pure game development.'
        }
      }
    ]
  }
];
