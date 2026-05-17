import { useEffect, useState } from 'react';
import { getFlights } from '../api/flight';
import { buyTicket } from '../api/tickets';
import { useAuth } from '../context/AuthContext';

const FlightsPage = () => {
    const { token } = useAuth();
    const [flights, setFlights] = useState<any[]>([]);
    const [page, setPage] = useState(1);
    const [total, setTotal] = useState(0);
    const size = 10;

    useEffect(() => {
        if (!token) return;
        (async () => {
            const data = await getFlights(page, size);
            setFlights(data.items || []);
            setTotal(data.totalElements || 0);
        })();
    }, [page, token]);

    const handleBuy = async (flightNumber: string, price: number, paidFromBalance: boolean, availableSeats: number) => {
        if (availableSeats <= 0) {
            alert('На этот рейс больше нет билетов.');
            return;
        }
        try {
            const result = await buyTicket(flightNumber, price, paidFromBalance);
            alert(`Билет куплен! UID: ${result.ticketUid}`);
            // Обновить список рейсов, чтобы отразить уменьшение мест
            const data = await getFlights(page, size);
            setFlights(data.items || []);
            setTotal(data.totalElements || 0);
        } catch (err: any) {
            const message = err.response?.data?.message || err.response?.status === 409
                ? 'Билетов больше нет.'
                : err.message;
            alert(`Ошибка: ${message}`);
        }
    };

    const totalPages = Math.ceil(total / size);

    return (
        <div style={{ maxWidth: 900, margin: '0 auto', padding: 20 }}>
            <h1>Доступные рейсы</h1>
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                <thead>
                <tr>
                    <th>Номер</th>
                    <th>Откуда</th>
                    <th>Куда</th>
                    <th>Дата</th>
                    <th>Цена</th>
                    <th>Осталось мест</th>
                    <th></th>
                </tr>
                </thead>
                <tbody>
                {flights.map((f: any) => (
                    <tr key={f.flightNumber}>
                        <td>{f.flightNumber}</td>
                        <td>{f.fromAirport}</td>
                        <td>{f.toAirport}</td>
                        <td>{new Date(f.date).toLocaleString()}</td>
                        <td>{f.price} руб.</td>
                        <td>{f.availableSeats}</td>
                        <td>
                            <button
                                onClick={() => handleBuy(f.flightNumber, f.price, false, f.availableSeats)}
                                disabled={f.availableSeats <= 0}
                                style={{ opacity: f.availableSeats <= 0 ? 0.5 : 1 }}
                            >
                                Купить
                            </button>
                        </td>
                    </tr>
                ))}
                </tbody>
            </table>
            <div>
                <button disabled={page <= 1} onClick={() => setPage(p => p - 1)}>Назад</button>
                <span> Страница {page} из {totalPages} </span>
                <button disabled={page >= totalPages} onClick={() => setPage(p => p + 1)}>Вперед</button>
            </div>
        </div>
    );
};

export default FlightsPage;