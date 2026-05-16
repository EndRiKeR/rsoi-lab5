import { useAuth } from '../context/AuthContext';
import { Navigate } from 'react-router-dom';
import type {JSX} from "react";

interface Props {
    children: JSX.Element;
    adminOnly?: boolean;
}

const ProtectedRoute = ({ children, adminOnly }: Props) => {
    const { token, hasRole } = useAuth();

    if (!token) {
        return <Navigate to="/login" replace />;
    }

    if (adminOnly && !hasRole('Admin')) {
        return <Navigate to="/" replace />;
    }

    return children;
};

export default ProtectedRoute;