#!/bin/bash

# Emit GitHub Release notes for one release: the verbatim CHANGELOG.md section for
# $VERSION, taken from each product lane this release actually touches. The lane
# flags are the same COPY_* names upload-changelogs.sh takes, so a release workflow
# declares its lanes identically for both scripts.
#
# Paths resolve from $GITHUB_WORKSPACE rather than the working directory:
# release-web.yml sets defaults.run.working-directory, so a relative path here
# would mean something different in each of the three release workflows.
#
# Optional $TAG adds a per-package link to the full changelog at that tag.

set -eu

: "${VERSION:?VERSION is required}"

ROOT="${GITHUB_WORKSPACE:-$(cd "$(dirname "$0")/../.." && pwd)}"
OUT="${OUT:-${RUNNER_TEMP:-.}/release-notes.md}"
REPO="${GITHUB_REPOSITORY:-beamable/BeamableProduct}"

# Each entry is "<package name>|<changelog path>", in the order it should appear.
lanes=()
if [ "${COPY_UNITY_SDK:-false}" = "true" ]; then
	lanes+=("com.beamable|client/Packages/com.beamable/CHANGELOG.md")
	lanes+=("com.beamable.server|client/Packages/com.beamable.server/CHANGELOG.md")
fi
if [ "${COPY_CLI:-false}" = "true" ]; then
	lanes+=("Beamable.Tools|cli/cli/CHANGELOG.md")
	lanes+=("Beamable.Server|microservice/microservice/CHANGELOG.md")
fi
if [ "${COPY_WEB_SDK:-false}" = "true" ]; then
	lanes+=("Beamable Web SDK|web/CHANGELOG.md")
fi

if [ ${#lanes[@]} -eq 0 ]; then
	echo "No lane selected; set COPY_UNITY_SDK, COPY_CLI, or COPY_WEB_SDK to 'true'." >&2
	exit 1
fi

# Print the body of the "## [$1] - <date>" section of changelog $2, stopping at the
# next "## " heading, with leading and trailing blank lines dropped. Matching the
# bracketed version at exactly column 4 keeps [6.1.0] from matching [16.1.0].
slice_section() {
	awk -v want="[$1]" '
		/^## /                  { if (inside) exit; inside = (index($0, want) == 4); next }
		!inside                 { next }
		/^[[:space:]]*$/        { if (started) blanks++; next }
		                        { while (blanks-- > 0) print ""; blanks = 0; started = 1; print }
	' "$2"
}

: > "$OUT"
found_any=false

for lane in "${lanes[@]}"; do
	name="${lane%%|*}"
	path="${lane#*|}"

	if [ ! -f "$ROOT/$path" ]; then
		echo "::warning::No changelog at $path; omitting $name from the release notes."
		continue
	fi

	section="$(slice_section "$VERSION" "$ROOT/$path")"

	# A production release whose changelog was never updated is worth surfacing, but
	# not worth failing a release that has already published its packages.
	if [ -z "$section" ]; then
		echo "::warning::$path has no '## [$VERSION]' section; omitting $name from the release notes."
		continue
	fi

	{
		echo "## $name"
		echo
		printf '%s\n' "$section"
		if [ -n "${TAG:-}" ]; then
			echo
			echo "[Full $name changelog](https://github.com/$REPO/blob/$TAG/$path)"
		fi
		echo
	} >> "$OUT"

	found_any=true
done

if [ "$found_any" != "true" ]; then
	echo "::warning::No changelog section for $VERSION in any selected lane."
	printf 'No changelog entries were recorded for %s.\n' "$VERSION" > "$OUT"
fi

echo "--- $OUT ---"
cat "$OUT"
