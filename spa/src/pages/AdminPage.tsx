import { useEffect, useState } from 'react';
import { getStatistics } from '../api/statistics';
import { useAuth } from '../context/AuthContext';

const AdminPage = () => {
    const { token } = useAuth();
    const [events, setEvents] = useState<any[]>([]);
    const [from, setFrom] = useState('');
    const [to, setTo] = useState('');

    useEffect(() => {
        if (!token) return;
        loadEvents();
    }, [token]);

    const loadEvents = async () => {
        try {
            const data = await getStatistics(from || undefined, to || undefined);
            setEvents(data);
        } catch (err) {
            console.error(err);
        }
    };

    if (!token) return <div>Доступ запрещён</div>;

    return (
        <div style={{ maxWidth: 900, margin: '0 auto', padding: 20 }}>
            <h1>Статистика (админ)</h1>
            <div style={{ marginBottom: 10 }}>
                <input type="datetime-local" value={from} onChange={e => setFrom(e.target.value)} placeholder="С" />
                <input type="datetime-local" value={to} onChange={e => setTo(e.target.value)} placeholder="По" />
                <button onClick={loadEvents}>Показать</button>
            </div>
            {events.length === 0 ? <p>Нет событий</p> : (
                <ul>
                    {events.map((e: any) => (
                        <li key={e.id}>{e.timestamp} — {e.eventType} — {e.username} — {e.payload}</li>
                    ))}
                </ul>
            )}
        </div>
    );
};

export default AdminPage;