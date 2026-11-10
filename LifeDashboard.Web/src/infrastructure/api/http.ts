import axios, {
    type AxiosInstance,
    type AxiosRequestConfig
} from 'axios';
import { useAuthStore } from '../../application/stores/authStore';


interface HttpClient extends Omit<AxiosInstance, 'get' | 'post' | 'put' | 'delete'> {

    get<T = unknown>(
        url: string,
        config?: AxiosRequestConfig
    ): Promise<T>;

    post<T = unknown>(
        url: string,
        data?: unknown,
        config?: AxiosRequestConfig
    ): Promise<T>;

    put<T = unknown>(
        url: string,
        data?: unknown,
        config?: AxiosRequestConfig
    ): Promise<T>;

    delete<T = unknown>(
        url: string,
        config?: AxiosRequestConfig
    ): Promise<T>;
}

let redirectingToLogin = false;

const axiosInstance = axios.create({
    baseURL: '/api',
    timeout: 10000,
    headers: {
        'Content-Type': 'application/json',
    },
});


axiosInstance.interceptors.request.use(
    (config) => {

        const token = useAuthStore.getState().token;

        if (token) {
            config.headers.set('Authorization', `Bearer ${token}`);
        } else {
            config.headers.delete('Authorization');
        }

        return config;

    }
);


axiosInstance.interceptors.response.use(
    (response) => {

        return response.data;

    },
    (error) => {

        const requestUrl = error.config?.url ?? '';
        const isLoginRequest = requestUrl.toLowerCase().includes('/auth/login');

        if (error.response?.status === 401) {
            useAuthStore.getState().logout();

            if (
                !isLoginRequest &&
                window.location.pathname !== '/login' &&
                !redirectingToLogin
            ) {
                redirectingToLogin = true;
                window.location.assign('/login');
            }
        }

        if (error.response) {

            console.error(
                'API Error:',
                error.response.data?.message ||
                '未知錯誤'
            );

        }
        else {

            console.error(
                '網路連線異常'
            );

        }

        return Promise.reject(error);

    }
);


const http = axiosInstance as HttpClient;


export default http;