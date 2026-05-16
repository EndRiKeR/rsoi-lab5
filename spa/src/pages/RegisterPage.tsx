import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import axios from 'axios';

const RegisterPage = () => {
    const navigate = useNavigate();
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [email, setEmail] = useState('');
    const [error, setError] = useState('');

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');
        try {
            await axios.post('http://localhost:8080/api/v1/register', {
                username,
                password,
                email
            });
            // После успешной регистрации переходим на страницу логина
            navigate('/login');
        } catch (err: any) {
            setError(err.response?.data?.message || 'Ошибка регистрации');
        }
    };

    return (
        <div style={{ maxWidth: 400, margin: '50px auto', padding: 20, border: '1px solid #ccc', borderRadius: 8 }}>
            <h1>Регистрация</h1>
            {error && <p style={{ color: 'red' }}>{error}</p>}
            <form onSubmit={handleSubmit}>
                <input
                    type="text"
                    placeholder="Логин"
                    value={username}
                    onChange={e => setUsername(e.target.value)}
                    required
                    style={{ width: '100%', padding: 10, marginBottom: 10 }}
                />
                <input
                    type="email"
                    placeholder="Email"
                    value={email}
                    onChange={e => setEmail(e.target.value)}
                    required
                    style={{ width: '100%', padding: 10, marginBottom: 10 }}
                />
                <input
                    type="password"
                    placeholder="Пароль"
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                    required
                    style={{ width: '100%', padding: 10, marginBottom: 10 }}
                />
                <button type="submit" style={{ width: '100%', padding: 10, background: '#28a745', color: 'white', border: 'none', borderRadius: 4 }}>
                    Зарегистрироваться
                </button>
            </form>
            <p style={{ marginTop: 10 }}>
                Уже есть аккаунт? <Link to="/login">Войти</Link>
            </p>
        </div>
    );
};

export default RegisterPage;