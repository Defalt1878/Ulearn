import path from "path";
import { Configuration } from "webpack";
import paths from "./paths";

export default {
	resolve: {
		alias: {
			ui: path.resolve(paths.appNodeModules, '@skbkontur/react-ui'),
			icons: path.resolve(paths.appNodeModules, '@skbkontur/react-icons'),
			src: path.resolve(paths.appSrc),
			//if u adding a new one don't forget to add it to package.json jest.moduleNameMapper section, ot *.test.* won't work
		}
	},
} as Configuration;
