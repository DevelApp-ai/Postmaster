import React from 'react';
import ReactDOM from 'react-dom/client';
import { PostmasterClient } from '../PostmasterClient';
import './css/main.css';

// Example of how to use the PostmasterClient component
const App = () => {
  return (
    <div className="app-container">
      <header>
        <h1>Postmaster Messaging</h1>
      </header>
      
      <main>
        <PostmasterClient 
          config={{
            hubUrl: '/hub',
            tokenProvider: () => localStorage.getItem('auth_token') || '',
            onError: (error) => console.error('Postmaster error:', error),
            onConnected: () => console.log('Connected to Postmaster hub'),
            onDisconnected: () => console.log('Disconnected from Postmaster hub'),
            onMessageReceived: (message) => console.log('New message received:', message)
          }}
        />
      </main>
      
      <footer>
        <p>Powered by Postmaster - Runtime Extendable Messaging</p>
      </footer>
    </div>
  );
};

// Render the app
const root = ReactDOM.createRoot(document.getElementById('root') as HTMLElement);
root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);
