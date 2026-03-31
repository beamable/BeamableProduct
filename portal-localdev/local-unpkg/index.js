#!/usr/bin/env node
/**
 * local-unpkg — a minimal unpkg-compatible file server.
 *
 * Fetches packages from a local Verdaccio npm registry and serves individual
 * files from their tarballs, mirroring the unpkg.com URL format:
 *
 *   /{package}@{version}/{file}
 *
 * Examples:
 *   /beamable-sdk@0.6.0/dist/browser/index.global.js
 *   /@beamable/portal-toolkit@0.1.2/package.json
 *
 * All resolved files are cached in memory — restart the server to bust the
 * cache (useful when re-publishing a package under the same version locally).
 */

'use strict'

const http = require('http')
const zlib = require('zlib')
const stream = require('stream')
const tar = require('tar-stream')

const VERDACCIO = process.env.VERDACCIO_URL || 'http://localhost:4873'
const PORT = Number(process.env.PORT || 4874)

// ---------------------------------------------------------------------------
// In-memory file cache — keyed by "{pkg}@{version}{/file}"
// ---------------------------------------------------------------------------
/** @type {Map<string, Buffer>} */
const fileCache = new Map()

// ---------------------------------------------------------------------------
// URL parsing
// ---------------------------------------------------------------------------
// Matches:
//   /@scope/name@1.2.3/some/file.js
//   /name@1.2.3/some/file.js
const PACKAGE_RE = /^\/((?:@[^/@]+\/)?[^/@]+)@([^/]+)(\/[^?#]*)$/

// ---------------------------------------------------------------------------
// Tarball extraction
// ---------------------------------------------------------------------------

/**
 * Downloads a tarball from `tarballUrl` and extracts `targetFile` from it.
 * npm tarballs place all files under a `package/` prefix, which is stripped.
 *
 * @param {string} tarballUrl
 * @param {string} targetFile  e.g. "/dist/browser/index.global.js"
 * @returns {Promise<Buffer>}
 */
function extractFileFromTarball(tarballUrl, targetFile) {
  const normalTarget = targetFile.replace(/^\//, '')

  return fetch(tarballUrl).then((tarballRes) => {
    if (!tarballRes.ok) {
      const err = new Error(`Tarball fetch failed (${tarballRes.status}): ${tarballUrl}`)
      err.status = 502
      throw err
    }

    return new Promise((resolve, reject) => {
      const extract = tar.extract()
      let resolved = false

      extract.on('entry', (header, entryStream, next) => {
        // Strip the "package/" prefix npm adds to all tarball entries
        const entryPath = header.name.replace(/^package\//, '')

        if (!resolved && entryPath === normalTarget) {
          resolved = true
          const chunks = []
          entryStream.on('data', (c) => chunks.push(c))
          entryStream.on('end', () => {
            resolve(Buffer.concat(chunks))
            next() // keep draining so the pipeline completes cleanly
          })
          entryStream.on('error', reject)
        } else {
          entryStream.resume()
          entryStream.on('end', next)
        }
      })

      extract.on('finish', () => {
        if (!resolved) {
          const err = new Error(`File not found in tarball: ${targetFile}`)
          err.status = 404
          reject(err)
        }
      })

      extract.on('error', (err) => {
        if (!resolved) reject(err)
      })

      const gunzip = zlib.createGunzip()
      gunzip.on('error', (err) => {
        if (!resolved) reject(err)
      })

      stream.Readable.fromWeb(tarballRes.body).pipe(gunzip).pipe(extract)
    })
  })
}

// ---------------------------------------------------------------------------
// HTTP server
// ---------------------------------------------------------------------------

const CONTENT_TYPES = {
  js: 'application/javascript; charset=utf-8',
  mjs: 'application/javascript; charset=utf-8',
  cjs: 'application/javascript; charset=utf-8',
  json: 'application/json; charset=utf-8',
  ts: 'text/plain; charset=utf-8',
  map: 'application/json; charset=utf-8',
}

const server = http.createServer(async (req, res) => {
  if (req.method !== 'GET' && req.method !== 'HEAD') {
    res.writeHead(405).end()
    return
  }

  const match = PACKAGE_RE.exec(req.url ?? '/')
  if (!match) {
    res.writeHead(400).end('Expected /{package}@{version}/{file}')
    return
  }

  const [, pkg, version, filePath] = match
  const cacheKey = `${pkg}@${version}${filePath}`

  try {
    let content = fileCache.get(cacheKey)

    if (!content) {
      // Ask Verdaccio for the specific version metadata to get the tarball URL
      const metaUrl = `${VERDACCIO}/${encodeURIComponent(pkg).replace('%40', '@')}/${version}`
      const metaRes = await fetch(metaUrl)
      if (!metaRes.ok) {
        const err = new Error(`Package not found: ${pkg}@${version}`)
        err.status = metaRes.status === 404 ? 404 : 502
        throw err
      }

      const meta = await metaRes.json()
      const tarballUrl = meta.dist?.tarball
      if (!tarballUrl) {
        const err = new Error(`No tarball URL in metadata for ${pkg}@${version}`)
        err.status = 404
        throw err
      }

      content = await extractFileFromTarball(tarballUrl, filePath)
      fileCache.set(cacheKey, content)
    }

    const ext = filePath.split('.').pop() ?? ''
    const contentType = CONTENT_TYPES[ext] ?? 'application/octet-stream'

    res.writeHead(200, {
      'Content-Type': contentType,
      'Content-Length': String(content.length),
      'Cache-Control': 'public, max-age=31536000, immutable',
      'Access-Control-Allow-Origin': '*',
    })

    if (req.method === 'HEAD') return res.end()
    res.end(content)
  } catch (err) {
    const status = err.status ?? 500
    console.error(`[${status}] ${cacheKey}: ${err.message}`)
    res.writeHead(status).end(err.message)
  }
})

server.listen(PORT, () => {
  console.log(`local-unpkg  →  http://localhost:${PORT}`)
  console.log(`registry     →  ${VERDACCIO}`)
})
