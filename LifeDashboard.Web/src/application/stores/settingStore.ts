import { create } from 'zustand';
import { ISystemSetting } from '@/domain/models/ISystemSetting';
import { SettingRepository } from '@/infrastructure/api/SettingRepository';

// 定義 Store 的狀態與可執行的 Action
interface SettingState {
    setting: ISystemSetting | null; // 儲存目前使用者的設定
    isLoading: boolean;             // 是否正在與後端通訊
    error: string | null;           // 錯誤訊息

    // Actions
    fetchSetting: () => Promise<void>;
    initializeSetting: (data: ISystemSetting) => Promise<void>;
    updateSetting: (data: ISystemSetting) => Promise<void>;
}

// 系統設定全域 Store (基於 Zustand)
// 負責協調 UI 與 Repository 之間的資料流
// 為什麼這樣設計：將「打 API 取得資料並存起來」的過程封裝。UI 只要呼叫 fetchSetting()，然後綁定 setting 變數即可，完全不用管非同步的複雜流程。
// 如果要修改功能應該修改哪裡：如果要修改預設值或是錯誤捕捉的預設訊息，請在此修改。
export const useSettingStore = create<SettingState>((set) => ({
    setting: null,
    isLoading: false,
    error: null,

    // 取得資料
    fetchSetting: async () => {
        set({ isLoading: true, error: null });
        try {
            const data = await SettingRepository.getSetting();
            set({ setting: data, isLoading: false });
        } catch (err: any) {
            set({ error: err.response?.data?.message || '取得設定失敗', isLoading: false });
        }
    },

    // 初始化資料
    initializeSetting: async (data: ISystemSetting) => {
        set({ isLoading: true, error: null });
        try {
            await SettingRepository.initializeSetting(data);
            set({ setting: data, isLoading: false }); // 成功後直接更新本地狀態
        } catch (err: any) {
            set({ error: err.response?.data?.message || '初始化設定失敗', isLoading: false });
            throw err; // 將錯誤往上拋，讓 UI 可以顯示 Toast 或警告框
        }
    },

    // 更新資料
    updateSetting: async (data: ISystemSetting) => {
        set({ isLoading: true, error: null });
        try {
            await SettingRepository.updateSetting(data);
            set({ setting: data, isLoading: false }); // 成功後同步更新本地狀態
        } catch (err: any) {
            set({ error: err.response?.data?.message || '更新設定失敗', isLoading: false });
            throw err; // 拋給 UI 處理
        }
    }
}));