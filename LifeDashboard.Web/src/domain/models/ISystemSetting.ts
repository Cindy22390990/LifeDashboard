// 系統設定型別定義 (Domain Model)
// 為什麼這樣設計：前端必須有自己獨立的型別定義，不依賴任何套件，這是前端領域的最核心。
// 如果要修改功能應該修改哪裡：如果後端的 SystemSettingDto 新增了欄位（例如 themeColor），請記得同步在這裡加上。

export interface ISystemSetting {
    userName: string;
    payday: number;
    living: number;
    savings: number;
    investment: number;
    entertainment: number;
}

// 通用的 API 回應型別
export interface IApiResponse {
    message: string;
    detail?: string;
}