import React, { Component } from "react";
import { useNavigate } from "react-router-dom";
import cn from "classnames";

import { notificationResetAction } from "src/actions/notifications";

import { DropdownContainer } from "@skbkontur/react-ui/internal/DropdownContainer";
import NotificationsIcon from "./NotificationsIcon";
import Notifications from "./Notifications";

import { connect } from "react-redux";

import { feed, notificationsFeed, WithNavigate, WithNavigateProvided } from "src/consts/routes";
import { DeviceType } from "src/consts/deviceType";

import { Dispatch } from "redux";
import { RootState } from "src/models/reduxState";

import styles from '../Header.less';


interface Props extends WithNavigate {
	notifications: { count: number };
	resetNotificationsCount: () => void;
	deviceType: DeviceType;
}

interface State {
	isOpened: boolean;
	isLoading: boolean;
	notificationsHtml: string;
	counter: number;
}

class NotificationsMenu extends Component<Props, State> {
	ref: React.RefObject<HTMLButtonElement> = React.createRef();
	dropdownContainerRef: React.RefObject<HTMLDivElement> = React.createRef();

	constructor(props: Props) {
		super(props);

		this.state = {
			isOpened: false,
			isLoading: false,
			notificationsHtml: "",
			counter: props.notifications.count,
		};
	}

	static getDerivedStateFromProps(props: Props, state: State) {
		const { notifications, } = props;
		if(state.counter !== notifications.count) {
			return {
				counter: notifications.count,
			};
		}
		return null;
	}

	componentDidMount() {
		document.addEventListener('mousedown', this.handleClickOutside);
		document.addEventListener('click', this.handleClickInsideNotification);
	}

	componentWillUnmount() {
		document.removeEventListener('mousedown', this.handleClickOutside);
		document.addEventListener('click', this.handleClickInsideNotification);
	}

	handleClickOutside = (event: MouseEvent) => {
		if(this.ref.current && !this.ref.current.contains(event.target as Node)
			&& this.dropdownContainerRef.current && !this.dropdownContainerRef.current.contains(event.target as Node)) {
			this.setState({
				isOpened: false,
			});
		}
	};

	handleClickInsideNotification = (event: MouseEvent) => {
		let node = event.target as HTMLElement;
		while (node && node.classList && !node.classList.contains('notifications__new-comment-notification')) {
			node = node.parentNode as HTMLElement;
		}

		if(this.ref.current && this.dropdownContainerRef.current &&
			(this.ref.current.contains(node) || this.dropdownContainerRef.current.contains(node))) {
			this.setState({
				isOpened: false,
			});
		}
	};

	async loadNotifications() {
		const r = await fetch('/' + notificationsFeed);
		return await r.text();
	}

	onClick = () => {
		const { resetNotificationsCount, deviceType, navigate, } = this.props;
		const { isOpened, } = this.state;
		if(deviceType == DeviceType.mobile) {
			navigate('/' + feed);
		} else {
			if(!isOpened) {
				this.setState({
					isOpened: true,
					isLoading: true,
				});
				this.loadNotifications()
					.then(notifications => {
						resetNotificationsCount();
						this.setState({
							isLoading: false,
							notificationsHtml: notifications
						});
					});
			}
		}
	};

	render() {
		const { isOpened, counter, isLoading, notificationsHtml, } = this.state;
		const { deviceType, } = this.props;

		const className = cn(
			styles.headerElement,
			styles.button,
			{ [styles.notificationsWithCounter]: counter !== 0 },
			{ [styles.opened]: isOpened }
		);

		return (
			<button className={ className } ref={ this.ref }
					onClick={ this.onClick }>
				<NotificationsIcon counter={ counter }/>
				{
					isOpened &&
					<DropdownContainer
						getParent={ this.getParent }
						offsetY={ 0 }
						align="right"
						offsetX={ deviceType === DeviceType.mobile ? -112 : 0 }>
						<div className={ styles.notificationsContainer }
							 ref={ this.dropdownContainerRef }>
							<Notifications isLoading={ isLoading } notifications={ notificationsHtml }/>
						</div>
					</DropdownContainer>
				}
			</button>
		);
	}

	getParent = () => {
		return this.ref.current;
	};
}

const mapStateToProps = (state: RootState) => ({
		notifications: state.notifications
	}
);

const mapDispatchToProps = (dispatch: Dispatch) => ({
		resetNotificationsCount: () => dispatch(notificationResetAction())
	}
);

function NotificationsMenuWithNavigate(props: WithNavigateProvided<Props>) {
	const navigate = useNavigate();

	return <NotificationsMenu { ...props } navigate={ navigate }/>;
}

export default connect(mapStateToProps, mapDispatchToProps)(NotificationsMenuWithNavigate);
