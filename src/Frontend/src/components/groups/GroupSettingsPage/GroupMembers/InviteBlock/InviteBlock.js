import React, { Component } from "react";
import PropTypes from "prop-types";
import api from "src/api";
import { Toggle, Button, Toast, Link, Input } from "ui";

import styles from './inviteBlock.less';

class InviteBlock extends Component {
	constructor(props) {
		super(props);
		this.state = {
			isInviteLinkEnabled: props.group.isInviteLinkEnabled
		};
	}

	render() {
		const isInviteLinkEnabled = this.state.isInviteLinkEnabled;

		return (
			<div className={ styles["toggle-invite"] }>
				<label>
					<Toggle
						checked={ isInviteLinkEnabled }
						onChange={ this.onToggle }>
					</Toggle>
					<span className={ styles["toggle-invite-text"] }>
						Ссылка для вступления в группу { isInviteLinkEnabled ? ' включена' : ' выключена' }
					</span>
				</label>
				{ isInviteLinkEnabled && this.renderInvite() }
			</div>
		)
	}

	renderInvite() {
		const { group } = this.props;

		return (
			<div className={ styles["invite-link"] }>
				<div className={ styles["invite-link-text"] }>
					<Button use="link" icon={ <Link/> } onClick={ () => {
						navigator.clipboard.writeText(`${ window.location.origin }/Account/JoinGroup?hash=${ group.inviteHash }`)
						Toast.push('Ссылка скопирована')
					} }>
						Скопировать ссылку
					</Button>
				</div>
				<div className={ styles["invite-link-input"] }>
					<Input
						type="text"
						value={ `${ window.location.origin }/Account/JoinGroup?hash=${ group.inviteHash }` }
						readOnly
						selectAllOnFocus
						width="65%"
					/>
				</div>
			</div>
		)
	}

	onToggle = () => {
		const { group } = this.props;
		const isInviteLinkEnabled = this.state.isInviteLinkEnabled;

		const update = () => {
			this.setState({
				isInviteLinkEnabled: !isInviteLinkEnabled,
			});
			this.props.group.isInviteLinkEnabled = !isInviteLinkEnabled;
		};
		const revert = () => {
			this.setState({
				isInviteLinkEnabled: isInviteLinkEnabled,
			});
			this.props.group.isInviteLinkEnabled = isInviteLinkEnabled;
		};
		update();

		api.groups.saveGroupSettings(group.id, { 'isInviteLinkEnabled': !isInviteLinkEnabled })
			.catch((error) => {
				error.showToast();
				revert();
			});
	};
}

InviteBlock.propTypes = {
	group: PropTypes.object,
};

export default InviteBlock;
