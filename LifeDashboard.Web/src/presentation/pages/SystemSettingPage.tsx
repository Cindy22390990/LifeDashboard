import React, { useEffect, useState } from 'react';
import { useSettingStore } from '@/application/stores/settingStore';
import type { ISystemSetting } from '@/domain/models/ISystemSetting';

// 系統設定頁面 (Presentation Layer)
// 負責呈現 UI、即時表單驗證 (100% 比例檢查)，並呼叫 Store 進行資料更新。
// 為什麼這樣設計：UI 層極度輕量化，完全沒有 API 邏輯，所有複雜的網路通訊與錯誤捕捉都交給下層的 Store 與 Repository 處理。
export const SystemSettingPage: React.FC = () => {
    // 1. 從 Store 取出全域狀態與 Actions
    const { setting, isLoading, error, fetchSetting, updateSetting, initializeSetting } = useSettingStore();

    // 2. 建立頁面專屬的本地表單狀態 (用於 Input 雙向綁定)
    const [formData, setFormData] = useState<ISystemSetting>({
        userName: '',
        payday: 5,
        living: 50,
        savings: 20,
        investment: 15,
        entertainment: 15,
    });

    // 3. 進入頁面時，自動向後端拉取最新的設定
    useEffect(() => {
        fetchSetting();
    }, [fetchSetting]);

    // 4. 當 Store 的設定讀取成功時，同步更新到本地表單供使用者編輯
    useEffect(() => {
        if (setting) {
            setFormData(setting);
        }
    }, [setting]);

    // 處理 Input 變更
    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setFormData((prev) => ({
            ...prev,
            // 稱呼是字串，其他欄位都是數字，做自動轉型
            [name]: name === 'userName' ? value : Number(value),
        }));
    };

    // 5. 即時 UX 驗證邏輯
    const totalRatio = formData.living + formData.savings + formData.investment + formData.entertainment;
    const isRatioValid = totalRatio === 100;
    const isPaydayValid = formData.payday >= 1 && formData.payday <= 31;
    const isFormValid = isRatioValid && isPaydayValid && formData.userName.trim() !== '';

    // 6. 儲存按鈕行為
    const handleSave = async () => {
        if (!isFormValid) return;

        try {
            if (setting) {
                // 如果原本就有資料，執行更新 (PUT)
                await updateSetting(formData);
                alert('設定已成功更新！');
            } else {
                // 如果沒有資料 (例如第一次登入)，執行初始化 (POST)
                await initializeSetting(formData);
                alert('系統設定初始化成功！');
            }
        } catch  {
            // 不宣告變數，或是改寫為 catch (error) { console.error(error); }
            // 錯誤已由 Store 捕捉，這裡就不做額外處理
        }
    };

    // 單純的畫面渲染 (簡易的 Inline-style 排版，未來你可換成 Tailwind CSS)
    return (
        <div style= {{ maxWidth: '500px', margin: '40px auto', fontFamily: 'sans-serif' }
}>
    <h2>⚙️ 系統設定(System Settings) </h2>

{/* 錯誤訊息提示區塊 */ }
{ error && <div style={ { color: 'red', marginBottom: '10px' } }>⚠️ { error } </div> }

<div style={ { border: '1px solid #ccc', padding: '20px', borderRadius: '8px' } }>
    <h3>👤 個人化設定 </h3>
        <label>
稱呼：
<input type="text" name = "userName" value = { formData.userName } onChange = { handleChange } />
    </label>

    < h3 style = {{ marginTop: '20px' }}>📅 發薪設定 </h3>
        <label>
每月發薪日：
<input type="number" name = "payday" value = { formData.payday } onChange = { handleChange } min = "1" max = "31" />
    號
{ !isPaydayValid && <span style={ { color: 'red', fontSize: '12px' } }> (請輸入 1 - 31 之間的日期)</span> }
</label>

    < h3 style = {{ marginTop: '20px' }}>📊 資產分配比例(總和必須為 100 %) </h3>
        < div style = {{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
            <label>🏠 生活：<input type="number" name = "living" value = { formData.living } onChange = { handleChange } /> % </label>
                <label>💰 儲蓄：<input type="number" name = "savings" value = { formData.savings } onChange = { handleChange } /> % </label>
                    <label>📈 投資：<input type="number" name = "investment" value = { formData.investment } onChange = { handleChange } /> % </label>
                        <label>🎮 娛樂：<input type="number" name = "entertainment" value = { formData.entertainment } onChange = { handleChange } /> % </label>
                            </div>

                            < hr style = {{ margin: '20px 0' }} />

                                < div style = {{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                    <strong style={ { color: isRatioValid ? 'green' : 'red' } }>
                                        目前總計：{ totalRatio }% { isRatioValid? '✅': '❌' }
                                            </strong>

                                            < button
onClick = { handleSave }
disabled = {!isFormValid || isLoading}
style = {{ padding: '10px 20px', cursor: isFormValid ? 'pointer' : 'not-allowed' }}
          >
{ isLoading? '儲存中...': '儲存設定' }
    </button>
    </div>
    </div>
    </div>
  );
};