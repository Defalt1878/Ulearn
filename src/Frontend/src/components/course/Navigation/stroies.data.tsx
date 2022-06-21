import { CourseMenuItem, MenuItem, SlideProgressStatus } from "./types";
import { SlideType } from "src/models/slide";
import { Props } from "./Navigation";
import { mock } from "src/storiesUtils";
import { DeviceType } from "src/consts/deviceType";
import React, { CSSProperties } from "react";
import { getDeviceType } from "src/utils/getDeviceType";
import { connect } from "react-redux";
import { deviceChangeAction } from "src/actions/device";
import { RootState } from "src/redux/reducers";

export const DesktopWrapper: (props: { children?: React.ReactNode; }) => React.ReactElement
	= ({ children }) => (
	<div style={ { width: '360px', } }>
		{ children }
	</div>
);


interface ViewportChangeHandlerProps {
	setDeviceType: (deviceType: DeviceType) => void;
	deviceType: DeviceType;
	render: (deviceType: DeviceType) => React.ReactNode;
}

interface ViewportChangeHandlerState {
	resizeTimeout?: NodeJS.Timeout;
}

class ViewportChangeHandler extends React.Component<ViewportChangeHandlerProps, ViewportChangeHandlerState> {
	constructor(props: ViewportChangeHandlerProps) {
		super(props);

		this.state = {
			resizeTimeout: undefined,
		};
	}

	componentDidMount = () => {
		addEventListener("resize", this.onWindowResize);
	};

	componentWillUnmount = () => {
		removeEventListener("resize", this.onWindowResize);
	};

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

	render() {
		const { deviceType, render, } = this.props;

		return render(deviceType);
	}
}

export const ViewportChangeHandlerRedux = connect(
	(rootState: RootState) => ({ deviceType: rootState.device.deviceType }),
	(dispatch =>
			({
				setDeviceType: (deviceType: DeviceType) => dispatch(deviceChangeAction(deviceType)),
			})
	))(ViewportChangeHandler);

export const deviceTypeToViewportStyle = (dt: DeviceType): CSSProperties => {
	switch (dt) {
		case DeviceType.desktop:
			return { height: 1080, width: 1920 };
		case DeviceType.laptop:
			return { height: 768, width: 1366 };
		case DeviceType.tablet:
			return { height: 1024, width: 800 };
		case DeviceType.mobile:
			return { height: 240, width: 320 };
	}
};

export const ViewportWrapper = ({ children }: { children: React.ReactNode }): React.ReactElement => {
	return (
		<ViewportChangeHandlerRedux render={ render }/>
	);

	function render(dt: DeviceType) {
		return <div style={ deviceTypeToViewportStyle(dt) }>
			{ children }
		</div>;
	}
};

export const disableViewport = {
	parameters: {
		viewport: {
			disable: true,
		},
	},
};

export const skipLoki = {
	parameters: {
		loki: { skip: true },
	},
};

export const disableViewportAnLoki = {
	parameters: {
		viewport: {
			disable: true,
		},
		loki: { skip: true },
	},
};

export const defaultNavigationProps: Pick<Props, 'navigationOpened' | 'deviceType' | 'toggleNavigation' | 'onCourseClick' | 'returnInUnit'> = {
	navigationOpened: true,
	deviceType: DeviceType.desktop,

	toggleNavigation: mock,
	onCourseClick: mock,
	returnInUnit: mock,
};

export const standardSlideProps: MenuItem<SlideType> = {
	containsVideo: false,
	id: "1",
	isActive: false,
	maxScore: 0,
	questionsCount: 0,
	quizMaxTriesCount: 0,
	score: 0,
	status: SlideProgressStatus.notVisited,
	title: "Title",
	type: SlideType.Lesson,
	url: "",
	visited: false,
	additionalContentInfo: {
		publicationDate: null,
		isAdditionalContent: false,
	}
};

const metro = {
	isFirstItem: false,
	isLastItem: false,
	connectToPrev: false,
	connectToNext: false,
};

const slidesExamples = [
	{ metro: { ...metro, isFirstItem: true, } },
	{ containsVideo: true },
	{ type: SlideType.Quiz, questionsCount: 5, quizMaxTriesCount: 2, },
	{
		type: SlideType.Quiz,
		status: SlideProgressStatus.canBeImproved,
		questionsCount: 5,
		quizMaxTriesCount: 2,
		score: 0,
		maxScore: 5,
		metro: { ...metro, connectToNext: true, }
	},
	{
		type: SlideType.Exercise,
		score: 50,
		maxScore: 100,
		visited: true,
		status: SlideProgressStatus.canBeImproved,
		metro: { ...metro, connectToPrev: true, },
	},
	{ type: SlideType.Flashcards, hide: true, },
	{ status: SlideProgressStatus.done, metro: { ...metro, connectToNext: true, } },
	{ status: SlideProgressStatus.done, metro: { ...metro, connectToPrev: true, } },
	{},
	{ status: SlideProgressStatus.done, metro: { ...metro, connectToNext: true, } },
	{ status: SlideProgressStatus.done, metro: { ...metro, connectToPrev: true, } },
	{ metro: { ...metro, isLastItem: true, } },
];

export function getModuleNavigationProps(): MenuItem<SlideType>[] {
	return slidesExamples
		.map(s => ({ ...s, status: s.status || SlideProgressStatus.notVisited, }))
		.map(s => ({ ...s, visited: s.status !== SlideProgressStatus.notVisited, }))
		.map((slide, index, array,) => {
			const finalSlide = { ...standardSlideProps, ...slide };
			setSlideTitleFromProps(finalSlide);
			finalSlide.id = index.toString();
			const isFirstItem = index === 0;
			const isLastItem = index === array.length - 1;
			finalSlide.metro = {
				isFirstItem: isFirstItem,
				isLastItem: isLastItem,
				connectToPrev: finalSlide.visited && index > 0 && array[index - 1].visited || false,
				connectToNext: !isLastItem && finalSlide.visited && (isLastItem || array[index + 1].visited) || false,
			};
			return { ...finalSlide, api: '', isActive: false, };
		});
}

export function setSlideTitleFromProps(slide: MenuItem<SlideType>): void {
	const descriptions = [];

	const addToDescription = (description: string) => descriptions.push(description);

	switch (slide.type) {
		case SlideType.Lesson: {
			descriptions.push('Урок');
			if(slide.containsVideo) {
				addToDescription('с видео');
			}
			break;
		}
		case SlideType.Exercise:
			descriptions.push('Задача');
			break;
		case SlideType.Quiz:
			descriptions.push('Квиз');
			break;
		case SlideType.Flashcards:
			descriptions.push('Флешкарты');
			break;
	}

	if(slide.isActive) {
		addToDescription('выбраный');
	}

	if(slide.hide) {
		addToDescription('скрытый');
	}

	switch (slide.status) {
		case SlideProgressStatus.canBeImproved:
			addToDescription('можно улучшить');
			break;
		case SlideProgressStatus.done:
			addToDescription('пройден');
			break;
		case SlideProgressStatus.notVisited:
			addToDescription('не посещён');
			break;
	}

	slide.title = descriptions.join(', ');
}

export function getCourseModules(): CourseMenuItem[] {
	return [{
		title: "Преподавателю о курсе",
		id: "c069ba64-e101-40e3-9b76-b65a1ae619ae",
		isActive: false,
		isNotPublished: true,
	}, {
		title: "Первое знакомство с C#",
		id: "e1beb629-6f24-279a-3040-cf111f91e764",
		isActive: true,
		progress: {
			statusesBySlides: {},
			current: 0,
			max: 100,
			inProgress: 0,
		},
	}, {
		title: "Ошибки",
		id: "6c13729e-817b-a437-b9d3-275c01f8f4a8",
		isActive: false,
		progress: {
			statusesBySlides: {},
			current: 25,
			max: 100,
			inProgress: 0,
		},
	}, {
		title: "Ветвления",
		id: "148775ee-9ffa-8932-64d6-d64380484169",
		isActive: false,
		progress: {
			statusesBySlides: {},
			current: 50,
			max: 100,
			inProgress: 0,
		},
		isNotPublished: true,
		publicationDate: "2021-08-18T11:05:27"
	}, {
		title: "Циклы",
		id: "d083f956-8fa4-6024-04da-cfc8e17bd9db",
		isActive: false,
		progress: {
			statusesBySlides: {},
			current: 75,
			max: 100,
			inProgress: 0,
		},
		isNotPublished: true
	}, {
		title: "Массивы",
		id: "c777829b-7226-9049-ddf9-895234334f3f",
		isActive: false,
		progress: {
			statusesBySlides: {},
			current: 100,
			max: 100,
			inProgress: 0,
		},
		isNotPublished: true
	}, {
		title: "Коллекции, строки, файлы",
		id: "7d679719-4676-36c4-f4b3-44682fd6d8b0",
		isActive: false,
		progress: {
			statusesBySlides: {},
			current: 50,
			max: 100,
			inProgress: 25,
		},
		isNotPublished: true
	}, {
		title: "Тестирование",
		id: "0299fc2c-1fdc-d85d-d654-4a6a1353f64d",
		isActive: false,
		progress: {
			statusesBySlides: {},
			current: 50,
			max: 100,
			inProgress: 50,
		},
		isNotPublished: true
	}, {
		title: "Сложность алгоритмов",
		id: "fab42d9c-db92-daec-e883-7d5424eb6c13",
		isActive: false,
		progress: {
			statusesBySlides: {},
			current: 0,
			max: 100,
			inProgress: 75,
		},
		isNotPublished: true
	}, {
		title: "Рекурсивные алгоритмы",
		id: "40de4f88-54d6-3c23-faee-0f9de37ad824",
		isActive: false,
		progress: {
			statusesBySlides: {},
			current: 0,
			max: 100,
			inProgress: 100,
		},
		isNotPublished: true
	}, {
		title: "Поиск и сортировка",
		id: "e4df2f6b-dc5d-3cc1-1467-0edcc93c211a",
		isActive: false,
		progress: {
			statusesBySlides: {},
			current: 50,
			max: 100,
			inProgress: 50,
		},
		isNotPublished: true
	}, {
		title: "Практикум",
		id: "0e56024a-ae74-4efa-d8a7-fea7fab5055b",
		isActive: false,
		progress: {
			statusesBySlides: {},
			current: 100,
			max: 100,
			inProgress: 0,
		},
		isNotPublished: true
	}, {
		title: "Основы ООП",
		id: "b8ff29db-7416-c2f6-aa37-7e58a96ed597",
		isActive: false,
		progress: {
			statusesBySlides: {},
			current: 0,
			max: 100,
			inProgress: 100,
		},
		isNotPublished: true
	}, {
		title: "Наследование",
		id: "8fe5a2fc-fe15-a2a6-87b8-74f4f36af51d",
		isActive: false,
		progress: {
			statusesBySlides: {},
			current: 28,
			max: 100,
			inProgress: 54,
		},
		isNotPublished: true
	}, {
		title: "Целостность данных",
		id: "1557d92c-68e6-63d0-69ee-414354353685",
		isActive: false,
		progress: {
			statusesBySlides: {},
			current: 30,
			max: 100,
			inProgress: 30,
		},
		isNotPublished: true
	}, {
		title: "Структуры",
		id: "97940709-9f6d-03f4-2090-63bd08befcf1",
		isActive: false,
		progress: {
			statusesBySlides: {},
			current: 33,
			max: 100,
			inProgress: 66,
		},
		isNotPublished: true
	}].map(
		s => ({
			...s,
			additionalContentInfo: {
				publicationDate: null,
				isAdditionalContent: false,
			},
			progress: {
				...s.progress,
				inProgress: s.progress?.inProgress || 0,
				max: s.progress?.max || 0,
				current: s.progress?.current || 0,
				additionalInfoBySlide: {}
			},
		})
	);
}
