import { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
import { getUserTickets } from '../api/tickets';
import { getPrivilege } from '../api/privilege';

const HomePage = () => {
    const { token, logout } = useAuth();
    const navigate = useNavigate();
    const [tickets, setTickets] = useState<any[]>([]);
    const [privilege, setPrivilege] = useState<any>(null);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        if (!token) return;
        const load = async () => {
            try {
                const [ticketsData, privilegeData] = await Promise.all([
                    getUserTickets(),
                    getPrivilege()
                ]);
                setTickets(ticketsData || []);
                setPrivilege(privilegeData);
            } catch (err) {
                console.error(err);
            } finally {
                setLoading(false);
            }
        };
        load();
    }, [token]);

    if (!token) {
        return (
            <div style={{ textAlign: 'center', marginTop: 50 }}>
                <h1>Добро пожаловать</h1>
                <button onClick={() => navigate('/login')}>Войти</button>
            </div>
        );
    }

    if (loading) return <div>Загрузка...</div>;

    return (
        <div style={{ maxWidth: 800, margin: '0 auto', padding: 20 }}>
            <h1>Личный кабинет</h1>
            <div style={{ marginBottom: 20 }}>
                <h2>Бонусный счёт</h2>
                {privilege ? (
                    <p>Баланс: {privilege.balance} | Статус: {privilege.status}</p>
                ) : (
                    <p>Нет данных</p>
                )}
            </div>
            <div>
                <h2>Мои билеты ({tickets.length})</h2>
                {tickets.length === 0 ? (
                    <p>Нет активных билетов</p>
                ) : (
                    <ul>
                        {tickets.map((t: any) => (
                            <li key={t.ticketUid}>
                                {t.flightNumber} — {t.fromAirport} → {t.toAirport} ({t.status})
                            </li>
                        ))}
                    </ul>
                )}
            </div>
            <button onClick={() => { logout(); navigate('/login'); }}>Выйти</button>
        </div>
    );
};

export default HomePage;