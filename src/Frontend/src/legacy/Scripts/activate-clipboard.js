﻿window.documentReadyFunctions = window.documentReadyFunctions || [];

import ClipboardJS from "clipboard";

window.documentReadyFunctions.push(function () {
	new ClipboardJS('.clipboard-link').on('success', function (e) {
		const $trigger = $(e.trigger);
		if ($trigger.data('show-copied')) {
			const oldValue = $trigger.text();
			$trigger.text('скопировано!');
			setTimeout(function() {
				$trigger.text(oldValue);
				},
				1000);
		}
	});
});
