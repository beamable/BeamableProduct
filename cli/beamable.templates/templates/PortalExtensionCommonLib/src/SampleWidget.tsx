// A shared React component rendered through the Portal's design system.
//
// @beamable/portal-toolkit is a peerDependency (never a dependency), so the
// consuming extension provides the single shared toolkit/React instance — this
// library never bundles its own copy.

import { BeamCard } from '@beamable/portal-toolkit/react';

export function SampleWidget() {
  return <BeamCard>Common Library!</BeamCard>;
}
