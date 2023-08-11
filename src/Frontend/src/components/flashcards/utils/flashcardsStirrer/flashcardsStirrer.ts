import { RateTypes } from "../../../../consts/rateTypes";
import { BaseFlashcard } from "../../../../models/flashcards";

export function sortFlashcardsInAuthorsOrderWithRate(flashcards: BaseFlashcard[]): BaseFlashcard[] {
	const copy = [...flashcards];
	return copy.sort((left, right) => {
		return mapRateTypeToSortingCoefficient[right.rate] - mapRateTypeToSortingCoefficient[left.rate];
	});
}

const mapRateTypeToSortingCoefficient = {
	[RateTypes.notRated]: 6,
	[RateTypes.rate1]: 5,
	[RateTypes.rate2]: 4,
	[RateTypes.rate3]: 3,
	[RateTypes.rate4]: 2,
	[RateTypes.rate5]: 1,
};

export function getNextFlashcardRandomly(
	sequence: BaseFlashcard[],
	maxLastRateIndex: number
): BaseFlashcard | undefined {
	if(!sequence.length) {
		return;
	}

	const probabilities = sequence
		.map(calculateProbability)
		.sort((a, b) => b.probability - a.probability);

	const probabilitiesSum = probabilities.reduce(
		(sum, { probability }) => sum + probability, 0);

	//A randomizer is used to select a card.
	const probabilityThreshold = Math.random() * probabilitiesSum;
	let currentProbability = 0;

	for (const { probability, flashcard } of probabilities) {
		currentProbability += probability;

		if(currentProbability > probabilityThreshold) {
			return flashcard;
		}
	}

	return probabilities[probabilities.length - 1].flashcard;

	function calculateProbability(flashcard: BaseFlashcard) {
		const ratingCoefficient = mapRateTypeToProbabilityCoefficient[flashcard.rate];
		const timeCoefficient = maxLastRateIndex - flashcard.lastRateIndex;

		// For each card, the probability with which it will be shown next is considered.
		// This probability depends on:
		// Card rating;
		// The time that the card did not show ( priority );

		const probability = ratingCoefficient * timeCoefficient * timeCoefficient * timeCoefficient;

		return { probability, flashcard };
	}
}

const mapRateTypeToProbabilityCoefficient: { [rate: string]: number } = {
	[RateTypes.rate1]: 16,
	[RateTypes.rate2]: 8,
	[RateTypes.rate3]: 4,
	[RateTypes.rate4]: 2,
	[RateTypes.rate5]: 1,
};
