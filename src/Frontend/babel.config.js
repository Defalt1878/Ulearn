module.exports = function (api) {
	api.cache(true);

	const presets = [
		["@babel/preset-env",
			{
				modules: false,
				// useBuiltIns: "usage",
				// corejs: { version: "3.23", proposals: true }
			}
		],
		"@babel/preset-react",
		"@babel/preset-typescript",
	];

	const plugins = [
		// "@babel/plugin-proposal-class-properties",
		// "@babel/plugin-proposal-object-rest-spread",
		// "@babel/plugin-transform-arrow-functions",
		// '@babel/plugin-proposal-private-methods',
	];

	return {
		presets,
		plugins
	}
};
