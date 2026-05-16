import React, { createContext, useState, useContext, useEffect, useCallback } from 'react';

interface AuthContextType {
    token: string | null;
    roles: string[];
    setToken: (token: string | null) => void;
    login: () => void;
    logout: () => void;
    hasRole: (role: string) => boolean;
}

const AuthContext = createContext<AuthContextType>({
    token: null,
    roles: [],
    setToken: () => {},
    login: () => {},
    logout: () => {},
    hasRole: () => false,
});

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
    const [token, setToken] = useState<string | null>(() => localStorage.getItem('access_token'));
    const [roles, setRoles] = useState<string[]>([]);

    useEffect(() => {
        if (token) {
            try {
                const payload = JSON.parse(atob(token.split('.')[1]));
                // Извлекаем роль: ClaimTypes.Role или короткий "role"
                let extractedRoles: string[] = [];
                const roleClaim = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
                if (roleClaim) {
                    extractedRoles = Array.isArray(roleClaim) ? roleClaim : [roleClaim];
                } else if (payload.role) {
                    extractedRoles = Array.isArray(payload.role) ? payload.role : [payload.role];
                }
                setRoles(extractedRoles);
                localStorage.setItem('access_token', token);
            } catch {
                setRoles([]);
            }
        } else {
            setRoles([]);
            localStorage.removeItem('access_token');
        }
    }, [token]);

    const login = useCallback(() => {
        window.location.href = 'http://localhost:8080/api/v1/authorize';
    }, []);

    const logout = useCallback(() => {
        setToken(null);
    }, []);

    const hasRole = useCallback((role: string) => roles.includes(role), [roles]);

    return (
        <AuthContext.Provider value={{ token, roles, setToken, login, logout, hasRole }}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => useContext(AuthContext);