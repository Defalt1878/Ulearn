import HtmlWebpackPlugin from "html-webpack-plugin";
import MiniCssExtractPlugin from "mini-css-extract-plugin";
import webpack, { Chunk, Configuration, Module } from "webpack";
import { WebpackManifestPlugin } from "webpack-manifest-plugin";
import { merge } from "webpack-merge";
import paths from "./paths";
import pwaPlugins from "./pwa.webpack.plugins";
import base from './webpack.config.base';

const shouldUseSourceMap = true;
const cssFilename = paths.static.css + '/[name].[contenthash:8].css';
const chunkCssFilename = paths.static.css + '/[name].[contenthash:8].css';

// This is the production configuration.
// It compiles slowly and is focused on producing a fast and minimal bundle.
// The development configuration is different and lives in a separate file.

const config: Configuration = {
	mode: 'production',
	bail: true,
	entry: {
		main: [paths.legacy, paths.appIndexTsx]
	},
	output: {
		path: paths.appBuild,
		filename: paths.static.js + '/[name].[contenthash:8].js',
		chunkFilename: paths.static.js + '/[name].[contenthash:8].chunk.js',
		publicPath: '/',
		clean: true
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
							name: paths.static.media + '/[name].[contenthash:8].[ext]'
						}
					},
					{
						test: /\.(js|jsx|mjs|ts|tsx)$/,
						loader: 'babel-loader',
						include: paths.appSrc,
						options: {
							configFile: "./babel.config.cjs",
							cacheDirectory: true,
							compact: true
						}
					},
					{
						test: /\.less$/,
						use: [
							{
								loader: MiniCssExtractPlugin.loader,
								options: {}
							},
							{
								loader: "css-loader",
								options: {
									esModule: false,
									sourceMap: shouldUseSourceMap,
									modules: {
										mode: 'local',
										localIdentName: '[contenthash:base64:5]'
									},
									importLoaders: 2
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
											}
										]
									}
								}
							},
							{
								loader: 'less-loader',
								options: {
									sourceMap: shouldUseSourceMap
								}
							}
						]
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
										mode: 'global'
									},
									importLoaders: 1
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
											}
										]
									}
								}
							}
						]
					},
					{
						loader: 'file-loader',
						exclude: [/\.(js|jsx|mjs|ts|tsx)$/, /\.html$/, /\.json$/, /^$/],
						options: {
							name: paths.static.media + '/[name].[contenthash:8].[ext]'
						}
					}
					// ** STOP ** Are you adding a new loader?
					// Make sure to add the new loader(s) before the "file" loader.
				]
			}
		]
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
				minifyURLs: true
			}
		}),
		new webpack.ProvidePlugin({
			process: 'process/browser.js',
			$: 'jquery',
			jQuery: 'jquery',
			"window.$": 'jquery',
			"window.jQuery": 'jquery'
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
			fileName: 'asset-manifest.json'
		}),
		// Moment.js is an extremely popular library that bundles large locale files
		// by default due to how Webpack interprets its code. This is a practical
		// solution that requires the user to opt into importing specific locales.
		// https://github.com/jmblog/how-to-optimize-momentjs-with-webpack
		// You can remove this if you don't use Moment.js:
		new webpack.IgnorePlugin({
			resourceRegExp: /^\.\/locale$/,
			contextRegExp: /moment$/
		}),
		...pwaPlugins
	],
	optimization: {
		minimize: true,
		usedExports: true,
		runtimeChunk: 'single',
		splitChunks: {
			chunks: 'all',
			minSize: 200 * 1000,
			maxSize: 800 * 1000,
			name: (_module: Module, chunks: Chunk[]) =>
				chunks.map((chunk) => chunk.name).join('-'),
			cacheGroups: {
				reactVendor: {
					test: /[\\/]node_modules[\\/](react|react-dom|react-router-dom)[\\/]/,
					name: 'vendor-react',
					chunks: 'all'
				},
				corejsVendor: {
					test: /[\\/]node_modules[\\/](core-js)[\\/]/,
					name: 'vendor-corejs',
					chunks: 'all'
				},
				katexVendor: {
					test: /[\\/]node_modules[\\/](katex)[\\/]/,
					name: 'vendor-katex',
					chunks: 'all'
				},
				highchartsVendor: {
					test: /[\\/]node_modules[\\/](highcharts)[\\/]/,
					name: 'vendor-highcharts',
					chunks: 'all'
				},
				jqueryVendor: {
					test: /[\\/]node_modules[\\/](jquery|jquery-ui|webpack-jquery-ui)[\\/]/,
					name: 'vendor-jquery',
					chunks: 'all'
				},
				bootstrapVendor: {
					test: /[\\/]node_modules[\\/](bootstrap|bootstrap-fileinput|bootstrap-select)[\\/]/,
					name: 'vendor-bootstrap',
					chunks: 'all'
				},
				codeMirrorVendor: {
					test: /[\\/]node_modules[\\/](codemirror|react-codemirror2)[\\/]/,
					name: 'vendor-codemirror',
					chunks: 'all'
				},
				momentVendor: {
					test: /[\\/]node_modules[\\/](moment|moment-timezone)[\\/]/,
					name: 'vendor-moment',
					chunks: 'all'
				},
				konturVendor: {
					test: /[\\/]node_modules[\\/](@skbkontur)[\\/]/,
					name: 'vendor-kontur',
					chunks: 'all'
				}
			}
		}
	}
};

export default merge([base, config]);
