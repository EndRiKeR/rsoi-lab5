import axios from 'axios';

const API_BASE = 'http://localhost:8080/api/v1';

export async function getPrivilege() {
    const token = localStorage.getItem('access_token');
    const res = await axios.get(`${API_BASE}/privilege`, {
        headers: { Authorization: `Bearer ${token}` }
    });
    return res.data; // { balance, status, history }
}