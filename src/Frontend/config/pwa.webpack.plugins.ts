import paths from "./paths";
import WebpackPwaManifest from "webpack-pwa-manifest";
import { GenerateSW } from "workbox-webpack-plugin";
import { WebpackPluginInstance  } from "webpack";

const plugins: WebpackPluginInstance [] = [
	// as conversion allows to escape type bug
	new WebpackPwaManifest({
		filename: "manifest.json",
		inject: true,
		fingerprints: true,
		start_url: '/',
		scope: '/',

		name: 'Ulearn.me',
		short_name: 'Ulearn',
		description: 'Интерактивные онлайн-курсы по программированию',
		background_color: '#ffffff',
		theme_color: "#000000",
		prefer_related_applications: true,
		related_applications: [],
		ios: true,

		icons: [
			{
				src: paths.appPublic + '/logo.png',
				sizes: [512, 256, 192, 128, 64, 32,],
				type: "image/png",
				purpose: "any maskable",
				destination: paths.static.media + '/icons',
				ios: true,
			},
			{
				src: paths.appPublic + '/favicon.ico',
				sizes: [16,],
				type: "image/x-icon",
				purpose: "any maskable",
				destination: paths.static.media + '/icons',
				ios: 'startup',
			}
		],
	}) as WebpackPluginInstance,
	// Generate a service worker script that will precache, and keep up to date,
	// the HTML & assets that are part of the Webpack build.
	new GenerateSW({
		mode: process.env.NODE_ENV,
		swDest: 'sw',
		exclude: [/\.map$/, /asset-manifest\.json$/, /\.(?:png|jpg|jpeg|svg)/],
		navigateFallbackAllowlist: [/^(?!\/__).*/],
		maximumFileSizeToCacheInBytes: 1024 * 1024 * 5,
		skipWaiting: true,
		// Define runtime caching rules.
		runtimeCaching: [{
			// Match any request that ends with .png, .jpg, .jpeg or .svg.
			// serve only images from static or content, such as logo, icons, 404 and others
			// do not cache images from /course route as there can be a lot of images in galleries
			urlPattern: /(\/static|\/content).*\.(?:png|jpg|jpeg|svg)/i,
			// Apply a cache-first strategy.
			handler: 'CacheFirst',
			options: {
				// Use a custom cache name.
				cacheName: 'images',
				// Only cache 10 images.
				expiration: {
					maxEntries: 10,
				},
			},
		}],
	})
];

export default plugins;
