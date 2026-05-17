import axios from 'axios';

const API_BASE = 'http://localhost:8080/api/v1';

export async function getStatistics(from?: string, to?: string) {
    const token = localStorage.getItem('access_token');
    const res = await axios.get(`${API_BASE}/statistics`, {
        headers: { Authorization: `Bearer ${token}` },
        params: { from, to }
    });
    return res.data;
}