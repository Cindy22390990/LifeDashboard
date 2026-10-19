import http from './http';
import type { ISystemSetting, IApiResponse } from '@/domain/models/ISystemSetting';

// 系統設定 Repository
// 負責封裝所有對後端 SettingController 的 API 請求
// 為什麼這樣設計：隔離外部 API。前端元件不需要知道路徑是 `/Setting` 還是 `/SystemSetting`，也不需要知道是用 axios 還是 fetch。
// 如果要修改功能應該修改哪裡：如果後端 API 路徑改變了，或者新增了特定的查詢功能，請在這裡修改或新增方法。

export const SettingRepository = {
    // 取得目前設定 (GET)
    getSetting: async (): Promise<ISystemSetting> => {
        // 由於我們在 http.ts 攔截器已經 return response.data，所以這裡直接轉型
        return await http.get<unknown, ISystemSetting>('/Setting');
    },

    // 初始化設定 (POST)
    initializeSetting: async (data: ISystemSetting): Promise<IApiResponse> => {
        return await http.post<unknown, IApiResponse>('/Setting', data);
    },

    // 更新設定 (PUT)
    updateSetting: async (data: ISystemSetting): Promise<IApiResponse> => {
        return await http.put<unknown, IApiResponse>('/Setting', data);
    }
};