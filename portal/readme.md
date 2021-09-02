# Beamable - Portal

## System Setup

Install Node & NPM:
```sh
brew install node
node -v                       # v10.x.x
npm -v                        # 6.x.x
```


## Project Setup

Install project dependencies:
```sh
npm install
```
<hr>

## Development

Start a [local development server](http://localhost:10001) and begin watching source files for changes:
```sh
npm run start
```

Run tests with... (optionally, remove the watch flag to run them as a one-off)
```sh
npm run test:watch
```

## Production

Execute the build producing **production mode** artifacts:
```sh
PORTAL_ENV=prod npm run build:prod
```

Its worth noting the difference between the `PORTAL_ENV` variable, and the prod tag on the yarn build job. The prod tag on yarn builds the application with `NODE_ENV` set to production, yielding an production worthy application. The `PORTAL_ENV` variable dictates what environment the production worthy application should be pointed to. By default, the application will point to the dev environment (hitting dev.api.beamable), but when the variable is set to `prod`, then the api routes are directed towards the production apis. In both cases, the code itself is compiled as production nodejs. 
