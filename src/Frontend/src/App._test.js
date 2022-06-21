import React from 'react';
import UlearnApp from "./App";
import { createRoot } from 'react-dom/client';

it('renders without crashing', () => {
	const container = document.createElement('div');
	const root = createRoot(container);
	root.render(<UlearnApp/>);
	root.unmount();
});
