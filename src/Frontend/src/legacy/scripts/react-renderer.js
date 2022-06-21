/* This module allows to insert react components inside legacy layout.
 * Use following syntax to render react component:
 * 
 * <div class="react-render" data-component="comment" data-props="@(new { first = "First props", second="Second props"}.JsonSerialize())"></div>
 *  
 * Here `class="react-render"` is a trigger for work,
 * 		`data-component` is a component name (use should define it in Frontend/src/externalComponentRenderer.js), and
 * 		`data-props` are json-serialized props for the component (optional).
 * 	
 * 	Child components are not supported now.
 */

import { createRoot } from 'react-dom/client';

/* Import all components you want to insert into legacy (cshtml+jquery) layout */

import CommentsView from "src/components/comments/CommentsView/CommentsView";
import CommentsList from "src/components/comments/CommentsList/CommentsList";
import Comment from "src/components/comments/Comment/Comment";
import CommentSendForm from "src/components/comments/CommentSendForm/CommentSendForm";
import ToggleRolesModal from "src/components/UserRoles/ToggleRolesModal";

export default function () {
	$('.react-render').each(function () {
		const $this = $(this);
		$this.removeClass('react-render').addClass('react-rendered');
		const componentType = $this.data('component');
		let props = $this.data('props');
		if(!props)
			props = {};

		if(componentType) {
			renderReactComponent(componentType, this, props);
		}
	});
}

/* Define names for all components you want to use */
const components = {
	"CommentsView": CommentsView,
	"CommentsList": CommentsList,
	"Comment": Comment,
	"CommentSendForm": CommentSendForm,
	"ToggleRoles": ToggleRolesModal,
};

function renderReactComponent(componentType, element, props) {
	if(components[componentType] === undefined) {
		throw new Error("Unknown component type: " + componentType + ". Allowed types are: [" + Object.keys(components).join(", ") + "]");
	}

	let Component = components[componentType];

	if(element) {
		const root = createRoot(element);
		root.render(<Component { ...props } />);
	}
}

