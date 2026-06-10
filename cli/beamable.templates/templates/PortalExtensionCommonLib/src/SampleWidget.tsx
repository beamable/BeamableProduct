// A shared React component. Because react/react-dom are declared as
// peerDependencies (never dependencies), the consuming extension's build
// externalizes them to the Portal host's React instance — so hooks used here
// share the host's React and do not trigger "Invalid hook call".

import { useState } from 'react';
import { greet } from './sample';

interface SampleWidgetProps {
  name: string;
}

export function SampleWidget({ name }: SampleWidgetProps) {
  const [count, setCount] = useState(0);

  return (
    <div style={{ padding: 12 }}>
      <p>{greet(name)}</p>
      <button onClick={() => setCount((c) => c + 1)}>Clicked {count} times</button>
    </div>
  );
}
