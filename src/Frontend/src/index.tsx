import React from 'react';
import { createRoot } from 'react-dom/client';
import UlearnApp from 'src/App';
import * as Sentry from "@sentry/react";
import { Integrations } from "@sentry/tracing";
import '../config/polyfills';
import { register, unregister } from './registerServiceWorker';
import 'moment/locale/ru';
import "moment-timezone";
import { Toast } from "ui";

Sentry.init({
	dsn: "https://62e9c6b9ae6a47399a2b79600f1cacc5@sentry.skbkontur.ru/781",
	integrations: [new Integrations.BrowserTracing()],
});

const container = document.getElementById('root');

if(container) {
	const root = createRoot(container);
	root.render(<UlearnApp/>);
}

if(process.env.NODE_ENV !== 'development') {
	register({
		onUpdate: () =>
			Toast.push("Доступна новая версия ", {
				label: "обновить страницу",
				handler: () => {
					window.location.reload();
				}
			})
	});
}
