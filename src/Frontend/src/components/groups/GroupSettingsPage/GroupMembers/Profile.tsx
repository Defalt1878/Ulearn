import React from "react";
import { Link } from "ui";

import { ShortUserInfo } from "src/models/users";
import { SystemAccessType } from "src/consts/accessType";

import styles from "./Profile.less";

interface Props {
	user: ShortUserInfo;
	systemAccesses: SystemAccessType[];
	isSysAdmin: boolean;
	showLastNameFirst?: boolean;
}

function Profile(props: Props): React.ReactElement {
	const { isSysAdmin, systemAccesses, user, showLastNameFirst } = props;
	const canViewProfiles = systemAccesses.includes(SystemAccessType.viewAllProfiles) || isSysAdmin;
	const profileUrl = `/Account/Profile?userId=${ user.id }`;
	const name = showLastNameFirst && user.lastName && user.firstName
		? `${ user.lastName } ${ user.firstName }`
		: user.visibleName;

	return canViewProfiles
		? <Link title={ user.id } href={ profileUrl }>{ name }</Link>
		: <div title={ user.id } className={ styles.name }>{ name }</div>;
}

const GetNameWithSecondNameFirst = (user: ShortUserInfo): string => user.lastName && user.firstName ? `${ user.lastName } ${ user.firstName }` : user.visibleName;

export { Profile, GetNameWithSecondNameFirst };
