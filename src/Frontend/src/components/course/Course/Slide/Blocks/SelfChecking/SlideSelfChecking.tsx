import React, { useEffect, useState } from "react";

import { Checkbox } from "ui";
import SelfCheckingContainer, {
	SelfCheckingSection,
	SelfCheckingContainerProps,
	RenderedSelfCheckup,
} from "./SelfCheckingContainer";
import { BlockProps } from "../../BlocksRenderer";

import texts from "./SelfChecking.texts";
import styles from "./SelfChecking.less";
import cn from "classnames";

export interface SelfCheckingBlockProps {
	checkups: RenderedSelfCheckup[];
	collapsed?: boolean;
	onCheckupClick: (id: string, isChecked: boolean,) => void;
}

export type SlideSelfCheckingProps = SelfCheckingBlockProps & Partial<BlockProps> & Partial<SelfCheckingContainerProps>;

function SlideSelfChecking({
	sections,
	checkups,
	onCheckupClick,
	collapsed,
	className,
	...blockProps
}: SlideSelfCheckingProps) {
	const [checkupsState, setCheckupsState] = useState<RenderedSelfCheckup[]>(checkups.map(c => ({ ...c })));
	const blocksToRender: SelfCheckingSection[] = sections?.map(b => ({ ...b })) ?? [];

	useEffect(() => {
		setCheckupsState(checkups.map(c => ({ ...c })));
	}, [checkups]);

	if(checkupsState.length > 0) {
		blocksToRender.push(buildSelfCheckingBlock());
	}

	return (
		<SelfCheckingContainer
			sections={ blocksToRender }
			className={ cn({ [styles.onSlide]: collapsed }, className) }
			{ ...blockProps }/>
	);

	function buildSelfCheckingBlock() {
		return ({
			title: texts.checkups.self.title,
			isCompleted: checkupsState.every(c => c.isChecked),
			content:
				<React.Fragment>
					<span className={ styles.overviewSelfCheckComment }>
					{ texts.checkups.self.text }
					</span>
					<ul className={ styles.overviewSelfCheckList }>
						{ renderSelfCheckBoxes(checkupsState) }
					</ul>
				</React.Fragment>
		});
	}

	function renderSelfCheckBoxes(selfChecks: RenderedSelfCheckup[]): React.ReactNode {
		return (
			selfChecks.map(({ content, isChecked, id, }) =>
				<li key={ id }>
					<Checkbox
						id={ id }
						checked={ isChecked }
						onClick={ onSelfCheckBoxClick }>
					<span className={ styles.selfCheckText }>
						{ content }
					</span>
					</Checkbox>
				</li>
			)
		);
	}

	function onSelfCheckBoxClick(element: React.MouseEvent<HTMLInputElement>): void {
		const id = element.currentTarget.id;
		const newSelfChecks = [...checkupsState];

		const checkupIndex = newSelfChecks.findIndex(c => c.id === id);
		if(checkupIndex === -1) {
			return;
		}
		const newCheckup = { ...newSelfChecks[checkupIndex] };

		newCheckup.isChecked = !newCheckup.isChecked;
		newSelfChecks[checkupIndex] = newCheckup;

		onCheckupClick(id, newCheckup.isChecked);

		setCheckupsState(newSelfChecks);
	}
}


export default SlideSelfChecking;
