using FullCompo.Shared.Enums;
using FullCompo.Shared.Models;

namespace FullCompo.Core.Models;

/// <summary>
/// 集中管理所有组件尺寸预设。
/// 想修改组件大小，直接改这里的 Width / Height 数值即可。
/// </summary>
public static class WidgetSizePresets
{
    /// <summary>短条：两个短条堆叠在左侧</summary>
    public static readonly WidgetSize ShortBar = new()
    {
        Id = "short-bar",
        Name = "短条",
        Type = WidgetSizeType.Small,
        Columns = 2,
        Rows = 1,
        Width = 160,
        Height = 80
    };

    /// <summary>方形：右侧较大的正方形组件</summary>
    public static readonly WidgetSize Square = new()
    {
        Id = "square",
        Name = "方形",
        Type = WidgetSizeType.Medium,
        Columns = 2,
        Rows = 2,
        Width = 160,
        Height = 160
    };

    /// <summary>大方形：更大的正方形组件（预留）</summary>
    public static readonly WidgetSize LargeSquare = new()
    {
        Id = "large-square",
        Name = "大方形",
        Type = WidgetSizeType.Large,
        Columns = 3,
        Rows = 3,
        Width = 240,
        Height = 240
    };

    public static IEnumerable<WidgetSize> All => new[] { ShortBar, Square, LargeSquare };
}
