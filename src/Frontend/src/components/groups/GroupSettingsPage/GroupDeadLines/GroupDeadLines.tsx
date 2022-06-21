import React, { useState } from "react";
import moment from "moment-timezone";

import {
	Button,
	Checkbox,
	DatePicker,
	DropdownMenu,
	Gapped,
	Hint,
	Input,
	Kebab,
	Loader,
	MenuItem,
	Modal,
	Select,
	Toast,
} from "ui";
import { ValidationContainer, ValidationWrapper } from "@skbkontur/react-ui-validations";
import { ArrowChevronDown, Copy, Delete, Undo, Warning } from "icons";
import Avatar from "src/components/common/Avatar/Avatar";
import { Profile } from 'src/components/groups/GroupSettingsPage/GroupMembers/Profile';

import { clone } from "src/utils/jsonExtensions";
import getPluralForm from "src/utils/getPluralForm";
import {
	momentFromServerToLocal,
	momentToDateInputFormat,
	momentToTimeInputFormat,
	serverFormat
} from "src/utils/momentUtils";
import { isDeadLineOverlappedByAnother, } from "src/utils/deadLinesUtils";

import { DEFAULT_TIMEZONE } from "src/consts/defaultTimezone";

import { ShortUserInfo } from "src/models/users";
import { DeadLineInfo, DeadLineSlideType, ScorePercent } from "src/models/deadLines";
import {
	DeadLineModuleInfo,
	Markup,
	Props,
	SlidesMarkupValue,
	State,
	StateDeadLineInfo,
	StudentValueMarkup,
	ValidationErrorType
} from "./GroupDeadLines.types";

import styles from './groupDeadLines.less';
import texts from './GroupDeadLines.texts';

const gmtOffsetInHours = moment().utcOffset() / 60;
const defaultTime = '00:00';

const timeFormatChars = {
	'1': '[0-2]',
	'3': '[0-5]',
	'2': '[0-9]',
};

const notFoundId = '-1';
const isLoading = 'isLoading';

const GetNameWithSecondNameFirst = (user: ShortUserInfo) => user.lastName && user.firstName ? `${ user.lastName } ${ user.firstName }` : user.visibleName;

function GroupDeadLines({
	courseId,
	groupId,
	user,
	...api
}: Props): React.ReactElement {
	const [state, setState] = useState<State | null | typeof isLoading>(null);

	if(!state && state !== isLoading) {
		setState(isLoading);
		loadData();
	}

	const deadLines = state && state !== isLoading ? Object.values(state.actualDeadLines) : null;
	const isNewDeadLineAddedButNotSaved = deadLines?.some(d => d.id === notFoundId);

	return (
		<Loader type={ "big" } active={ state == null } className={ styles.text }>
			<Gapped gap={ 12 } vertical>
				<p>{ texts.info }</p>
				<Hint text={ isNewDeadLineAddedButNotSaved
					? <span>{ texts.saveBeforeAdding }</span>
					: null }>
					<Button disabled={ isNewDeadLineAddedButNotSaved || false }
							use={ 'primary' } onClick={ addDeadLine }>
						{ texts.addDeadLine }
					</Button>
				</Hint>
				<ValidationContainer>
					{
						deadLines && deadLines.length > 0 &&
						<div className={ styles.table }>
							<span className={ styles.tableHeader }>
								{ texts.dateAndTime }
							</span>
							<span className={ styles.tableHeader }>
								{ texts.module }
							</span>
							<span className={ styles.tableHeader }>
								{ texts.slide }
							</span>
							<span className={ styles.tableHeader }>
								{ texts.student }
							</span>
							<span className={ styles.score }>
								{ texts.score }
							</span>
							{ renderDeadLines() }
							{ renderChooseStudentsModal() }
						</div>
					}
				</ValidationContainer>
			</Gapped>
		</Loader>
	);

	function renderDeadLines() {
		if(!state || state === isLoading) {
			return;
		}

		return deadLines?.map(d =>
			<DeadLine
				key={ d.id }

				deadLineInfo={ d }
				stateDeadLine={ state.responseDeadLines[d.id] }
				isNewDeadLineAddedButNotSaved={ isNewDeadLineAddedButNotSaved }

				units={ state.unitsMarkup }
				slides={ state.slidesMarkupByUnit[d.unitId] }
				students={ state.studentsMarkup }

				error={ state?.errors[d.id] }

				deleteDeadLine={ deleteDeadLine }
				saveDeadLine={ saveDeadLine }
				changeDate={ changeDate }
				cancelChanges={ cancelChanges }
				changePercent={ changePercent }
				changeSlide={ changeSlide }
				openChooseStudentsModal={ openChooseStudentsModal }
				changeTime={ changeTime }
				changeUnit={ changeUnit }
				copyDeadLinesForNextModule={ copyDeadLinesForNextModule }
			/>);
	}

	function renderChooseStudentsModal() {
		if(!state || state === isLoading || !state.studentsModal) {
			return;
		}
		const studentsModal = state.studentsModal;
		const deadLine = state.actualDeadLines[studentsModal.deadLineId];
		const allStudentsChecked = !deadLine.userIds;

		return (
			<Modal width={ 600 } onClose={ cancelAndCloseChooseStudentsModal }>
				<Modal.Header>Выбор студентов</Modal.Header>
				<Modal.Body>
					<Gapped vertical gap={ 8 }>
						{ state.studentsMarkup
							.filter(s => s[0] !== notFoundId)
							.map(([id, value]) => {
									const checked = deadLine.userIds?.includes(id) || false;
									const markup = value as StudentValueMarkup;
									const fullName = markup.visibleName ? markup.visibleName : value as string;
									const avatarUrl = markup.avatarUrl ? markup.avatarUrl : null;

									return (
										<div key={ id }>
											<Checkbox
												checked={ checked }
												onValueChange={ (value) =>
													changeStudent(studentsModal.deadLineId, id, value ? 'add' : 'delete') }>
												<Avatar user={ {
													id: id,
													visibleName: fullName,
													avatarUrl: avatarUrl,
													firstName: fullName,
													lastName: fullName
												} } size={ "small" }/>
												<Profile
													user={ { visibleName: fullName, id, } }
													systemAccesses={ user.systemAccesses }
													isSysAdmin={ user.isSystemAdministrator }/>
											</Checkbox>
										</div>);
								}
							)
						}
					</Gapped>
				</Modal.Body>
				<Modal.Footer>
					<Gapped vertical gap={ 8 }>
						<Checkbox
							disabled={ allStudentsChecked }
							checked={ allStudentsChecked }
							onValueChange={ (value) => changeStudent(studentsModal.deadLineId, notFoundId) }>
							Выбрать всех студентов
						</Checkbox>
						<Gapped gap={ 8 }>
							<Button use={ 'primary' } onClick={ closeChooseStudentsModal }>ОК</Button>
							<Button onClick={ cancelAndCloseChooseStudentsModal }>Отменить</Button>
						</Gapped>
					</Gapped>
				</Modal.Footer>
			</Modal>
		);
	}

	function openChooseStudentsModal(id: string,) {
		if(!state || state === isLoading) {
			return;
		}

		const deadLine = state.actualDeadLines[id];
		setState({
			...state,
			studentsModal: { deadLineId: deadLine.id, userIds: deadLine.userIds ? [...deadLine.userIds] : null }
		});
	}

	function closeChooseStudentsModal() {
		if(!state || state === isLoading) {
			return;
		}

		setState({ ...state, studentsModal: undefined });
	}

	function cancelAndCloseChooseStudentsModal() {
		if(!state || state === isLoading || !state.studentsModal) {
			return;
		}

		const studentsModal = state.studentsModal;
		const newState = clone(state);
		const deadLine = { ...newState.actualDeadLines[studentsModal.deadLineId] };
		newState.studentsModal = undefined;

		newState.actualDeadLines[deadLine.id].userIds = studentsModal.userIds ? [...studentsModal.userIds] : null;

		addOverlappingErrorInfoTo(Object.values(newState.actualDeadLines));

		setState(newState);
	}

	function loadData() {
		Promise.all([
			api.getDeadLines(courseId, groupId),
			api.getStudents(groupId),
			api.getCourse(courseId)
		]).then(([deadLinesResponse, studentsResponse, courseInfo]) => {
			const units: { [id: string]: DeadLineModuleInfo } = {};
			const scoringGroups = courseInfo.scoring.groups;
			courseInfo.units
				.reduce((pv, cv) => {
					const slides = cv.slides
						.filter(s => s.maxScore > 0)
						.map(s => ({
							id: s.id,
							title: s.title,
							scoringGroupId: s.scoringGroup,
						}));
					if(slides.length > 0) {
						pv[cv.id] = {
							id: cv.id,
							title: cv.title,
							slides,
						};
					}
					return pv;
				}, units);
			const unitsValues = Object.values(units);

			deadLinesResponse.deadLines
				.sort((d1, d2) => {
					const a = moment(d1.date, serverFormat);
					const b = moment(d2.date, serverFormat);
					return a.diff(b);
				});

			const unitsMarkup: Markup<string>[] = unitsValues.map(m => [m.id, m.title]);
			const slidesMarkupByUnit = unitsValues
				.reduce((pv, cv) => {
					const scoringGroupIdsByUnit = new Set(cv.slides.map(s => s.scoringGroupId));
					const scoringGroupByUnit = scoringGroups.filter(sg => scoringGroupIdsByUnit.has(sg.id));

					return {
						...pv,
						[cv.id]: cv.slides
							.map(s => [{ id: s.id }, s.title])
							.concat(scoringGroupByUnit.map(
								s => [{ id: s.id, isScoringGroup: true }, `Все слайды «${ s.abbr }»`]))
							.concat([[{ id: notFoundId }, texts.allSlides]]),
					};
				}, {});
			const studentsMarkup: Markup<string, StudentValueMarkup | string>[] = studentsResponse.students
				.map(
					s => [s.user.id, {
						visibleName: GetNameWithSecondNameFirst(s.user),
						avatarUrl: s.user.avatarUrl,
					}]);

			studentsMarkup.sort((a, b) =>
				(a[1] as StudentValueMarkup).visibleName.localeCompare((b[1] as StudentValueMarkup).visibleName));

			studentsMarkup.push([notFoundId, texts.allStudents]);

			const unitsIds = new Set(unitsValues.map(u => u.id));
			const slidesIdsByUnitId: {
				[unitId: string]: Set<string>;
			} = unitsValues.reduce((pv, cv) => {
				return {
					...pv,
					[cv.id]: new Set(cv.slides.map(s => s.id)),
				};
			}, {});
			const studentsIds = new Set(studentsResponse.students.map(s => s.user.id));
			const scoringGroupsIds = new Set(scoringGroups.map(g => g.id));

			const deadLines = deadLinesResponse.deadLines
				.reduce((pv, cv) => {
					const { date, time, } = parseTime(cv.date);

					//filtering all deadlines for deleted units/slides or for excluded students
					if(!unitsIds.has(cv.unitId) ||
						cv.slideType === DeadLineSlideType.ScoringGroupId && cv.slideValue !== null && !scoringGroupsIds.has(
							cv.slideValue) ||
						cv.slideType === DeadLineSlideType.SlideId && cv.slideValue !== null && !slidesIdsByUnitId[cv.unitId].has(
							cv.slideValue) ||
						cv.userIds !== null && cv.userIds.every(userId => !studentsIds.has(userId))
					) {
						return pv;
					}

					return {
						...pv,
						[cv.id]: {
							...cv,
							date,
							time,
						},
					};
				}, {});

			const responseDeadLines = clone(deadLines);

			addOverlappingErrorInfoTo(Object.values(deadLines));

			setState({
				actualDeadLines: deadLines,
				responseDeadLines,
				errors: {},

				unitsMarkup,
				slidesMarkupByUnit,
				studentsMarkup,
				studentsModal: undefined,
			});
		});
	}

	function addOverlappingErrorInfoTo(deadlines: StateDeadLineInfo[]) {
		deadlines.forEach(d => {
			const overlappingDeadLines = deadlines
				.filter(d1 => d1.unitId === d.unitId &&
					(d1.slideType === DeadLineSlideType.All ||
						d1.slideType === d.slideType && d1.slideValue === d.slideValue) &&
					(d1.userIds === null ||
						d1.userIds.every(u => d.userIds?.includes(u))))
				.map(buildDeadLineFromStateDeadLine);
			d.isOverlappedByOtherDeadLine = overlappingDeadLines.length > 1
				&& isDeadLineOverlappedByAnother(buildDeadLineFromStateDeadLine(d), overlappingDeadLines);
		});
	}

	function parseTime(dateString: string) {
		const publicationMoment = momentFromServerToLocal(dateString);
		const date = momentToDateInputFormat(publicationMoment);
		const time = momentToTimeInputFormat(publicationMoment);

		return { date, time };
	}

	function addDeadLine() {
		if(!state || state === isLoading) {
			return;
		}

		const newDeadLines = clone(state.actualDeadLines);
		newDeadLines[notFoundId] = {
			id: notFoundId,
			courseId,
			date: momentToDateInputFormat(moment()),
			time: defaultTime,
			unitId: state.unitsMarkup[0][0],
			groupId,
			scorePercent: 0,
			slideType: DeadLineSlideType.All,
			slideValue: null,
			userIds: null,
		};

		addOverlappingErrorInfoTo(Object.values(newDeadLines));

		setState({
			...state,
			actualDeadLines: newDeadLines,
		});
	}

	function copyDeadLinesForNextModule(id: string) {
		if(!state || state === isLoading) {
			return;
		}

		const newDeadLines = clone(state.actualDeadLines);
		const deadLine = newDeadLines[id];
		const nextUnitIndex = state.unitsMarkup.findIndex(u => u[0] === deadLine.unitId) + 1;
		if(nextUnitIndex >= state.unitsMarkup.length) {
			return;
		}

		newDeadLines[notFoundId] = {
			...deadLine,
			id: notFoundId,
			unitId: state.unitsMarkup[nextUnitIndex][0],
			date: momentToDateInputFormat(moment(deadLine.date, 'DD.MM.YYYY').add(1, 'w')),
		};

		addOverlappingErrorInfoTo(Object.values(newDeadLines));

		setState({
			...state,
			actualDeadLines: newDeadLines,
		});
	}

	function changeDate(
		id: string,
		value: string,
	) {
		validateDate(value, id);
		changeDeadLine(id, (deadLineInfo) => deadLineInfo.date = value);
	}

	function validateDate(value: string, id: string,) {
		if(!state || state === isLoading) {
			return;
		}

		const errors = { ...state.errors, };
		const [day, month, year] = value.split('.');
		const isDateInvalid = !day || !month || !year;

		if(isDateInvalid) {
			errors[id] = 'date';
		} else {
			delete errors[id];
		}

		setState(prevState => (prevState && prevState !== isLoading
			? {
				...prevState,
				errors,
			}
			: null));

		return !isDateInvalid;
	}

	function validateTime(value: string, id: string) {
		if(!state || state === isLoading) {
			return;
		}

		const errors = { ...state.errors, };
		const [hours, minutes] = value.split(":");
		const isTimeInvalid = !hours || parseInt(hours) > 23 || !minutes || value.length < 5;
		if(isTimeInvalid) {
			errors[id] = 'time';
		} else {
			delete errors[id];
		}

		setState(prevState => (prevState && prevState !== isLoading
			? {
				...prevState,
				errors,
			}
			: null));

		return !isTimeInvalid;
	}

	function changeTime(
		id: string,
		value: string,
	) {
		validateTime(value, id);
		changeDeadLine(id, (deadLineInfo) => deadLineInfo.time = value);
	}

	function changeUnit(
		id: string,
		value: string,
	) {
		changeDeadLine(id, (deadLineInfo) => {
			if(!state || state === isLoading) {
				return;
			}

			deadLineInfo.unitId = value;
			deadLineInfo.slideValue = null;
			deadLineInfo.slideType = DeadLineSlideType.All;
		});
	}

	function changeSlide(
		id: string,
		value: SlidesMarkupValue,
	) {
		changeDeadLine(id, (deadLineInfo) => {
			if(value.id === notFoundId) {
				deadLineInfo.slideValue = null;
				deadLineInfo.slideType = DeadLineSlideType.All;
			} else {
				deadLineInfo.slideValue = value.id;
				deadLineInfo.slideType = value.isScoringGroup ? DeadLineSlideType.ScoringGroupId : DeadLineSlideType.SlideId;
			}
		});
	}

	function changePercent(
		id: string,
		value: ScorePercent,
	) {
		changeDeadLine(id, (deadLineInfo) => deadLineInfo.scorePercent = value);
	}

	function changeStudent(
		id: string,
		value: string,
		addOrDelete: 'add' | 'delete' = 'add',
	) {
		changeDeadLine(id, (deadLineInfo) => {
			if(addOrDelete === 'add') {
				if(value === notFoundId) {
					deadLineInfo.userIds = null;
					return;
				}
				if(deadLineInfo.userIds === null) {
					deadLineInfo.userIds = [value];
				} else {
					deadLineInfo.userIds = [...deadLineInfo.userIds, value];
				}
			}
			if(addOrDelete === 'delete') {
				if(deadLineInfo.userIds?.length === 1) {
					deadLineInfo.userIds = null;
				} else {
					deadLineInfo.userIds = deadLineInfo.userIds?.filter(id => id !== value) || null;
				}
			}
		});
	}

	function changeDeadLine(id: string, update: (deadLine: StateDeadLineInfo) => void) {
		if(!state || state === isLoading) {
			return;
		}

		const deadlines = clone(state.actualDeadLines);
		const deadLineInfo = deadlines[id];
		if(deadLineInfo) {
			update(deadLineInfo);
			addOverlappingErrorInfoTo(Object.values(deadlines));
		}

		setState(prevState => (prevState && prevState !== isLoading
			? {
				...prevState,
				actualDeadLines: deadlines,
			}
			: null));
	}

	function deleteDeadLine(id: string,) {
		if(!state || state === isLoading) {
			return;
		}

		const newState = clone(state);
		delete newState.actualDeadLines[id];

		addOverlappingErrorInfoTo(Object.values(newState.actualDeadLines));

		setState(newState);
		if(id !== notFoundId) {
			api.deleteDeadLine(id);
			delete newState.responseDeadLines[id];
			Toast.push(texts.deleteToast);
		}
	}

	function buildDeadLineFromStateDeadLine(deadLine: StateDeadLineInfo): DeadLineInfo {
		return {
			...deadLine,
			// eslint-disable-next-line @typescript-eslint/ban-ts-comment
			// @ts-ignore
			time: undefined,
			date: moment(`${ deadLine.date }T${ deadLine.time }`, 'DD.MM.YYYYTHH:mm')
				.local()
				.tz(DEFAULT_TIMEZONE)
				.format(serverFormat)
		};
	}

	async function saveDeadLine(id: string,) {
		if(!state || state === isLoading) {
			return;
		}

		const newState = clone(state);
		const deadLine = buildDeadLineFromStateDeadLine(newState.actualDeadLines[id]);

		if(id === notFoundId) {
			deadLine.id = (await api.createDeadLine(courseId, deadLine as DeadLineInfo)
				.then(r => r))
				.id;
			newState.actualDeadLines[deadLine.id] = { ...newState.actualDeadLines[id], id: deadLine.id };
			delete newState.actualDeadLines[id];
		} else {
			await api.changeDeadLine(deadLine as DeadLineInfo);
		}
		newState.responseDeadLines[deadLine.id] = newState.actualDeadLines[deadLine.id];

		setState(newState);

		Toast.push(texts.saveToast);
	}

	function cancelChanges(id: string,) {
		if(!state || state === isLoading) {
			return;
		}
		const newState = clone(state);
		const deadLine = { ...newState.actualDeadLines[id] };

		newState.actualDeadLines[deadLine.id] = newState.responseDeadLines[deadLine.id];

		addOverlappingErrorInfoTo(Object.values(newState.actualDeadLines));

		setState(newState);
	}
}


function DeadLine(
	{
		deadLineInfo,
		stateDeadLine,
		isNewDeadLineAddedButNotSaved,

		units,
		slides,

		error,

		changeDate,
		changeTime,
		changeUnit,
		changeSlide,
		openChooseStudentsModal,
		changePercent,
		saveDeadLine,
		deleteDeadLine,
		cancelChanges,
		copyDeadLinesForNextModule,
	}: {
		deadLineInfo: StateDeadLineInfo,
		stateDeadLine: StateDeadLineInfo,
		isNewDeadLineAddedButNotSaved?: boolean,

		units: Markup<string>[],
		slides: Markup<SlidesMarkupValue>[],
		students: Markup<string, StudentValueMarkup | string>[],

		error: ValidationErrorType | undefined,

		changeDate: (id: string, date: string) => void,
		changeTime: (id: string, time: string) => void,
		changeUnit: (id: string, unitId: string) => void,
		changeSlide: (id: string, slideValue: SlidesMarkupValue) => void,
		openChooseStudentsModal: (id: string) => void,
		changePercent: (id: string, percent: ScorePercent) => void,
		saveDeadLine: (id: string) => void,
		deleteDeadLine: (id: string) => void,
		copyDeadLinesForNextModule: (id: string) => void,
		cancelChanges: (id: string) => void,
	}
) {

	const gmtOffsetInHoursAsString = `${ gmtOffsetInHours >= 0 ? '+' : '-' }${ gmtOffsetInHours }`;

	const pendingCreation = deadLineInfo.id === notFoundId;
	const pendingChanges = pendingCreation || isDifferent(stateDeadLine, deadLineInfo);
	const anyChanges = pendingChanges || pendingCreation;

	const dateValidationInfo = (error === 'date' || error === 'time&date') ? { message: texts.wrongDate } : null;
	const timeValidationInfo = (error === 'time' || error === 'time&date') ? { message: texts.wrongTime } : null;

	const slideItem = deadLineInfo.slideValue &&
		slides.find(s => s[0].id === deadLineInfo.slideValue)?.[0] ||
		slides.find(s => s[0].id === notFoundId)?.[0];

	const unitIndex = units.findIndex(u => u[0] === deadLineInfo.unitId);
	const isNextModuleExists = unitIndex + 1 < units.length;

	return (
		<React.Fragment>
				<span>
					{ deadLineInfo.isOverlappedByOtherDeadLine &&
					<Hint text={ texts.conflict }>
						<Warning size={ 16 } className={ styles.conflictHint }/>
					</Hint> }
					<ValidationWrapper validationInfo={ dateValidationInfo }>
						<DatePicker
							width={ 120 }
							value={ deadLineInfo.date }
							onValueChange={ changeDateWithId }
							enableTodayLink
						/>
					</ValidationWrapper>

					<ValidationWrapper validationInfo={ timeValidationInfo }>
						<Input
							width={ 120 }
							alwaysShowMask
							onValueChange={ changeTimeWithId }
							rightIcon={ `GMT${ gmtOffsetInHoursAsString }` }
							formatChars={ timeFormatChars }
							mask={ `12:32` }
							value={ deadLineInfo.time }
						/>
					</ValidationWrapper>
				</span>
			<span>
					<Select<string>
						maxWidth={ '100%' }
						width={ '100%' }
						items={ units }
						value={ deadLineInfo.unitId }
						onValueChange={ changeUnitWithId }/>
				</span>
			<span>
					<Select<SlidesMarkupValue>
						maxWidth={ '100%' }
						width={ '100%' }
						items={ slides }
						value={ slideItem }
						onValueChange={ changeSlideWithId }/>
				</span>
			<span>
				<Button onClick={ openChooseStudentsModalWithId }>
					{
						deadLineInfo.userIds && deadLineInfo.userIds.length > 0
							? <>{ getPluralForm(deadLineInfo.userIds.length,
								'Выбран', 'Выбраны', 'Выбрано') } { deadLineInfo.userIds.length } { getPluralForm(
								deadLineInfo.userIds.length,
								'студент', 'студента', 'студентов') }</>
							: <>Выбраны все студенты </>
					}
					<ArrowChevronDown className={ styles.iconInButtonAsSelectIcon }
									  color={ '#a6a6a6' }/>
				</Button>
				</span>
			<span>
					<Select<ScorePercent>
						maxWidth={ '100%' }
						width={ '100%' }
						value={ deadLineInfo.scorePercent }
						items={ [0, 25, 50, 75] }
						onValueChange={ changePercentWithId }
					/>
				</span>
			<span>
					<Gapped gap={ 16 }>
						<Hint text={ anyChanges || error !== undefined ? texts.saveDeadLine : null }>
							<Button
								use={ 'link' }
								size={ "medium" }
								onClick={ saveDeadLineWithId }
								disabled={ !anyChanges || error !== undefined }
							>
								{ texts.saveButtonText }
							</Button>
						</Hint>
						<DropdownMenu caption={ <Kebab size={ 'medium' }/> }>
							{
								!pendingCreation &&
								<MenuItem disabled={ !pendingChanges } onClick={ cancelChangesWithId } icon={ <Undo/> }>
									{ texts.cancel }
								</MenuItem>
							}
							<MenuItem onClick={ deleteDeadLineWithId }
									  icon={ <Delete className={ styles.deleteButtonText }/> }>
								 { texts.delete }
							</MenuItem>
							{ isNextModuleExists &&
							<MenuItem
								disabled={ isNewDeadLineAddedButNotSaved }
								onClick={ copyDeadLinesForNextModuleWithId }
								icon={ <Copy/> }>
								<Hint text={ isNewDeadLineAddedButNotSaved && texts.saveBeforeAdding }>
									{ texts.copyForNextUnit }
								</Hint>
							</MenuItem>
							}
						</DropdownMenu>
					</Gapped>
				</span>
		</React.Fragment>
	);

	function isDifferent(d1: StateDeadLineInfo, d2: StateDeadLineInfo) {
		return d1.time !== d2.time
			|| d1.date !== d2.date
			|| d1.userIds !== null && d2.userIds === null
			|| d1.userIds === null && d2.userIds !== null
			|| d1.userIds !== null && d2.userIds !== null && (d1.userIds.length !== d2.userIds.length || d1.userIds.some(
				userId => d2.userIds?.indexOf(userId) === -1))
			|| d1.slideType !== d2.slideType
			|| d1.slideValue !== d2.slideValue
			|| d1.scorePercent !== d2.scorePercent
			|| d1.unitId !== d2.unitId;
	}

	function changeDateWithId(date: string) {
		changeDate(deadLineInfo.id, date);
	}

	function changeTimeWithId(time: string) {
		changeTime(deadLineInfo.id, time);
	}

	function changeUnitWithId(unitId: string) {
		changeUnit(deadLineInfo.id, unitId);
	}

	function changeSlideWithId(slideValue: SlidesMarkupValue) {
		changeSlide(deadLineInfo.id, slideValue);
	}

	function openChooseStudentsModalWithId() {
		openChooseStudentsModal(deadLineInfo.id);
	}

	/*	function changeStudentWithId(studentId: string) {
			changeStudent(deadLineInfo.id, studentId);
		}*/

	function changePercentWithId(percent: ScorePercent) {
		changePercent(deadLineInfo.id, percent);
	}

	function saveDeadLineWithId() {
		saveDeadLine(deadLineInfo.id);
	}

	function deleteDeadLineWithId() {
		deleteDeadLine(deadLineInfo.id);
	}

	function copyDeadLinesForNextModuleWithId() {
		copyDeadLinesForNextModule(deadLineInfo.id);
	}

	function cancelChangesWithId() {
		cancelChanges(deadLineInfo.id);
	}
}

export default GroupDeadLines;
