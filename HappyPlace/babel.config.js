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
          hooks: './src/hooks',
          screens: './src/screens',
          services: './src/services',
          store: './src/store',
        },
        extensions: ['.js', '.jsx', '.json', '.ts', '.tsx'],
      },
    ],
  ],
};