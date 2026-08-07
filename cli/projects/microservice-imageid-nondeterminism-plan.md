# Plan: microservice `deploy plan` phantom "Updating service" (non-deterministic image id)

Status: **root cause confirmed + fixed (pending user's own end-to-end test).** Separate from the
bundle/portal-extension diff work (that is fixed + verified). This is in the shared microservice
build path.

## CONFIRMED ROOT CAUSE (supersedes the two-options analysis below)

Empirically established on the user's machine (Docker 29.5.3 / buildx 0.34.1-desktop):

1. `diff -rq bin/beamApp` across rebuilds → **published bytes identical** (the `dotnet publish` is
   deterministic). So source→artifact is fine.
2. Yet `imageId` changes every build. `docker save` + config-blob diff of two builds showed the images
   are byte-identical **except** an attached **`attestation-manifest`** whose digest differs. The real
   image manifest (`sha256:68413612…`, config + layers) is **identical** across builds.
3. Recent buildx/BuildKit attach a **provenance attestation by default**. It embeds build timestamps,
   so the exported OCI **image index** digest changes every build. That index digest is exactly what
   the CLI parses from `writing image sha256:…` and stores as `imageId`.
4. The service checksum is `hash(serviceName;imageId;…)`, so a new index digest → new checksum →
   phantom "Updating service" + wasted re-upload.

**Not a CLI code regression.** Git archaeology found no offending commit — the checksum has always been
image-id-based, and the post-#4320 build-command commits are behavior-neutral. It "used to work" on
older Docker that didn't attach provenance by default; a Docker/buildx upgrade (surfacing via the new
net10 build path) flipped the default on.

## FIX (implemented + empirically verified end-to-end)

`cli/cli/Commands/Services/ServicesBuildCommand.cs`, two changes that go together:

1. **`--provenance=false --sbom=false`** on `docker buildx build`. No attestation manifest is attached,
   so the exported image is a single manifest whose **store id is reproducible** across rebuilds of
   unchanged source (instead of an index whose digest churns with the attestation's timestamps).
2. **Capture the image id via `docker inspect {tag} --format {{.Id}}`** after the build (status scrape
   kept only as a last-resort fallback). Two reasons:
   - Disabling attestations changes BuildKit's exporter wording (`exporting manifest sha256:…` instead
     of `writing image sha256:…`), so the old `BuildkitStatusUtil` scrape found nothing → "could not
     identify image ID" hard failure.
   - The exporter's `exporting manifest sha256:…` digest is a **sub-manifest digest that `docker image
     save` cannot resolve** (this broke `deploy release` upload: "No such image"). `docker inspect .Id`
     returns the canonical **store id**, which is what `docker image save` and the registry push accept.

   (An earlier attempt used `--iidfile`; it returned that non-save-able sub-manifest digest, which is
   why release failed. `docker inspect .Id` is the correct source.)

Verified on the reporting machine (Docker 29.5.3 / buildx 0.34.1-desktop) with the patched CLI:
- Two consecutive full `deploy plan`s → `docker inspect service-test:latest .Id` identical
  (`sha256:95be01ae…`) → **reproducible**.
- `docker image save 95be01aedad0` (the short id the CLI stores) succeeds and the archive contains
  `manifest.json` → **upload path satisfied**.

3. **`--build-arg SOURCE_DATE_EPOCH=0`.** `--provenance=false` alone made the id stable only while the
   BuildKit `COPY` cache stayed warm (a cache hit reuses the cached config, including its original
   `created` timestamp). A **cold** rebuild — which happens across `deploy release` / cache eviction —
   regenerated the config with a fresh `created` and produced a new id, so the phantom "Updating"
   returned after a release. `SOURCE_DATE_EPOCH` pins the config `created` (and clamps layer file
   timestamps) to a fixed value, making the id content-addressed and time-independent.

Verified on the reporting machine (Docker 29.5.3 / buildx 0.34.1-desktop):
- Two `--no-cache` (cold) builds with `--provenance=false --build-arg SOURCE_DATE_EPOCH=0` →
  identical store id `sha256:3df15c69…` → **reproducible regardless of cache state** (without
  SOURCE_DATE_EPOCH, two `--no-cache` builds gave different config digests).
- `docker image save 3df15c69d69d` succeeds with a `manifest.json` archive → **upload path satisfied**.

> Note: `--provenance=false` + `docker inspect .Id` was necessary but **not sufficient** — it was only
> warm-cache-stable. `SOURCE_DATE_EPOCH` is what makes it truly reproducible across cold builds.

Expect a **one-time** "Updating service" on the first plan after the switch, then deploy → re-plan is
clean and stays stable across releases and cache evictions. Consumers must be reinstalled with the
patched CLI (`beam` on PATH) to pick this up.

Not pursued: `SOURCE_DATE_EPOCH`/`rewrite-timestamp` (the `created` timestamp turned out not to be the
cause — it was identical across builds) and the content-checksum rework (unnecessary once the id is
reproducible).

---

## (Earlier analysis — kept for context; the two options below were pre-confirmation)

## Symptom

Clean realm, one microservice (`service-test`) + one bundle deployed successfully. Re-running
`beam deploy plan` with no source changes still reports:

```
Updating 1 service
 - service-test [f2abc9e53000]->[f04424f714b6]
Uploading 1 service
 - service-test
```

The bundle correctly shows no change. Confirmed by the user: the `nextImageId` is **different on every
run** — the build is non-deterministic.

## Root cause

- The image id is BuildKit's image-config digest, parsed from the `writing image sha256:…` status line
  (`ServicesBuildCommand.cs` ~561-571, `BuildkitStatusUtil.TryGetImageId`). BuildKit stamps a fresh
  `created` timestamp into the config and captures fresh file **mtimes** into the `COPY` layers each
  build (`beamable.templates/templates/BeamService/Dockerfile`), so identical source → new image id.
- `-p:Deterministic="True"` (`ServicesBuildCommand.cs` ~272) fixes only the assembly MVID/PE timestamp,
  not the image. No `SOURCE_DATE_EPOCH` / `--output …,rewrite-timestamp=true` anywhere. No
  `ContinuousIntegrationBuild`.
- The server stores the CLI's `ShortImageId` verbatim (push tag = imageId, `ServiceUploadUtil.cs` ~338;
  round-trips through `ConvertToBeamoV2`/`ConvertToManifestView` unchanged). So the mismatch is 100%
  client-side.
- Change detection keys off the volatile image id twice: `servicesToUpload` (`DeploymentService.cs`
  ~1863, `imageId != remoteService.imageId`) and the service checksum
  `hash(serviceName;imageId;enabled;templateId)` (`BeamoExtensions.cs:19-23`, via `ResetChecksum` in
  `EnsureEntriesHaveChecksums`). Both fire on every plan.
- There is **no** source-content checksum for a microservice today; the image id is the sole key.

## Two fix approaches

### Option A — make the image build reproducible (root-cause)
Set `SOURCE_DATE_EPOCH` (constant or source-derived) and export with BuildKit
`--output type=…,rewrite-timestamp=true` so the config `created` and the COPY-layer mtimes are
normalized; add `ContinuousIntegrationBuild=true` to the publish. Same source ⇒ same image id ⇒
`servicesToUpload`/checksum naturally match ⇒ no phantom diff, and no wasted re-upload.
- **Pros:** fixes the actual nondeterminism; no change to diff/checksum logic; genuinely reproducible
  artifacts.
- **Cons/risks:** `rewrite-timestamp` needs BuildKit ≥ 0.11 (recent buildx/Docker Desktop) — an
  environmental dependency across all users; base image tag `dotnet/runtime:8.0-alpine` is mutable, so
  a base update still (legitimately) changes the id; reproducible *container* builds are fiddly and I
  can't fully verify id-stability without running Docker in target environments.

### Option B — content-checksum change detection (works around the volatile id) [tentative recommendation]
Compute a deterministic hash of the service's **publish output** (the bytes that get COPY'd into the
image — stable because `Deterministic=True` makes the assemblies byte-stable), store it as the
service's checksum, and use it as the change key instead of the image id. When the local content hash
equals the deployed one: inherit the remote image id into `next`, and **skip the Docker build + upload
entirely** (no diff, and a large speed-up on no-op plans). When it differs: rebuild → new id → upload
→ change. Mirrors how portal extensions already do change detection (content hash of built assets, not
a docker id).
- **Pros:** fully within CLI control (no Docker-version dependency); consistent with the PE pattern;
  skips needless rebuilds/uploads when unchanged.
- **Cons/risks:** changes what the service `checksum` means (self-consistent, but one migration rebuild
  the first time after the change since the stored checksum is still image-id-based); relies on
  `Deterministic=True` making the publish output byte-stable (needs a quick confirm); more moving parts
  in the build/deploy flow than Option A.

## Open decision
Which approach (or both)? Recommendation: **Option B** for robustness + the no-op speed-up, optionally
layering Option A later for true build reproducibility. Blast radius: the microservice build/deploy
path used by all `beam deploy` users — worth explicit sign-off before implementing.
