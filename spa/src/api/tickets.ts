import axios from 'axios';

const API_BASE = 'http://localhost:8080/api/v1';

export async function buyTicket(flightNumber: string, price: number, paidFromBalance: boolean) {
    const token = localStorage.getItem('access_token');
    const res = await axios.post(`${API_BASE}/tickets`, {
        flightNumber,
        price,
        paidFromBalance
    }, {
        headers: { Authorization: `Bearer ${token}` }
    });
    return res.data;
}

export async function getUserTickets() {
    const token = localStorage.getItem('access_token');
    const res = await axios.get(`${API_BASE}/tickets`, {
        headers: { Authorization: `Bearer ${token}` }
    });
    return res.data; // массив TicketResponse
}

export async function returnTicket(ticketUid: string) {
    const token = localStorage.getItem('access_token');
    const res = await axios.delete(`${API_BASE}/tickets/${ticketUid}`, {
        headers: { Authorization: `Bearer ${token}` }
    });
    return res.status;
}