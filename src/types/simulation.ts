export type GameSpeed = 0 | 1 | 2 | 5; // 0=paused, 1=1x, 2=2x, 5=5x speed

export interface GameDate {
  year: number;
  month: number; // 1 to 12
  day: number;   // 1 to 30
}

export interface InteractiveChoice {
  label: string;
  description: string;
  cost?: number;
  effect: {
    moraleChange?: number;
    cashChange?: number;
    hypeChange?: number;
    reputationChange?: number;
    techDebtChange?: number;
    bugChange?: number;
    customMessage?: string;
  };
}

export interface DynamicEvent {
  id: string;
  title: string;
  description: string;
  category: 'opportunity' | 'crisis' | 'industry' | 'employee';
  choices: InteractiveChoice[];
  createdAtDate: string; // YYYY-MM-DD
}

export interface ToastNotification {
  id: string;
  title: string;
  message: string;
  type: 'info' | 'success' | 'warning' | 'error' | 'award';
  timestamp: string;
}
