import { Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/Layout';
import LoginPage from './pages/LoginPage';
import CallbackPage from './pages/CallbackPage';
import HomePage from './pages/HomePage';
import FlightsPage from './pages/FlightsPage';
import RegisterPage from './pages/RegisterPage';
import TicketsPage from './pages/TicketsPage';
import PrivilegePage from './pages/PrivilegePage';
import AdminPage from './pages/AdminPage';
import ProtectedRoute from './components/ProtectedRoute';

function App() {
    return (
        <Routes>
            <Route element={<Layout />}>
                <Route path="/login" element={<LoginPage />} />
                <Route path="/callback" element={<CallbackPage />} />
                <Route path="/" element={<ProtectedRoute><HomePage /></ProtectedRoute>} />
                <Route path="/flights" element={<ProtectedRoute><FlightsPage /></ProtectedRoute>} />
                <Route path="/tickets" element={<ProtectedRoute><TicketsPage /></ProtectedRoute>} />
                <Route path="/privilege" element={<ProtectedRoute><PrivilegePage /></ProtectedRoute>} />
                <Route path="/admin" element={<ProtectedRoute adminOnly><AdminPage /></ProtectedRoute>} />
                <Route path="/register" element={<RegisterPage />} />
                <Route path="*" element={<Navigate to="/" replace />} />
            </Route>
        </Routes>
    );
}

export default App;