import { Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const LoginPage = () => {
    const { login } = useAuth();

    return (
        <div style={{ textAlign: 'center', marginTop: '50px' }}>
            <h1>Вход в систему</h1>
            <button onClick={login} style={{ padding: '10px 20px', fontSize: '16px' }}>
                Войти через Identity Provider
            </button>
            <p style={{ marginTop: '15px' }}>
                Нет аккаунта? <Link to="/register">Зарегистрироваться</Link>
            </p>
        </div>
    );
};

export default LoginPage;