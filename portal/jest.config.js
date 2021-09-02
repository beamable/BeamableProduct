module.exports = {
    transform: {
      '^.+\\.svelte$': ["svelte-jester", { "preprocess": true }],
      '^.+\\.js$': 'ts-jest',
      "^.+\\.tsx?$": "ts-jest",
    },
    moduleFileExtensions: ['js', 'ts', 'svelte'],
    testMatch: [
      "<rootDir>/src/**/*.spec.ts"
    ],
    globals: {
      'ts-jest': {
        tsConfig: "tsconfig.test.json"
      }
    },
    moduleDirectories: ["node_modules", "fixtures", "src"],
  }