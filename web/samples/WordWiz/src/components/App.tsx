import { Navigate, Route, Routes, HashRouter } from 'react-router-dom';
import { routes } from '@app/navigation/routes.tsx';

export function App() {
  return (
    <HashRouter>
      <Routes>
        {routes.map((route) => (
          <Route key={route.path} {...route} />
        ))}
        <Route path="*" element={<Navigate to="/" />} />
      </Routes>
    </HashRouter>
  );
}
