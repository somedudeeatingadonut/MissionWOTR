import { CriticReview, GameProject } from '../types/game';
import { getGenreById } from '../data/genres';
import { getThemeById } from '../data/themes';

const CRITIC_OUTLETS = [
  { name: 'IGNited', weight: 1.0, bias: 'action' },
  { name: 'PC Gamerz', weight: 1.05, bias: 'pc' },
  { name: 'Poly-gone', weight: 0.95, bias: 'art' },
  { name: 'GameInformer-ish', weight: 1.0, bias: 'general' }
];

export function generateCriticReviews(project: GameProject): {
  averageScore: number;
  reviews: CriticReview[];
} {
  const genre = getGenreById(project.genreId);
  const theme = getThemeById(project.themeId);
  const isSynergy = genre.synergyThemes.includes(project.themeId);

  const baseQuality = project.qualityScore;
  const reviews: CriticReview[] = [];

  CRITIC_OUTLETS.forEach(outlet => {
    // Variance per critic (-6 to +6)
    const variance = Math.floor(Math.random() * 13) - 6;
    let score = Math.round((baseQuality + variance) * outlet.weight);
    score = Math.max(10, Math.min(100, score));

    const pros: string[] = [];
    const cons: string[] = [];

    // Pros
    if (isSynergy) {
      pros.push(`The combination of ${genre.name} and ${theme.name} works brilliantly.`);
    }
    if (project.polishPoints > 80) {
      pros.push('Exceptional polish and smooth gameplay loops.');
    }
    if (project.progress.art >= project.totalRequirements.art) {
      pros.push('Gorgeous visuals and art direction.');
    }
    if (project.progress.audio >= project.totalRequirements.audio) {
      pros.push('Atmospheric soundtrack and sound design.');
    }
    if (pros.length === 0) {
      pros.push('Decent foundational concept.');
    }

    // Cons
    if (project.bugs > 20) {
      cons.push(`Plagued by technical bugs (${project.bugs} known issues remaining).`);
    } else if (project.bugs > 8) {
      cons.push('Occasional glitches and minor frame drops.');
    }
    if (!isSynergy) {
      cons.push(`The ${theme.name} setting feels odd for a ${genre.name} game.`);
    }
    if (project.techDebt > 35) {
      cons.push('Underlying performance feels sluggish and unoptimized.');
    }
    if (cons.length === 0) {
      cons.push('Few complaints; a solid release.');
    }

    // Summary statement
    let summary = '';
    if (score >= 90) {
      summary = `An absolute masterpiece! A defining moment for ${genre.name} lovers.`;
    } else if (score >= 80) {
      summary = `A fantastic, highly recommended experience with only minor flaws.`;
    } else if (score >= 70) {
      summary = `A solid, enjoyable game that will satisfy fans of the genre.`;
    } else if (score >= 55) {
      summary = `An average effort. Has some neat ideas but is held back by execution.`;
    } else {
      summary = `A disappointing release with major technical and design problems.`;
    }

    reviews.push({
      outlet: outlet.name,
      score,
      summary,
      pros,
      cons
    });
  });

  const averageScore = Math.round(
    reviews.reduce((sum, r) => sum + r.score, 0) / reviews.length
  );

  return { averageScore, reviews };
}
