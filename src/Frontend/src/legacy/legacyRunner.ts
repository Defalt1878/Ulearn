import * as Sentry from "@sentry/react";
import { Toast } from "ui";

$();

//special function running legacy scripts, capturing it, and sending to sentry
export default function runLegacy(code: (() => void) | string | (() => void)[]): void {
	if(Array.isArray(code)) {
		code.forEach(c => runLegacy(c));
	}
	try {
		if(typeof code === 'string') {
			eval(code);
		}
		if(typeof code === 'function') {
			code();
		}
	} catch (error) {
		console.error(error);
		Sentry.captureException(error);
		Toast.push('Произошла ошибка при запуске скриптов. Некоторые действия могут оказаться недоступными.');
	}
}
