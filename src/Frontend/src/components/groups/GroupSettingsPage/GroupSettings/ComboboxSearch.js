import React, { Component } from "react";
import PropTypes from "prop-types";
import api from "../../../../api";
import ComboBox from "@skbkontur/react-ui/components/ComboBox/ComboBox";
import colorHash from "../../../../utils/colorHash";

export class ComboboxSearch extends Component {
	render () {
		const { selected } = this.props;
		return (
		<ComboBox
			getItems={this.getItems}
			size="small"
			width="100%"
			value={selected}
			renderItem={this.renderItem}
			renderValue={this.renderItem}
			renderNotFound={this.renderNotFound}
			onChange={this.onChangeItem}
			placeholder="Начните вводить имя, фамилию или логин преподавателя"/>
		);
	}

	getItems = (query) => {
		const { accesses, owner } = this.props;
		const includes = (stack, item) => stack.toLowerCase().includes(item.toLowerCase());
		const uniqueValue = (item) => {
			return (owner.id !== item.id) &&
			(accesses.filter(i => i.user.id === item.id)).length === 0;
		};
		// TODO objIncludes(obj, propNames, query)

		return api.users.getUsersCourse(this.props.courseId)
			.then(json => {
				return json.instructors
					.filter(item => {
						return (uniqueValue(item)) &&
							(includes(item.visible_name, query) ||
							includes(item.login, query))
					})
					.map(item => ({
								value: item.id,
								label: item.visible_name,
								...item,
							}
						)
					)
				})
			.catch(error => {
				console.error(error);
				return [];
			});
	};

	renderItem = (item) => {
		const name = item.label;
		const firstLetter = name[name.search(/[a-zA-Zа-яА-Я]/)].toUpperCase();

		return (
		<div className="combo-item">
			<div
				className="combo-item_avatar"
				style={{backgroundColor: `${colorHash(name)}`}}>
				{firstLetter}
			</div>
			<span>{name}</span>
			<span className="combo-item_login">логин: {item.login}</span>
		</div>
		)
	};

	renderNotFound = (item) => {
		if (item === undefined) {
			return (
				<span>
					В этом курсе нет свободных преподавателей
				</span>
			)
		}

	};

	onChangeItem = (_, item) => {
		this.props.onAddTeacher(item);
	}
}

ComboboxSearch.propTypes = {
	selected: PropTypes.object,
	courseId: PropTypes.string,
	onAddTeacher: PropTypes.func,
	accesses: PropTypes.array,
	owner: PropTypes.object,
};