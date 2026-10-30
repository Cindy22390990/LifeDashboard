import axios, {
    type AxiosInstance,
    type AxiosRequestConfig
} from 'axios';


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


const axiosInstance = axios.create({
    baseURL: '/api',
    timeout: 10000,
    headers: {
        'Content-Type': 'application/json',
    },
});


axiosInstance.interceptors.request.use(
    (config) => {

        const token = localStorage.getItem("token");

        if (token) {

            config.headers.Authorization =
                `Bearer ${token}`;

        }

        return config;

    }
);


axiosInstance.interceptors.response.use(
    (response) => {

        return response.data;

    },
    (error) => {

        if (error.response) {

            console.error(
                'API Error:',
                error.response.data.message ||
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