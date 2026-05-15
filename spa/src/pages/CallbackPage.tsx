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
            setToken(token);
            navigate('/', { replace: true });
        } else {
            navigate('/login', { replace: true });
        }
    }, [searchParams, setToken, navigate]);

    return <div>Загрузка...</div>;
};

export default CallbackPage;