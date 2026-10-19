using System;

namespace LifeDashboard.Domain.ValueObjects
{
    // 資產分配比例 Value Object
    // 負責統一管理生活、儲蓄、投資、娛樂的比例分配
    // 為什麼這樣設計：將「比例加總必須是 100」的商業邏輯鎖死在核心，確保任何地方建立此比例時絕對合法。
    // 如果要修改功能應該修改哪裡：未來若要增加新的分配項目（例如：學習），請在此處新增屬性並修改建構子的 100% 驗證邏輯。
    public class AllocationRatio
    {
        public int Living { get; private set; }
        public int Savings { get; private set; }
        public int Investment { get; private set; }
        public int Entertainment { get; private set; }

        // 建構子 (Constructor)
        // 在建立物件的當下就進行驗證，保證產生出來的 AllocationRatio 一定是合法的
        public AllocationRatio(int living, int savings, int investment, int entertainment)
        {
            var total = living + savings + investment + entertainment;

            // 防呆驗證：加總必須剛好等於 100
            if (total != 100)
            {
                throw new ArgumentException($"資產分配比例總和必須為 100%。目前總和為：{total}%");
            }

            // 防呆驗證：比例不能為負數
            if (living < 0 || savings < 0 || investment < 0 || entertainment < 0)
            {
                throw new ArgumentException("資產分配比例不可包含負數。");
            }

            Living = living;
            Savings = savings;
            Investment = investment;
            Entertainment = entertainment;
        }

        // Value Object 必備：相等性比較
        // 為什麼這樣設計：值物件沒有 Id，只要內部所有的值都一樣，我們就視為同一個物件
        public override bool Equals(object? obj)
        {
            if (obj is AllocationRatio other)
            {
                return Living == other.Living &&
                       Savings == other.Savings &&
                       Investment == other.Investment &&
                       Entertainment == other.Entertainment;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Living, Savings, Investment, Entertainment);
        }
    }
}