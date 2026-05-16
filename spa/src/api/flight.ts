import axios from 'axios';

const API_BASE = 'http://localhost:8080/api/v1';

export async function getFlights(page = 1, size = 10) {
    const token = localStorage.getItem('access_token');
    const res = await axios.get(`${API_BASE}/flights`, {
        params: { page, size },
        headers: { Authorization: `Bearer ${token}` }
    });
    return res.data; // { page, pageSize, totalElements, items }
}

export async function getFlightByNumber(flightNumber: string) {
    const token = localStorage.getItem('access_token');
    const res = await axios.get(`${API_BASE}/flights/${flightNumber}`, {
        headers: { Authorization: `Bearer ${token}` }
    });
    return res.data;
}