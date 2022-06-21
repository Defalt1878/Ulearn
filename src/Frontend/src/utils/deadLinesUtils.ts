import { DeadLineInfo, DeadLineSlideType } from "src/models/deadLines";
import { momentFromServerToLocal, momentToServerFormat, } from "./momentUtils";
import moment from "moment-timezone";

export function isDeadLineOverlappedByAnother(deadLine: DeadLineInfo, deadLines: DeadLineInfo[]): boolean {
	const currentDeadLine = getDeadLine(deadLines, momentFromServerToLocal(deadLine.date));

	return currentDeadLine.current?.id !== deadLine.id;
}

export function getDeadLineForStudent(
	deadLines: DeadLineInfo[],
	studentIds: string[] | null,
): DeadLineSchedule {
	deadLines = deadLines.filter(
		d => d.userIds === null || d.userIds.every(userId => studentIds?.includes(userId)));

	return getDeadLine(deadLines);
}

export function getDeadLineForSlide(
	deadLines: DeadLineInfo[],
	slideScoringGroupId: string | null,
	slideId: string,
	unitId: string,
	curMoment = moment(),
): DeadLineSchedule {
	deadLines = deadLines.filter(
		d => d.unitId === unitId && (
			d.slideType === DeadLineSlideType.All ||
			d.slideType === DeadLineSlideType.ScoringGroupId && d.slideValue === slideScoringGroupId ||
			d.slideType === DeadLineSlideType.SlideId && d.slideValue === slideId));

	return getDeadLine(deadLines, curMoment);
}

export interface DeadLineSchedule {
	current: DeadLineInfo | null;
	next: DeadLineInfo | null;
}

export function getDeadLine(deadLines: DeadLineInfo[], curMoment = moment()): DeadLineSchedule {
	if(deadLines.length === 0) {
		return {
			current: null,
			next: null,
		};
	}
	const convertedDeadLines = deadLines
		.map(d => ({ ...d, date: momentFromServerToLocal(d.date) }))
		.sort((d1, d2) => {
			const diff = d2.date.diff(d1.date);

			if(diff !== 0) {
				return diff;
			}

			return d2.scorePercent - d1.scorePercent;
		});

	const inactiveDeadLines = convertedDeadLines
		.filter(d => d.date.diff(curMoment) > 0);
	const activeDeadLines = convertedDeadLines
		.filter(d => d.date.diff(curMoment) <= 0);
	const lastActiveWithMoment = activeDeadLines ? activeDeadLines[0] : undefined;

	let next = null;
	let current = null;

	if(lastActiveWithMoment) {
		current = { ...lastActiveWithMoment, date: momentToServerFormat(lastActiveWithMoment.date) };
	}

	if(inactiveDeadLines.length > 0) {
		const maxScoreAmongInActive = Math.max(...inactiveDeadLines.map(d => d.scorePercent));
		next = inactiveDeadLines.find(d => d.scorePercent == maxScoreAmongInActive)!;
		if(next) {
			next = { ...next, date: momentToServerFormat(next.date) };
		}
	}

	const isCurrentOverlapped = current && next && next.scorePercent >= current.scorePercent;

	return {
		current: isCurrentOverlapped ? null : current,
		next: next,
	};
}
