import React, { FC } from 'react';
import { ShortUserInfo } from "../../../../../../models/users";
import styles from "./teacher.less";
import Avatar from "../../../../../common/Avatar/Avatar";
import Profile from "../../../../../common/Profile/Profile";
import { AccountState } from "../../../../../../redux/account";

interface Props {
	account: AccountState;
	user: ShortUserInfo;
	status: string;
	kebab?: React.ReactNode;
}

const Teacher: FC<Props> = ({ account, user, status, kebab }) => {
	return (
		<div key={ user.id } className={ styles["teacher-block"] }>
			<Avatar user={ user } size={ "big" }/>
			<div className={ styles["teacher-name"] }>
				<Profile
					user={ user }
					systemAccesses={ account.systemAccesses }
					isSysAdmin={ account.isSystemAdministrator }
				/>
				<span className={ styles["teacher-status"] }>
					{ status }
				</span>
			</div>
			{ kebab &&
				<div className={ styles["teacher-action"] }>
					{ kebab }
				</div>
			}
		</div>
	);
};

export default Teacher;
