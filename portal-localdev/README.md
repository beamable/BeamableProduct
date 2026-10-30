# portal-localdev

The local registry for `@beamable/sdk` and `@beamable/portal-toolkit` builds:

| Service | Port | Purpose |
|---------|------|---------|
| **Verdaccio** | 4873 | Local npm registry. Holds local builds; proxies npmjs for everything else. |
| **local-unpkg** | 4874 | Serves files from the registry's tarballs in unpkg.com's URL format, so the portal loads an extension's SDK the way it does in production. |

You don't run these by hand. The `:web` variant of `beam local run` (and the `web-dev` stack) starts them as the
`web: registry` step, from the portal workspace:

```bash
beam local run csharp-portal:web
beam local stop          # stops them; `beam local stop --purge` also deletes what was published
```

## Versions

Every local build gets a version of its own: the next minor after the highest version on npmjs, then `-0.` and
the time, for example `0.6.0-0.20261023120000`. Nothing is ever unpublished or republished, so npm's cache, the
lockfiles and the browser never hold a stale copy of a version. Semver puts a local build above every release so
far and below the next one's prereleases (`-dev.N`, `-rc.N`).

Only the stack's `web: install` and the extensions' own installs in a local run talk to this registry, and only for
the `@beamable` scope, with `--no-save`. A local version can't reach a `package.json` or a lockfile.

The portal loads an extension's SDK from `VITE_SDK_CDN_BASE`, unpkg.com when unset. A stack with `:web` starts the
portal with `/local-unpkg`, which Vite proxies to local-unpkg on :4874.
