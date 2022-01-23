import React from "react";

import NavigationItem from '../NavigationItem';

import { MenuItem, } from '../../types';
import { SlideType } from 'src/models/slide';

import styles from './NavigationContent.less';


interface Props {
	items: MenuItem<SlideType>[];
	onClick: () => void;
	getRefToActive?: React.RefObject<HTMLLIElement>;
	courseId: string;
}

function NavigationContent({ items, onClick, getRefToActive, courseId, }: Props): React.ReactElement {
	return (
		<ol className={ styles.root }>
			{ items.map(renderItem) }
		</ol>
	);

	function renderItem(menuItem: MenuItem<SlideType>, index: number, items: MenuItem<SlideType>[]) {
		const isFirstItem = index === 0;
		const isLastItem = index === items.length - 1;
		const metroSettings = {
			isFirstItem: isFirstItem,
			isLastItem: isLastItem,
			connectToPrev: menuItem.visited && index > 0 && items[index - 1].visited || false,
			connectToNext: !isLastItem && menuItem.visited && (isLastItem || items[index + 1].visited) || false,
		};
		return (
			<NavigationItem
				{ ...menuItem }
				key={ menuItem.id }
				metro={ metroSettings }
				onClick={ onClick }
				getRefToActive={ getRefToActive }
				courseId={ courseId }
			/>
		);
	}
}

export default NavigationContent;
