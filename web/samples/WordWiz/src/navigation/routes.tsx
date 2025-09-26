import type { ComponentType, JSX } from 'react';

import { IndexPage } from '@app/pages/IndexPage.tsx';
import { GamePage } from '@app/pages/GamePage.tsx';

interface Route {
  path: string;
  Component: ComponentType;
  title?: string;
  icon?: JSX.Element;
}

export const routes: Route[] = [
  { path: '/', Component: IndexPage },
  { path: '/game', Component: GamePage },
];
