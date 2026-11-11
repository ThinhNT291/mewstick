namespace Mewstick.Models;

/// <summary>Bốn góc phần tư của ma trận Eisenhower, dùng để sắp xếp mức độ ưu tiên task.</summary>
public enum EisenhowerQuadrant
{
    /// <summary>Không gõ mã ưu tiên — xếp cuối danh sách.</summary>
    None = 0,

    /// <summary>-gq: Gấp &amp; Quan trọng — làm ngay.</summary>
    UrgentImportant = 1,

    /// <summary>-kq: Không gấp &amp; Quan trọng — lên lịch làm.</summary>
    NotUrgentImportant = 2,

    /// <summary>-gk: Gấp &amp; Không quan trọng — có thể giao bớt.</summary>
    UrgentNotImportant = 3,

    /// <summary>-kk: Không gấp &amp; Không quan trọng — cân nhắc bỏ.</summary>
    NotUrgentNotImportant = 4
}