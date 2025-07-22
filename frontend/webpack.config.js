// webpack.config.js
const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');

module.exports = (env, argv) => {
  const isProduction = argv.mode === 'production';
  
  return {
    entry: './src/index.tsx',
    output: {
      path: path.resolve(__dirname, 'dist'),
      filename: '[name].[contenthash].js',
      clean: true,
    },
    devtool: isProduction ? 'source-map' : 'inline-source-map',
    devServer: {
      static: './dist',
      hot: true,
      proxy: {
        '/hub': {
          target: 'https://localhost:5001',
          ws: true,
          secure: false
        }
      }
    },
    module: {
      rules: [
        {
          test: /\.tsx?$/,
          use: 'ts-loader',
          exclude: /node_modules/,
        },
        {
          test: /\.css$/,
          use: [
            isProduction ? MiniCssExtractPlugin.loader : 'style-loader',
            'css-loader',
          ],
        },
      ],
    },
    resolve: {
      extensions: ['.tsx', '.ts', '.js'],
    },
    plugins: [
      new HtmlWebpackPlugin({
        template: './src/index.html',
      }),
      ...(isProduction ? [new MiniCssExtractPlugin({
        filename: 'css/[name].[contenthash].css',
      })] : []),
    ],
    optimization: {
      splitChunks: {
        chunks: 'all',
      },
    },
  };
};
