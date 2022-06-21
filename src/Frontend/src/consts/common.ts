import React, { ReactFragment, ReactPortal } from "react";

export const botId = 'bot';
export const botName = 'Юрий Юлернович';

export interface HasReactChild {
	children?: React.ReactNode;
}

export interface HasReactChildStrict {
	children: React.ReactElement | ReactFragment | ReactPortal;
}
