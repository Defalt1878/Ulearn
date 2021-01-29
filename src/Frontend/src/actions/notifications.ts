import { NOTIFICATIONS__COUNT_UPDATED, NotificationsAction } from "src/actions/notifications.types";

export const notificationUpdateAction = (count: number, lastTimestamp: string): NotificationsAction => ({
	type: NOTIFICATIONS__COUNT_UPDATED,
	count,
	lastTimestamp,
});
