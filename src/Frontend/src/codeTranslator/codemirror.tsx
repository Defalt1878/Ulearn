import React from "react";
import { createRoot } from "react-dom/client";
import { EditorConfiguration } from "codemirror";

import StaticCode, { Props } from "src/components/course/Course/Slide/Blocks/Exercise/StaticCode";

import { Language } from "src/consts/languages";


export default function translateTextareaToCode(
	textarea: HTMLTextAreaElement,
	additionalSettings?: { settings: Partial<Props>, config: Partial<EditorConfiguration> }
): void {
	const { lang } = textarea.dataset;

	const code = textarea.textContent || '';
	const language = (lang || Language.cSharp) as Language;

	const nodeElement = document.createElement('div');
	textarea.parentNode?.replaceChild(nodeElement, textarea);

	const root = createRoot(nodeElement);
	root.render(<StaticCode
		language={ language }
		code={ code }
		codeMirrorOptions={ additionalSettings?.config }
		disableStyles={ additionalSettings?.settings.disableStyles }
	/>);
}

export const settingsForFlashcards = {
	settings: {
		disableStyles: true,
	},
	config: {
		readOnly: 'nocursor',
	}
};
