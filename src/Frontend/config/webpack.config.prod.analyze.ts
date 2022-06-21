import base from "./webpack.config.prod";
import { BundleAnalyzerPlugin } from "webpack-bundle-analyzer";
import { merge } from "webpack-merge";

const config = { plugins: [new BundleAnalyzerPlugin()] };

export default merge([base, config]);
