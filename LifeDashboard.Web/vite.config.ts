import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';
import path from 'path';

// Vite 專案設定檔
// 負責編譯設定、路徑別名以及 PWA (漸進式網頁應用程式) 配置
// 如果要修改功能應該修改哪裡：未來如果要換 App 的 Icon、名稱或是主題顏色，請在這裡的 manifest 中修改。
export default defineConfig({
    plugins: [
        react(),
        VitePWA({
            registerType: 'autoUpdate', // 自動更新 Service Worker，確保使用者總是拿到最新版
            includeAssets: ['favicon.ico', 'apple-touch-icon.png', 'masked-icon.svg'],
            manifest: {
                name: 'LifeDashboard 記帳系統',
                short_name: 'LifeDash',
                description: '基於 Clean Architecture 的個人記帳與目標管理 APP',
                theme_color: '#ffffff',
                background_color: '#ffffff',
                display: 'standalone', // 隱藏瀏覽器網址列，呈現原生 APP 體驗
                icons: [
                    {
                        src: 'pwa-192x192.png',
                        sizes: '192x192',
                        type: 'image/png'
                    },
                    {
                        src: 'pwa-512x512.png',
                        sizes: '512x512',
                        type: 'image/png'
                    }
                ]
            }
        })
    ],
    resolve: {
        alias: {
            // 設定路徑別名，之後 import 就可以用 '@/domain/...' 代替 '../../domain/...'
            '@': path.resolve(__dirname, './src'),
        },
    },
    server: {
        port: 3000, // 前端固定跑在 3000 port
    }
});