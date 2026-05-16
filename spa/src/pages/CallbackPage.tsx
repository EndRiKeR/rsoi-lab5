import { useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const CallbackPage = () => {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();
    const { setToken } = useAuth();

    useEffect(() => {
        const token = searchParams.get('token');
        if (token) {
            console.log('CallbackPage: token получен', token.substring(0, 20) + '...');
            setToken(token);
            navigate('/', { replace: true });
        } else {
            console.log('CallbackPage: токен отсутствует, переадресация на логин');
            navigate('/login', { replace: true });
        }
    }, [searchParams, setToken, navigate]);

    return <div>Загрузка...</div>;
};

export default CallbackPage;