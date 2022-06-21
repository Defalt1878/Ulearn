module.exports = {
	parser: "@babel/eslint-parser",
	env: {
		browser: true,
		commonjs: true,
		es6: true,
		jest: true
	},
	parserOptions: {
		allowImportExportEverywhere: true,
		babelOptions: {
			presets: [
				["@babel/preset-env", { modules: false, }],
				"@babel/preset-react",
				"@babel/preset-typescript"
			]
		}
	},
	overrides: [
		{
			parser: "@typescript-eslint/parser",
			files: [
				"*.{ts,tsx}",
				"global.d.ts"
			],
			rules: {
				"no-mixed-spaces-and-tabs": 0
			},
			parserOptions: {
				project: "tsconfig.json",
				tsconfigRootDir: "."
			},
			plugins: [
				'@typescript-eslint',
			],
			extends: [
				'eslint:recommended',
				'plugin:@typescript-eslint/eslint-recommended',
				'plugin:@typescript-eslint/recommended'
			],
		}]
};
