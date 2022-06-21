import { applyMiddleware, compose, createStore, Store } from "redux";
import thunkMiddleware from "redux-thunk";
import * as Sentry from "@sentry/react";
import rootReducer from "src/redux/reducers";

export default function setupStore(): Store {
	const env = process.env.NODE_ENV || 'development';
	const isDevelopment = env === 'development';

	let middlewares;
	if(isDevelopment) {
		// eslint-disable-next-line @typescript-eslint/ban-ts-comment
		// @ts-ignore __REDUX_DEVTOOLS_EXTENSION_COMPOSE__ is field added by google chrome browser plugin, see more https://extension.remotedev.io/
		const composeEnhancers = window.__REDUX_DEVTOOLS_EXTENSION_COMPOSE__ || compose;
		middlewares = composeEnhancers(applyMiddleware(thunkMiddleware));

	} else {
		const sentryReduxEnhancer = Sentry.createReduxEnhancer();

		middlewares = compose(applyMiddleware(thunkMiddleware), sentryReduxEnhancer);
	}

	return createStore(
		rootReducer,
		middlewares
	);
}
