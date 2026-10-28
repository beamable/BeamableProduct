// Builds the CLI's local web apps into ../cli/LocalWeb/, which cli.csproj embeds into the CLI:
//   LocalWeb/<app>/   index.html, index.js and style.css of `beam local management` (stacks) and `beam bundles management`
//   LocalWeb/runtime/ the portal's components, theme and React runtime the apps load first
// Each app is built like a React portal extension (definePortalExtensionConfig): React and the Beam* components come
// from the runtime's window globals, not from the app's bundle.

import { execSync } from 'node:child_process'
import { copyFileSync, cpSync, existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { basename, dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const here = dirname(fileURLToPath(import.meta.url))
const out = resolve(here, '../cli/LocalWeb')
const toolkit = resolve(here, 'node_modules/@beamable/portal-toolkit')
const apps = [
  { name: 'stacks', title: 'Beam Local Stacks' },
  { name: 'bundles', title: 'Beam Bundles' },
]

// The toolkit is this repository's (beam-portal-toolkit); its build helpers come from its dist.
if (!existsSync(resolve(toolkit, 'dist/vite.mjs'))) {
  execSync('pnpm install --frozen-lockfile && pnpm build', { cwd: resolve(here, '../../beam-portal-toolkit'), stdio: 'inherit' })
}

// The runtime is built from agentic-portal by the toolkit's sync (`PORTAL_REPO_PATH=… pnpm sync-components`). Where
// that hasn't run, it is built here from the agentic-portal checkout: PORTAL_REPO_PATH, else the one next to this
// repository (agentic-portal_<suffix> beside BeamableProduct_<suffix>, then agentic-portal). Without one (CI has none),
// the published toolkit's runtime is used: BEAM_PORTAL_RUNTIME_VERSION, or the latest release.
let runtime = resolve(toolkit, 'runtime')
let unpacked = null
const productDir = resolve(here, '../..')
const suffix = /^BeamableProduct(_.+)?$/.exec(basename(productDir))?.[1] ?? ''
const portalRepo = [process.env.PORTAL_REPO_PATH, resolve(productDir, '..', `agentic-portal${suffix}`), resolve(productDir, '../agentic-portal')]
  .find(dir => dir && /"build:runtime"/.test(existsSync(resolve(dir, 'package.json')) ? readFileSync(resolve(dir, 'package.json'), 'utf8') : ''))
if (!existsSync(resolve(runtime, 'beam-runtime.js')) && portalRepo) {
  console.log(`Building the portal runtime from ${portalRepo}`)
  try {
    if (!existsSync(resolve(portalRepo, 'node_modules'))) execSync('npm ci', { cwd: portalRepo, stdio: 'inherit' })
    execSync('npm run build:runtime', { cwd: portalRepo, stdio: 'inherit' })
    mkdirSync(runtime, { recursive: true })
    for (const file of ['beam-runtime.js', 'beam-runtime.css']) copyFileSync(resolve(portalRepo, 'dist-runtime', file), resolve(runtime, file))
  } catch (err) {
    console.error(`Could not build the portal runtime in ${portalRepo}: ${err.message}`)
  }
}
if (!existsSync(resolve(runtime, 'beam-runtime.js'))) {
  const version = process.env.BEAM_PORTAL_RUNTIME_VERSION || 'latest'
  unpacked = mkdtempSync(join(tmpdir(), 'beam-runtime-'))
  try {
    const tarball = execSync(`npm pack @beamable/portal-toolkit@${version} --silent`, { cwd: unpacked }).toString().trim().split('\n').pop()
    execSync(`tar -xzf "${tarball}"`, { cwd: unpacked })
    runtime = resolve(unpacked, 'package/runtime')
  } catch (err) {
    console.error(`Could not fetch @beamable/portal-toolkit@${version}: ${err.message}`)
  }
}
if (!existsSync(resolve(runtime, 'beam-runtime.js'))) {
  console.error(
    '\nNo portal runtime: this toolkit has none built, no agentic-portal checkout with `build:runtime` was found next to\n' +
    'this repository, and the published toolkit has none. Clone agentic-portal next to BeamableProduct (or set\n' +
    'PORTAL_REPO_PATH to it), or set BEAM_PORTAL_RUNTIME_VERSION to a toolkit release that ships runtime/.\n',
  )
  if (unpacked) rmSync(unpacked, { recursive: true, force: true })
  process.exit(1)
}

const { build } = await import('vite')
const { definePortalExtensionConfig } = await import('@beamable/portal-toolkit/vite')

rmSync(out, { recursive: true, force: true })
cpSync(runtime, resolve(out, 'runtime'), { recursive: true })
if (unpacked) rmSync(unpacked, { recursive: true, force: true })

const page = readFileSync(resolve(here, 'shared/index.html'), 'utf8')
for (const app of apps) {
  const config = await definePortalExtensionConfig({
    entry: resolve(here, app.name, 'main.tsx'),
    name: `beam_local_${app.name}`,
    outDir: resolve(out, app.name),
    minify: true,
  })
  // The folder was cleared above; vite won't empty one outside its root anyway.
  await build({ ...config, build: { ...config.build, emptyOutDir: false }, configFile: false, root: here, logLevel: 'warn' })
  writeFileSync(resolve(out, app.name, 'index.html'), page.replace('__TITLE__', app.title))
  console.log(`✓ ${app.name}  →  cli/LocalWeb/${app.name}/`)
}
