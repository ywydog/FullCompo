using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FullCompo.Core.Abstractions;
using FullCompo.Core.Models;
using FullCompo.Shared.Models;

namespace FullCompo.Widgets.Builtin;

public abstract class WidgetBase : IWidget
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual IImage? Icon => null;
    public abstract IEnumerable<WidgetSize> SupportedSizes { get; }

    public abstract Control CreateView(WidgetContext context);

    public virtual Control? CreateSettingsView(WidgetSettings settings) => null;

    public virtual WidgetSettings CreateDefaultSettings() => new();

    public virtual void OnActivated(WidgetContext context)
    {
    }

    public virtual void OnDeactivated()
    {
    }

    /// <summary>
    /// When true, the WidgetContainer will not render its default theme background,
    /// allowing the widget to provide its own full background (e.g. Microsoft-style cards).
    /// </summary>
    public virtual bool HasCustomBackground => false;

    protected static IBrush GetThemeBrush(string resourceKey)
    {
        if (Application.Current?.TryGetResource(resourceKey, out var resource) == true && resource is IBrush brush)
        {
            return brush;
        }
        return Brushes.Transparent;
    }
}
