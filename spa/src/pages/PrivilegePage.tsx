import { useEffect, useState } from 'react';
import { getPrivilege } from '../api/privilege';
import { useAuth } from '../context/AuthContext';

const PrivilegePage = () => {
    const { token } = useAuth();
    const [data, setData] = useState<any>(null);

    useEffect(() => {
        if (!token) return;
        (async () => {
            const res = await getPrivilege();
            setData(res);
        })();
    }, [token]);

    if (!token) return <div>Необходимо войти</div>;
    if (!data) return <div>Загрузка...</div>;

    return (
        <div style={{ maxWidth: 600, margin: '0 auto', padding: 20 }}>
            <h1>Бонусный счёт</h1>
            <p>Баланс: {data.balance}</p>
            <p>Статус: {data.status}</p>
            <h2>История операций</h2>
            {data.history?.length === 0 ? <p>Нет операций</p> : (
                <ul>
                    {data.history.map((h: any, idx: number) => (
                        <li key={idx}>
                            {new Date(h.date).toLocaleString()} — {h.operationType} ({h.balanceDiff}) по билету {h.ticketUid}
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
};

export default PrivilegePage;