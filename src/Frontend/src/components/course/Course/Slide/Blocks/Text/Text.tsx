import React, { createRef, RefObject, useEffect, useRef } from "react";
import classNames from "classnames";

import translateCode from "src/codeTranslator/translateCode";
import scrollToView from "src/utils/scrollToView";
import { withNavigate } from "src/utils/router";

import { studentZipDownloadUrl, WithNavigate } from "src/consts/routes";

import styles from "./Text.less";

interface Props extends WithNavigate {
	className?: string;
	content?: string;
	lines?: string[];
	disableAnchorsScrollHandlers?: boolean;
	disableTranslatingTex?: boolean;
	children?: React.ReactElement;
}

function Text(props: Props): React.ReactElement {
	const textContainer: RefObject<HTMLDivElement> = createRef();
	const prevProps = usePreviousProps(props);
	const navigate = props.navigate;

	useEffect(() => {
		const { disableTranslatingTex, disableAnchorsScrollHandlers, content, children, } = props;
		if(!prevProps || prevProps.content !== content || prevProps.children?.key !== children?.key) {
			if(!disableTranslatingTex) {
				translateTex();
			}

			if(!disableAnchorsScrollHandlers) {
				addScrollHandlersToAnchors();
			}
		}
	});

	function addScrollHandlersToAnchors(): void {
		if(!textContainer.current) {
			return;
		}

		const anchors = Array.from(textContainer.current.getElementsByTagName('a'));
		//if href equal to origin + pathname + hash => <a href="#hash"> that means its navigation on slide via headers, we need to handle this via our modificated scrolling
		const hashAnchorsLinks = anchors.filter(
			a => (window.location.origin + window.location.pathname + a.hash).toLowerCase() === a.href.toLowerCase());

		const hashInUrl = window.location.hash;
		if(hashInUrl) {
			const hashToScroll = hashInUrl.replace('#', '').toLowerCase();
			if(anchors.some(a => a.name.toLowerCase() === hashToScroll)) {
				scrollToHashAnchor(hashInUrl);
			}
		}

		for (const hashAnchor of hashAnchorsLinks) {
			const { hash } = hashAnchor;
			hashAnchor.addEventListener('click', (e) => {
				e.stopPropagation();
				e.preventDefault();
				scrollToHashAnchor(hash);
			});
		}

		const sameOriginLinks = anchors.filter(
			a => a.origin.toLowerCase() === window.location.origin.toLowerCase()
				&& a.pathname.toLowerCase() !== window.location.pathname.toLowerCase()
				&& !a.pathname.toLowerCase().startsWith(studentZipDownloadUrl) //filtering all download links
		);
		for (const anchor of sameOriginLinks) {
			anchor.addEventListener('click', (e) => {
				e.stopPropagation();
				e.preventDefault();
				navigate({
					pathname: anchor.pathname,
					search: anchor.search
				});
			});
		}
	}

	function scrollToHashAnchor(hash: string): void {
		if(hash.length === 0) {
			return;
		}

		window.history.pushState(null, '', hash);

		const anchors = document.querySelectorAll(`a[name=${ hash.replace('#', '') }]`);
		if(anchors.length > 0) {
			scrollToView({ current: anchors[0] }, {
				animationDuration: 500,
				behavior: 'smooth',
				additionalTopOffset: 50,
			});
		}
	}

	function translateTex(): void {
		if(textContainer.current) {
			translateCode(textContainer.current);
		}
	}

	function usePreviousProps(props: Props) {
		const ref = useRef<Props>();
		useEffect(() => {
			ref.current = props;
		});

		return ref.current;
	}

	const { content, className, children, lines, } = props;
	if(content) {
		return (
			<div
				ref={ textContainer }
				className={ classNames(styles.text, className) }
				dangerouslySetInnerHTML={ { __html: content } }
			/>
		);
	}

	if(lines) {
		return (
			<div
				ref={ textContainer }
				className={ classNames(styles.text, className) }>
				{ getContentFromTexLines(lines) }
			</div>
		);
	}
	return (
		<div
			ref={ textContainer }
			className={ classNames(styles.text, className) }>
			{ children }
		</div>
	);

	function getContentFromTexLines(lines: string[]): React.ReactNode {
		return lines.map((line, index) => (<p key={ index } className={ "tex" }>{ line }</p>));
	}
}


export default withNavigate(Text);
