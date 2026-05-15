import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';

const HomePage = () => {
    const { token, logout } = useAuth();
    const navigate = useNavigate();

    if (!token) {
        return (
            <div style={{ textAlign: 'center', marginTop: '50px' }}>
                <h1>Главная страница</h1>
                <p>Вы не авторизованы.</p>
                <button onClick={() => navigate('/login')} style={{ padding: '10px 20px', fontSize: '16px' }}>
                    Войти
                </button>
            </div>
        );
    }

    return (
        <div style={{ textAlign: 'center', marginTop: '50px' }}>
            <h1>Главная страница</h1>
            <p>Вы успешно авторизованы!</p>
            <button onClick={logout} style={{ padding: '10px 20px', fontSize: '16px' }}>
                Выйти
            </button>
        </div>
    );
};

export default HomePage;