import getGenderForm from "src/utils/getGenderForm";
import { getMoment } from "src/utils/momentUtils";
import { GroupAccessesInfo } from "src/models/groups";

export default {
	changeOwner: 'Сделать владельцем',
	removeTeacher: 'Забрать доступ',
	addTeacherSearch: 'Добавить преподавателя:',
	onDeleteStudentsToast: 'Студенты исключены из группы',

	teachersHeader: 'Преподаватели',
	teachersInfo: 'Преподаватели могут видеть список участников группы, проводить код-ревью\n' +
		'и проверку тестов, выставлять баллы и смотреть ведомость.',
	owner: 'Владелец',

	buildGrantedInfo: (item: GroupAccessesInfo): string =>
		`Полный доступ 
		${ getGenderForm(item.grantedBy.gender, 'предоставила', 'предоставил') } ${ item.grantedBy.visibleName } ${ getMoment(item.grantTime) }`,
};
