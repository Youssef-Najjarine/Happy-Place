module.exports = {
  presets: ['module:@react-native/babel-preset'],
  plugins: [
    [
      'module-resolver',
      {
        root: ['./'],
        alias: {
          assets: './assets',
          components: './src/components',
          screens: './src/screens',
          store: './src/store',
        },
        extensions: ['.js', '.jsx', '.json', '.ts', '.tsx'],
      },
    ],
  ],
};