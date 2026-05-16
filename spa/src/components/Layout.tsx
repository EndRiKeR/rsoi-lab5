import { Link, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const Layout = () => {
    const { token, hasRole, logout } = useAuth();
    const navigate = useNavigate();

    const handleLogout = () => {
        logout();
        navigate('/login');
    };

    if (!token) {
        return <Outlet />;
    }

    return (
        <div>
            <nav style={{ padding: '10px', background: '#f0f0f0', marginBottom: '20px' }}>
                <Link to="/" style={{ marginRight: 10 }}>Главная</Link>
                <Link to="/flights" style={{ marginRight: 10 }}>Рейсы</Link>
                <Link to="/tickets" style={{ marginRight: 10 }}>Билеты</Link>
                <Link to="/privilege" style={{ marginRight: 10 }}>Бонусы</Link>
                {hasRole('Admin') && (
                    <Link to="/admin" style={{ marginRight: 10, color: 'red', fontWeight: 'bold' }}>Админ-панель</Link>
                )}
                <button onClick={handleLogout} style={{ float: 'right' }}>Выйти</button>
            </nav>
            <div style={{ padding: '0 20px' }}>
                <Outlet />
            </div>
        </div>
    );
};

export default Layout;