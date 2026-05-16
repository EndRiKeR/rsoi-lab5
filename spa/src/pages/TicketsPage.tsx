import { useEffect, useState } from 'react';
import { getUserTickets, returnTicket } from '../api/tickets';
import { useAuth } from '../context/AuthContext';

const TicketsPage = () => {
    const { token } = useAuth();
    const [tickets, setTickets] = useState<any[]>([]);

    useEffect(() => {
        if (!token) return;
        (async () => {
            const data = await getUserTickets();
            setTickets(data || []);
        })();
    }, [token]);

    const handleReturn = async (ticketUid: string) => {
        try {
            await returnTicket(ticketUid);
            alert('Билет возвращен');
            setTickets(prev => prev.filter(t => t.ticketUid !== ticketUid));
        } catch (err: any) {
            alert(`Ошибка: ${err.response?.data?.message || err.message}`);
        }
    };

    if (!token) return <div>Необходимо войти</div>;

    return (
        <div style={{ maxWidth: 800, margin: '0 auto', padding: 20 }}>
            <h1>Мои билеты</h1>
            {tickets.length === 0 ? <p>Нет купленных билетов</p> : (
                <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                    <thead>
                    <tr><th>UID</th><th>Рейс</th><th>Откуда</th><th>Куда</th><th>Цена</th><th>Статус</th><th></th></tr>
                    </thead>
                    <tbody>
                    {tickets.map((t: any) => (
                        <tr key={t.ticketUid}>
                            <td>{t.ticketUid}</td>
                            <td>{t.flightNumber}</td>
                            <td>{t.fromAirport}</td>
                            <td>{t.toAirport}</td>
                            <td>{t.price}</td>
                            <td>{t.status}</td>
                            <td>
                                {t.status === 'PAID' && (
                                    <button onClick={() => handleReturn(t.ticketUid)}>Вернуть</button>
                                )}
                            </td>
                        </tr>
                    ))}
                    </tbody>
                </table>
            )}
        </div>
    );
};

export default TicketsPage;