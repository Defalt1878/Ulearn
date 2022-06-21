import autoprefixer from "autoprefixer";
import webpack, { Configuration } from "webpack";
import HtmlWebpackPlugin from "html-webpack-plugin";
import { WebpackManifestPlugin } from "webpack-manifest-plugin";
import MiniCssExtractPlugin from "mini-css-extract-plugin";
import paths from "./paths";
import pwaPlugins from "./pwa.webpack.plugins";
import base from './webpack.config.base';
import { merge } from "webpack-merge";

const shouldUseSourceMap = true;
const cssFilename = paths.static.css + '/[name].[contenthash:8].css';
const chunkCssFilename = paths.static.css + '/[id].[contenthash:8].css';

// This is the production configuration.
// It compiles slowly and is focused on producing a fast and minimal bundle.
// The development configuration is different and lives in a separate file.

const config: Configuration = {
	mode: 'production',
	bail: true,
	entry: {
		oldBrowser: paths.oldBrowserJs,
		main: [paths.legacy, paths.appIndexTsx],
	},
	output: {
		path: paths.appBuild,
		filename: paths.static.js + '/[name].[chunkhash:8].js',
		chunkFilename: paths.static.js + '/[name].[chunkhash:8].chunk.js',
		publicPath: '/',
		clean: true,
	},
	resolve: {
		extensions: ['.ts', '.tsx', '.js', '.json']
	},
	module: {
		strictExportPresence: true,
		rules: [
			{
				oneOf: [
					{
						test: [/\.bmp$/, /\.gif$/, /\.jpe?g$/, /\.png$/],
						loader: 'url-loader',
						options: {
							limit: 10000,
							name: paths.static.media + '/[name].[contenthash:8].[ext]',
						},
					},
					{
						test: /\.(js|jsx|mjs|ts|tsx)$/,
						loader: 'babel-loader',
						include: paths.appSrc,
						options: {
							configFile: "./babel.config.js",
							cacheDirectory: true,
							compact: true,
						},
					},
					{
						test: /\.less$/,
						use: [
							{
								loader: MiniCssExtractPlugin.loader,
								options: {},
							},
							{
								loader: "css-loader",
								options: {
									esModule: false,
									sourceMap: shouldUseSourceMap,
									modules: {
										mode: 'local',
										localIdentName: '[contenthash:base64:5]',
									},
									importLoaders: 2,
								}
							},
							{
								loader: 'postcss-loader',
								options: {
									postcssOptions: {
										ident: 'postcss',
										plugins: [
											"postcss-preset-env",
											{
												autoprefixer: { flexbox: 'no-2009' }
											},
										]
									}
								},
							},
							{
								loader: 'less-loader',
								options: {
									sourceMap: shouldUseSourceMap,
								}
							},
						],
					},
					{
						test: /\.css$/,
						use: [
							'style-loader',
							{
								loader: 'css-loader',
								options: {
									esModule: false,
									modules: {
										auto: (resourcePath: string) => !resourcePath.endsWith('.global.css'),
										mode: 'global',
									},
									importLoaders: 1,
								},
							},
							{
								loader: 'postcss-loader',
								options: {
									postcssOptions: {
										ident: 'postcss',
										plugins: [
											"postcss-preset-env",
											{
												autoprefixer: { flexbox: 'no-2009' }
											},
										]
									}
								},
							},
						],
					},
					{
						loader: 'file-loader',
						exclude: [/\.(js|jsx|mjs|ts|tsx)$/, /\.html$/, /\.json$/],
						options: {
							name: paths.static.media + '/[name].[contenthash:8].[ext]',
						},
					},
					// ** STOP ** Are you adding a new loader?
					// Make sure to add the new loader(s) before the "file" loader.
				],
			},
		],
	},
	plugins: [
		// Generates an `index.html` file with the <script> injected.
		new HtmlWebpackPlugin({
			inject: true,
			template: paths.appHtml,
			favicon: paths.appPublic + '/favicon.ico',
			minify: {
				removeComments: true,
				collapseWhitespace: true,
				removeRedundantAttributes: true,
				useShortDoctype: true,
				removeEmptyAttributes: true,
				removeStyleLinkTypeAttributes: true,
				keepClosingSlash: true,
				minifyJS: true,
				minifyCSS: true,
				minifyURLs: true,
			},
			chunksSortMode: (chunk1, chunk2) => {
				/* oldBrowser.js should be the first bundle. For more complex cases see solution
				   at https://github.com/jantimon/html-webpack-plugin/issues/481#issuecomment-287370259*/
				if(chunk1 === 'oldBrowser') {
					return -1;
				}
				if(chunk2 === 'oldBrowser') {
					return 1;
				}
				return 0;
			},
		}),
		new webpack.ProvidePlugin({
			process: 'process/browser',
			$: 'jquery',
			jQuery: 'jquery',
			"window.$": 'jquery',
			"window.jQuery": 'jquery',
		}),
		// See https://github.com/webpack-contrib/mini-css-extract-plugin for details
		new MiniCssExtractPlugin({
			// Options similar to the same options in webpackOptions.output
			// both options are optional
			filename: cssFilename,
			chunkFilename: chunkCssFilename
		}),
		// Generate a manifest file which contains a mapping of all asset filenames
		// to their corresponding output file so that tools can pick it up without
		// having to parse `index.html`.
		new WebpackManifestPlugin({
			fileName: 'asset-manifest.json',
		}),
		// Moment.js is an extremely popular library that bundles large locale files
		// by default due to how Webpack interprets its code. This is a practical
		// solution that requires the user to opt into importing specific locales.
		// https://github.com/jmblog/how-to-optimize-momentjs-with-webpack
		// You can remove this if you don't use Moment.js:
		new webpack.IgnorePlugin({
			resourceRegExp: /^\.\/locale$/,
			contextRegExp: /moment$/,
		}),
		...pwaPlugins,
	],
	optimization: {
		minimize: true,
		runtimeChunk: 'single',
	},
};

export default merge([base, config]);
