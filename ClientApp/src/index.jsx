import { Suspense } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router';
import {App} from './App';
import './Utilities/i18next';
import './custom.css'
import {unregister} from "./registerServiceWorker";
import {Loading} from "./Components/Loading";

const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
const rootElement = document.getElementById('root');

createRoot(rootElement).render(
    <Suspense fallback={<Loading/>}>
      <BrowserRouter basename={baseUrl}>
        <App />
      </BrowserRouter>
    </Suspense>
);


unregister();
