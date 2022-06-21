import React from "react";
import { withNavigate, } from "src/utils/router";
import { WithNavigate, } from "src/consts/routes";
import { HasReactChild, } from "src/consts/common";

/* Component which captures <a> clicks and, if there is a matching route defined, routes them.
   Based on https://mattdistefano.com/blog/2017/11/30/handling-static-html-links-with-react-router
   and https://github.com/STRML/react-router-component/blob/master/lib/CaptureClicks.js
 */

const isModifiedEvent = <T, >(e: React.MouseEvent<T>) => !!(e.metaKey || e.altKey || e.ctrlKey || e.shiftKey);

const getNearest = (branch: any, root: any, tagName: string) => {
	let candidate = branch;

	while (candidate && candidate.tagName !== tagName) {
		candidate =
			candidate.parentElement === root ? null : candidate.parentElement;
	}

	return candidate;
};

const hasTarget = (anchor: HTMLAnchorElement) => anchor.target && anchor.target !== '_self';

const isSameDomain = (anchor: HTMLAnchorElement) =>
	anchor &&
	window &&
	window.location &&
	anchor.protocol === window.location.protocol &&
	anchor.host === window.location.host;

const fileRegex = /\.[a-zA-Z0-9]{2,4}$/;

const isAnchorLink = (anchor: HTMLAnchorElement) => anchor && anchor.href.indexOf("#") === 0;

const isProbablyFile = (anchor: HTMLAnchorElement) => anchor && anchor.pathname && fileRegex.test(anchor.pathname);

const isClientRoutable = (anchor: HTMLAnchorElement) =>
	anchor &&
	isSameDomain(anchor) &&
	!hasTarget(anchor) &&
	!isProbablyFile(anchor);

interface Props extends WithNavigate, HasReactChild {
	exclude: string[];
}

function LinkClickCapturer({ children, exclude, navigate, }: Props): React.ReactElement {
	return (
		<div onClick={ onClick }>
			{ children }
		</div>
	);

	function onClick<T>(e: React.MouseEvent<T>) {
		// Ignore canceled events, modified clicks, and right clicks.
		if(e.defaultPrevented || e.button !== 0 || isModifiedEvent(e)) {
			return;
		}

		const anchor = getNearest(e.target, e.currentTarget, 'A');

		if(!isClientRoutable(anchor)) {
			return;
		}

		if(exclude.some(prefix => anchor.pathname.startsWith(prefix))) {
			return;
		}

		e.preventDefault();

		if(isAnchorLink(anchor)) {
			return;
		}

		navigate(anchor.pathname + anchor.search);
	}

}

export default withNavigate(LinkClickCapturer);
