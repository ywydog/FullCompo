using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using FullCompo.Core.Abstractions;
using FullCompo.Core.Abstractions.Services;
using FullCompo.Core.Models;
using FullCompo.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FullCompo.App.Views;

public class WidgetContainer : Border
{
    private readonly IServiceProvider _services;
    private readonly IConfigService _configService;
    private readonly IThemeService _themeService;
    private readonly IEditHistoryService _editHistoryService;

    public WidgetInstanceConfig Config { get; }
    public IWidget Widget { get; private set; }
    public WidgetSize CurrentSize { get; private set; }

    private bool _isEditMode;
    private bool _isSelected;
    private bool _isDragging;
    private Point _dragStart;
    private Point _positionStart;

    private Grid _rootGrid = null!;
    private TextBlock _label = null!;
    private Button _deleteButton = null!;
    private Border _contentBorder = null!;

    public event EventHandler? SelectionChanged;
    public event EventHandler<bool>? DragStateChanged;
    public event EventHandler? RequestDelete;
    public event EventHandler? LongPressEditMode;

    private DispatcherTimer? _longPressTimer;
    private bool _longPressTriggered;

    private const double GridSize = 80.0;
    private static readonly TimeSpan LongPressDuration = TimeSpan.FromSeconds(3);

    public WidgetContainer(WidgetInstanceConfig config, IWidget widget, WidgetSize size, IServiceProvider services)
    {
        _services = services;
        _configService = services.GetRequiredService<IConfigService>();
        _themeService = services.GetRequiredService<IThemeService>();
        _editHistoryService = services.GetRequiredService<IEditHistoryService>();

        Config = config;
        Widget = widget;
        CurrentSize = size;

        BuildContainer();
        ApplySize();
        LoadWidget();

        _longPressTimer = new DispatcherTimer { Interval = LongPressDuration };
        _longPressTimer.Tick += OnLongPressTimerTick;

        _themeService.ThemeChanged += (_, _) => ApplyThemeColors();
    }

    private void OnLongPressTimerTick(object? sender, EventArgs e)
    {
        _longPressTimer?.Stop();
        if (_isEditMode || _longPressTriggered) return;

        _longPressTriggered = true;
        LongPressEditMode?.Invoke(this, EventArgs.Empty);
    }

    private void BuildContainer()
    {
        _rootGrid = new Grid();

        // Content area
        _contentBorder = new Border
        {
            Background = Widget.HasCustomBackground ? Brushes.Transparent : GetThemedBrush("ThemeBackgroundBrush"),
            BorderBrush = Widget.HasCustomBackground ? Brushes.Transparent : GetThemedBrush("ThemeBorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = CurrentSize.IsCircular ? new CornerRadius(CurrentSize.Width / 2) : new CornerRadius(Widget.HasCustomBackground ? 20 : 18),
            Padding = Widget.HasCustomBackground ? new Thickness(0) : new Thickness(8),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 4,
                Blur = 16,
                Spread = 0,
                Color = Color.Parse("#33000000")
            })
        };

        var contentPresenter = new ContentPresenter
        {
            Name = "WidgetContent",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
        };
        _contentBorder.Child = contentPresenter;

        // Label
        _label = new TextBlock
        {
            Text = Widget.Name,
            FontSize = 10,
            Foreground = GetThemedBrush("ThemeForegroundBrush"),
            Background = new SolidColorBrush(Color.Parse("#80000000")),
            Padding = new Thickness(4, 2),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Thickness(4, 4, 0, 0),
            IsVisible = false
        };

        // Delete button
        _deleteButton = new Button
        {
            Content = "×",
            Width = 20,
            Height = 20,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Thickness(0, -8, -8, 0),
            IsVisible = false
        };
        _deleteButton.Click += (_, _) => RequestDelete?.Invoke(this, EventArgs.Empty);

        _rootGrid.Children.Add(_contentBorder);
        _rootGrid.Children.Add(_label);
        _rootGrid.Children.Add(_deleteButton);

        Child = _rootGrid;

        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
    }

    private void ApplySize()
    {
        Width = CurrentSize.Width;
        Height = CurrentSize.Height;
    }

    private void LoadWidget()
    {
        var presenter = _contentBorder.Child as ContentPresenter;
        if (presenter == null) return;

        try
        {
            var loggerFactory = _services.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var context = new WidgetContext(
                Config.Id,
                Config.PanelId,
                CurrentSize,
                _services,
                loggerFactory.CreateLogger(Widget.GetType().FullName ?? Widget.Id),
                Config.Settings);

            Widget.OnActivated(context);
            presenter.Content = Widget.CreateView(context);
        }
        catch (Exception ex)
        {
            var logger = _services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<WidgetContainer>>();
            logger.LogError(ex, "Failed to load widget {WidgetId}", Widget.Id);
            presenter.Content = new TextBlock
            {
                Text = "加载失败",
                Foreground = GetThemedBrush("ThemeErrorBrush"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
        }
    }

    public void SetWidget(IWidget widget)
    {
        if (widget.Id == Widget.Id) return;

        _editHistoryService.RecordState();

        try
        {
            Widget.OnDeactivated();
        }
        catch (Exception ex)
        {
            var logger = _services.GetService<Microsoft.Extensions.Logging.ILogger<WidgetContainer>>();
            logger?.LogError(ex, "Failed to deactivate widget {WidgetId}", Widget.Id);
        }

        Widget = widget;
        Config.WidgetId = widget.Id;
        _label.Text = widget.Name;
        LoadWidget();
    }

    public void SetSize(WidgetSize size)
    {
        if (size.Id == CurrentSize.Id) return;

        _editHistoryService.RecordState();
        CurrentSize = size;
        Config.SizeId = size.Id;
        ApplySize();
        _contentBorder.CornerRadius = size.IsCircular ? new CornerRadius(size.Width / 2) : new CornerRadius(18);
        LoadWidget();
    }

    public void SetEditMode(bool isEditMode)
    {
        _isEditMode = isEditMode;
        _label.IsVisible = isEditMode;
        if (!isEditMode)
        {
            SetSelected(false);
        }
        else if (_isSelected)
        {
            _deleteButton.IsVisible = true;
        }
    }

    public void SetSelected(bool isSelected)
    {
        _isSelected = isSelected;
        if (isSelected && _isEditMode)
        {
            _contentBorder.BorderBrush = GetThemedBrush("ThemeAccentBrush");
            _contentBorder.BorderThickness = new Thickness(2);
            _deleteButton.IsVisible = true;
        }
        else
        {
            _contentBorder.BorderBrush = GetThemedBrush("ThemeBorderBrush");
            _contentBorder.BorderThickness = new Thickness(1);
            _deleteButton.IsVisible = false;
        }
    }

    public void SavePosition()
    {
        if (Parent is not Canvas canvas) return;
        Config.PosX = Canvas.GetLeft(this);
        Config.PosY = Canvas.GetTop(this);
    }

    private void ApplyThemeColors()
    {
        try
        {
            _contentBorder.Background = Widget.HasCustomBackground
                ? Brushes.Transparent
                : GetThemedBrush("ThemeBackgroundBrush");
            _contentBorder.BorderBrush = Widget.HasCustomBackground
                ? Brushes.Transparent
                : (_isSelected && _isEditMode
                    ? GetThemedBrush("ThemeAccentBrush")
                    : GetThemedBrush("ThemeBorderBrush"));
            _label.Foreground = GetThemedBrush("ThemeForegroundBrush");

            // Ask the widget to recreate its view so it picks up the new theme brushes
            LoadWidget();
        }
        catch (Exception ex)
        {
            var logger = _services.GetService<Microsoft.Extensions.Logging.ILogger<WidgetContainer>>();
            logger?.LogError(ex, "Failed to apply theme colors");
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _longPressTimer?.Stop();
        _longPressTriggered = false;

        if (!_isEditMode)
        {
            // Long-press in normal mode enters edit mode
            _dragStart = e.GetPosition(this);
            _longPressTimer?.Start();
            return;
        }

        _isDragging = false;
        _dragStart = e.GetPosition(this);
        _positionStart = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
        e.Pointer.Capture(this);
        e.Handled = true;

        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isEditMode)
        {
            // Cancel long-press if finger/mouse moves significantly
            var current = e.GetPosition(this);
            var delta = current - _dragStart;
            if (Math.Abs(delta.X) > 3 || Math.Abs(delta.Y) > 3)
            {
                _longPressTimer?.Stop();
                _longPressTriggered = false;
            }
            return;
        }

        if (e.Pointer.Captured != this) return;

        var current = e.GetPosition(this);
        var delta = current - _dragStart;

        if (!_isDragging && (Math.Abs(delta.X) > 3 || Math.Abs(delta.Y) > 3))
        {
            _isDragging = true;
            DragStateChanged?.Invoke(this, true);
        }

        if (!_isDragging) return;

        var newX = _positionStart.X + delta.X;
        var newY = _positionStart.Y + delta.Y;

        // Snap to grid
        var snappedX = Math.Round(newX / GridSize) * GridSize;
        var snappedY = Math.Round(newY / GridSize) * GridSize;

        if (Math.Abs(newX - snappedX) < 20)
        {
            newX = snappedX;
        }
        if (Math.Abs(newY - snappedY) < 20)
        {
            newY = snappedY;
        }

        Canvas.SetLeft(this, Math.Max(0, newX));
        Canvas.SetTop(this, Math.Max(0, newY));
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _longPressTimer?.Stop();

        if (e.Pointer.Captured != this)
        {
            _longPressTriggered = false;
            return;
        }

        e.Pointer.Capture(null);

        _isDragging = false;
        DragStateChanged?.Invoke(this, false);
        e.Handled = true;
    }

    private static IBrush GetThemedBrush(string resourceKey)
    {
        if (Application.Current?.TryGetResource(resourceKey, out var resource) == true && resource is IBrush brush)
        {
            return brush;
        }
        return Brushes.Transparent;
    }
}
