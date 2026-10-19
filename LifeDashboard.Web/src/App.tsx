import React from 'react';
import { SystemSettingPage } from './presentation/pages/SystemSettingPage';

function App() {
    return (
        <div>
        {/* 未來這裡可以加上 Navbar 或 Router，現在先單純渲染設定頁 */ }
        < SystemSettingPage />
        </div>
    );
}

export default App;