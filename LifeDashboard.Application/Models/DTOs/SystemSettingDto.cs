namespace LifeDashboard.Application.Models.DTOs
{
    // 系統設定 DTO (Data Transfer Object)
    // 負責在前端 (API) 與後端 (Application 層) 之間傳遞資料
    // 為什麼這樣設計：隱藏系統內部的 Entity 細節 (例如不把 PasswordHash 傳給前端)，並且讓接收進來的資料結構扁平化，方便前端串接。
    // 如果要修改功能應該修改哪裡：如果設定頁面新增了欄位（例如：UI 主題顏色），請在這裡加上對應的屬性，讓前端可以傳遞。
    public class SystemSettingDto
    {
        public string UserName { get; set; } = string.Empty;

        public int Payday { get; set; }

        // 分配比例
        public int Living { get; set; }
        public int Savings { get; set; }
        public int Investment { get; set; }
        public int Entertainment { get; set; }
    }
}