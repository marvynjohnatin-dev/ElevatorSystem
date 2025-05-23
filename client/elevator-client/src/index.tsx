import React from 'react';
import ReactDOM from 'react-dom/client';
import './index.css';
import Elevator from './pages/Elevator';
import './styles/elevatorSystem.css';

const root = ReactDOM.createRoot(
  document.getElementById('root') as HTMLElement
);
root.render(
  <React.StrictMode>
    <Elevator />
  </React.StrictMode>
);