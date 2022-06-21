import React from "react";
import CourseNavigationItem, { Props } from "./CourseNavigationItem";
import { mock } from "src/storiesUtils";
import type { Story } from "@storybook/react";
import { DesktopWrapper, disableViewport } from "../../stroies.data";

export default {
	title: "CourseNavigationHeader",
	...disableViewport
};

const defaultProps: Props = {
	onClick: mock,
	id: '1',
	isActive: false,
	title: "Первое знакомство с C#",
	courseId: 'basicprogramming',
	isNotPublished: true,
	publicationDate: undefined,
	additionalContentInfo: { isAdditionalContent: false, publicationDate: null, },
	progress: {
		current: 0,
		additionalInfoBySlide: {},
		max: 100,
		inProgress: 0,
	},
};

const Template: Story<Partial<Props>[]> = (propsArray) => (
	<DesktopWrapper>
		<nav>
			{ Object.values(propsArray).map(props => (
				<CourseNavigationItem { ...defaultProps } { ...props }/>
			)) }
		</nav>
	</DesktopWrapper>
);

const Default = Template.bind({});
Default.args = [
	{},
	{ title: 'Ошибки' },
	{ title: 'Тут много текста в одном пункте. Надеюсь, таких длинных текстов у нас не будет, но на всякий случай их нужно учесть' },
	{ title: 'Тутдлинноесловобезпробеловкакжесложноегонабиратьононедолжноничеголоматьвидимолучшийвариантегообрезатьмноготочием' },
	{ title: 'Текущий модуль', isActive: true, },
	{ title: "Модуль с прогресс-баром", },
	{
		title: "Модуль с заполненным прогресс-баром",
		progress: {
			additionalInfoBySlide: {},
			current: 100,
			max: 100,
			inProgress: 0,
		}
	},
	{ title: "Модуль с прогресс-баром", },
];
