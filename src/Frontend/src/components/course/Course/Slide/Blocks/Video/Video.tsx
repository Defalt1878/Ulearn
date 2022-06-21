import React from 'react';

import YouTube from 'react-youtube';
import { Link, } from "ui";
import { BlocksWrapper, Text, } from "src/components/course/Course/Slide/Blocks";
import { ArrowChevronDown, ArrowChevronUp, } from "icons";

import classNames from 'classnames';
import { Cookies, withCookies } from 'react-cookie';

import styles from './Video.less';
import { BlockRenderContext } from "../../BlocksRenderer";

const videoCookieName = 'youtube-video-rate';

interface AnnotationFragment {
	text: string;
	offset: string;
}

interface Annotation {
	text: string;
	fragments: AnnotationFragment[];
}

interface Props {
	annotation: Annotation;
	googleDocLink: string;
	videoId: string;
	className: string;
	containerClassName: string;
	hide: boolean;
	cookies: Cookies;
	renderContext: BlockRenderContext;
}

interface State {
	showedAnnotation: boolean;
	autoplay: boolean;
	removeBottomPaddings: boolean;
}

class Video extends React.Component<Props, State> {
	private ytPlayer: YT.Player | null = null;

	constructor(props: Props) {
		super(props);

		const { renderContext, } = props;
		//const { autoplay } = queryString.parse(window.location.search);
		const params = new URLSearchParams(window.location.search);
		const autoplay = params.get('autoplay');

		this.state = {
			autoplay: renderContext.previous === undefined ? !!autoplay : false,
			showedAnnotation: renderContext.previous === undefined && renderContext.next === undefined,
			removeBottomPaddings: !renderContext.hide &&
				(renderContext.next !== undefined
						? renderContext.next.type !== renderContext.type
						: true
				)
		};
	}

	componentDidUpdate() {
		const { cookies, } = this.props;

		if(this.ytPlayer) {
			const newVideoRate = parseFloat(cookies.get(videoCookieName) || '1');

			this.ytPlayer.setPlaybackRate(newVideoRate);
		}
	}

	render() {
		const {
			videoId,
			className,
			containerClassName,
			googleDocLink,
			hide,
		} = this.props;
		const {
			autoplay,
		} = this.state;

		const containerClassNames = classNames(styles.videoContainer, { [containerClassName]: containerClassName });
		const frameClassNames = classNames(styles.frame, { [className]: className });

		const opts = {
			playerVars: {
				autoplay: autoplay ? 1 : 0,
				/* Disable related videos */
				rel: 0,
			},
		};

		return (
			<React.Fragment>
				<YouTube
					//iframeClassName={ containerClassNames }
					//className={ frameClassNames }
					videoId={ videoId }
					opts={ opts }
					onReady={ this.onReady }
					onPlaybackRateChange={ this.onPlaybackRateChange }
				/>
				{ hide && <BlocksWrapper hide isBlock withoutBottomPaddings={ !!googleDocLink }>
					<Text disableAnchorsScrollHandlers disableTranslatingTex className={ styles.withoutBottomMargins }>
						<p>Видео выше скрыто</p>
					</Text>
				</BlocksWrapper> }
				{ googleDocLink && this.renderAnnotation() }
			</React.Fragment>
		);
	}

	onRateChange = (rate: number): void => {
		const { cookies } = this.props;
		cookies.set(videoCookieName, rate);
	};

	onReady = (event: {
			target: YT.Player
		}
	): void => {
		const { cookies } = this.props;

		this.ytPlayer = event.target;
		const rate = parseFloat(cookies.get(videoCookieName) || '1');
		this.ytPlayer.setPlaybackRate(rate);
	};

	onPlaybackRateChange = (event: { target: YT.Player, data: number }) => {
		const { cookies } = this.props;

		cookies.set(videoCookieName, event.data);
	};

	renderAnnotation = () => {
		const { showedAnnotation, removeBottomPaddings, } = this.state;
		const { annotation, googleDocLink, hide, } = this.props;

		return (
			<BlocksWrapper
				withoutEyeHint
				withoutBottomPaddings={ removeBottomPaddings }
				hide={ hide }
				isBlock
				className={ styles.withoutBottomMargins }>
				<Text disableAnchorsScrollHandlers disableTranslatingTex>
					{
						annotation
							? this.renderAnnotationContent(showedAnnotation, annotation, googleDocLink)
							: <p>
								Помогите написать <Link target="_blank" href={ googleDocLink }>текстовое
								содержание</Link> этого видео.
							</p>
					}
				</Text>
			</BlocksWrapper>
		);
	};

	renderAnnotationContent = (showedAnnotation: boolean, annotation: Annotation, googleDocLink: string) => {
		const titleClassName = showedAnnotation ? styles.opened : styles.closed;

		return (
			<React.Fragment>
				<h3 className={ classNames(styles.annotationTitle, titleClassName) } onClick={ this.toggleAnnotation }>
					Содержание видео
					<span className={ styles.annotationArrow }>
						{ showedAnnotation
							? <ArrowChevronUp/>
							: <ArrowChevronDown/> }
					</span>
				</h3>
				{ showedAnnotation &&
					<React.Fragment>
						<p>{ annotation.text }</p>
						{ annotation.fragments.map(({ text, offset }) => {
							const [hours, minutes, seconds] = offset.split(':');
							const [hoursAsInt, minutesAsInt, secondsAsInt] = [hours, minutes, seconds].map(
								t => Number.parseInt(t));
							const timeInSeconds = hoursAsInt * 60 * 60 + minutesAsInt * 60 + secondsAsInt;
							return (
								<p key={ offset }>
									<Link onClick={ () => this.setVideoTime(timeInSeconds) }>
										{ hoursAsInt > 0 && `${ hours }:` }
										{ `${ minutes }:` }
										{ seconds }
									</Link>
									{ ` — ${ text }` }
								</p>
							);
						})
						}
						<p>
							Ошибка в содержании? <Link target="_blank" href={ googleDocLink }>Предложите
							исправление!</Link>
						</p>
					</React.Fragment>
				}
			</React.Fragment>
		);
	};

	toggleAnnotation = () => {
		this.setState({
			showedAnnotation: !this.state.showedAnnotation,
		});
	};

	setVideoTime = (seconds: number) => {
		this.ytPlayer?.seekTo(seconds, true);
	};
}

export default withCookies(Video);
