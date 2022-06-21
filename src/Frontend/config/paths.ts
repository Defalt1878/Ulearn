import path from "path";
import fs from "fs-extra";

const appDirectory = fs.realpathSync(process.cwd());
const resolveApp = (relativePath: string) => path.resolve(appDirectory, relativePath);

const contentPath = 'static';

export default {
	appBuild: resolveApp('build'),
	appPublic: resolveApp('public'),
	appHtml: resolveApp('public/index.html'),
	appIndexTsx: resolveApp('src/index.tsx'),
	legacy: resolveApp('src/legacy/legacy.ts'),
	appSrc: resolveApp('src'),
	yarnLockFile: resolveApp('yarn.lock'),
	appNodeModules: resolveApp('node_modules'),
	oldBrowserJs: resolveApp('src/oldBrowser.js'),
	webUrl: 'https://localhost:44300',

	static: {
		css: contentPath + '/css',
		js: contentPath + '/js',
		media: contentPath + '/media',
	}
};
