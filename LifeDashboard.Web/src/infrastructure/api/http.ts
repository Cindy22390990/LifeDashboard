import axios from 'axios';

// 基礎 HTTP 客戶端 (Axios 實體)
// 負責統管所有對外的 API 請求設定與攔截
// 為什麼這樣設計：把 Request/Response 的共用邏輯集中處理。Component 不應該知道 Axios 的存在，只會呼叫 Repository。
// 如果要修改功能應該修改哪裡：未來如果後端加上 JWT 驗證，請在 request interceptor 裡面自動把 Token 塞進 Header。

const http = axios.create({
    // 對應你剛剛後端 Swagger 的 Port 號
    baseURL: '/api',
    timeout: 10000, // 10秒 Timeout 防呆
    headers: {
        'Content-Type': 'application/json',
    },
});

// Request 攔截器
http.interceptors.request.use(
    (config) => {
        // 未來這裡可以從 LocalStorage 或 Store 取出 Token 加入 Header
        // const token = localStorage.getItem('token');
        // if (token) {
        //   config.headers.Authorization = `Bearer ${token}`;
        // }
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

// Response 攔截器
http.interceptors.response.use(
    (response) => {
        // 如果後端回傳 2xx，直接把 data 拆出來往上拋
        return response.data;
    },
    (error) => {
        // 統一處理 API 錯誤
        // 為什麼這樣設計：在這裡處理 400 Bad Request 或 401 Unauthorized，確保全站錯誤提示格式一致。
        if (error.response) {
            console.error('API Error:', error.response.data.message || '發生未知的伺服器錯誤');
            // 如果是 401，可以直接在這裡觸發強制登出並轉址到 Login
        } else {
            console.error('網路連線異常，請檢查你的網路狀態');
        }
        return Promise.reject(error);
    }
);

export default http;