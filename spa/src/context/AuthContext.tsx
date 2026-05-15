import React, { createContext, useState, useContext, useEffect } from 'react';

interface AuthContextType {
    token: string | null;
    setToken: (token: string | null) => void;
    login: () => void;
    logout: () => void;
}

const AuthContext = createContext<AuthContextType>({
    token: null,
    setToken: () => {},
    login: () => {},
    logout: () => {},
});

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
    const [token, setToken] = useState<string | null>(localStorage.getItem('access_token'));

    useEffect(() => {
        if (token) {
            localStorage.setItem('access_token', token);
        } else {
            localStorage.removeItem('access_token');
        }
    }, [token]);

    const login = () => {
        window.location.href = 'http://localhost:8080/api/v1/authorize';
    };

    const logout = () => {
        setToken(null);
    };

    return (
        <AuthContext.Provider value={{ token, setToken, login, logout }}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => useContext(AuthContext);