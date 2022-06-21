import React, { Component } from 'react';
import { Dispatch } from "redux";
import { BrowserRouter } from 'react-router-dom';

import api from "src/api";
import setupStore from "src/setupStore";
import { Provider, connect, } from "react-redux";

import { ThemeContext, Toast } from "ui";
import ErrorBoundary from "src/components/common/ErrorBoundary";
import NotFoundErrorBoundary from "src/components/common/Error/NotFoundErrorBoundary";
import YandexMetrika from "src/components/common/YandexMetrika";
import Header from "src/components/common/Header";
import EmailNotConfirmedModal from "src/components/notificationModal/EmailNotConfirmedModal";
import Router from "src/Router";

import theme from "src/uiTheme";

import { AccountProblemType } from "src/consts/accountProblemType";
import { AccountState } from "src/redux/account";
import { RootState } from "src/models/reduxState";
import { deviceChangeAction } from "src/actions/device";
import { DeviceType } from "src/consts/deviceType";
import { getDeviceType } from "./utils/getDeviceType";

const store = setupStore();

// Update notifications count each minute
setInterval(() => {
	if(store.getState().account.isAuthenticated) {
		api.notifications.getNotificationsCount(store.getState().notifications.lastTimestamp)(store.dispatch);
	}
}, 60 * 1000);

api.setServerErrorHandler(
	(message) => Toast.push(message ? message : 'Произошла ошибка. Попробуйте перезагрузить страницу.'));

function UlearnApp(): React.ReactElement {
	return (
		<Provider store={ store }>
			<ConnectedUlearnApp/>
		</Provider>
	);
}

interface Props {
	account: AccountState;
	getNotificationsCount: () => void;
	getCurrentUser: () => void;
	getCourses: () => void;
	setDeviceType: (deviceType: DeviceType) => void;
	children?: React.ReactNode;
}

interface State {
	initializing: boolean;
	resizeTimeout?: NodeJS.Timeout;
}

class InternalUlearnApp extends Component<Props, State> {
	constructor(props: Props) {
		super(props);
		this.state = {
			initializing: true,
			resizeTimeout: undefined,
		};
	}

	onWindowResize = () => {
		const { resizeTimeout, } = this.state;

		const throttleTimeout = 66;

		//resize event can be called rapidly, to prevent performance issue, we throttling event handler
		if(!resizeTimeout) {
			this.setState({
				resizeTimeout: setTimeout(this.handleResize, throttleTimeout)
			});
		}
	};

	handleResize = () => {
		const { setDeviceType, } = this.props;

		this.setState({
			resizeTimeout: undefined,
		});
		setDeviceType(getDeviceType());
	};

	componentDidMount() {
		const { getCurrentUser, getCourses, } = this.props;
		getCurrentUser();
		getCourses();
		this.setState({
			initializing: false
		});
		window.addEventListener("resize", this.onWindowResize);
	}

	componentDidUpdate(prevProps: Props) {
		const { getNotificationsCount, account, } = this.props;

		if(!prevProps.account.isAuthenticated && account.isAuthenticated) {
			getNotificationsCount();
		}
	}

	componentWillUnmount() {
		window.removeEventListener("resize", this.onWindowResize);
	}


	render() {
		const { initializing, } = this.state;
		const { account } = this.props;

		const pathname = window.location.pathname.toLowerCase();
		const params = new URLSearchParams(window.location.search);
		const isLtiParam = params.get('isLti');
		const isLtiParamLower = params.get('islti');
		const isLti = pathname.endsWith('/ltislide') || (isLtiParam?.toString().toLowerCase() === 'true'); //TODO remove this flag,that hiding header and nav menu
		const isHeaderVisible = !isLti;

		if(isLti) {
			// dirty way to remove padding top from body element (default is 50px, declared in legacy legacy ulearn.global.css main section
			document.body.style.paddingTop = '10px';
		}

		return (
			<BrowserRouter>
				<ThemeContext.Provider value={ theme }>
					<ErrorBoundary>
						{ isHeaderVisible && <Header initializing={ initializing }/> }
						<NotFoundErrorBoundary>
							{ !initializing && // Avoiding bug: don't show page while initializing.
								// Otherwise we make two GET requests sequentially.
								// Unfortunately some our GET handlers are not idempotent (i.e. /Admin/CheckNextExerciseForSlide)
								<Router account={ account }/>
							}
						</NotFoundErrorBoundary>
						{ account
							&& this.isEmailNotConfirmed()
							&& <EmailNotConfirmedModal/>
						}
						{ this.renderMetricsIfNotDevelopment() }
					</ErrorBoundary>
				</ThemeContext.Provider>
			</BrowserRouter>
		);
	}

	isEmailNotConfirmed = () => {
		const { account } = this.props;
		return account.isAuthenticated
			&& account.accountProblems.length > 0
			&& account.accountProblems.some(p => p.problemType === AccountProblemType.emailNotConfirmed);
	};

	renderMetricsIfNotDevelopment = () => {
		if(process.env.NODE_ENV === 'development') {
			return null;
		}
		return <YandexMetrika/>;
	};
}

const mapStateToProps = (state: RootState) => {
	return {
		account: state.account,
	};
};

const mapDispatchToProps = (dispatch: Dispatch) => {
	return {
		getCurrentUser: () => api.account.redux.getCurrentUser()(dispatch),
		getCourses: () => api.courses.getCourses()(dispatch),
		getNotificationsCount: () => api.notifications.getNotificationsCount()(dispatch),
		setDeviceType: (deviceType: DeviceType) => dispatch(deviceChangeAction(deviceType)),
	};
};

const ConnectedUlearnApp = connect(mapStateToProps, mapDispatchToProps)(InternalUlearnApp);

export default UlearnApp;
