namespace LifeDashboard.Domain.Enums;

public enum AccountType
{
    //帳戶類型
    // 日常現金流 (參與生存預算計算，例如：錢包、主要薪轉戶)
    Cashflow = 1,
    // 儲備/預備金 (隔離資金，不參與日常預算，例如：緊急預備金帳戶)
    Saving = 2,
    // 長期投資 (隔離資金，例如：股票證券戶、定存)
    Investment = 3
}