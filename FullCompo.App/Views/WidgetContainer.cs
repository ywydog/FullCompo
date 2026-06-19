using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Media;
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

    private const double GridSize = 80.0;

    public WidgetContainer(WidgetInstanceConfig config, IWidget widget, WidgetSize size, IServiceProvider services)
    {
        _services = services;
        _configService = services.GetRequiredService<IConfigService>();
        _themeService = services.GetRequiredService<IThemeService>();

        Config = config;
        Widget = widget;
        CurrentSize = size;

        BuildContainer();
        ApplySize();
        LoadWidget();
    }

    private void BuildContainer()
    {
        _rootGrid = new Grid();

        // Content area
        _contentBorder = new Border
        {
            Background = GetBackgroundBrush(),
            BorderBrush = GetBorderBrush(),
            BorderThickness = new Thickness(1),
            CornerRadius = CurrentSize.IsCircular ? new CornerRadius(CurrentSize.Width / 2) : new CornerRadius(18),
            Padding = new Thickness(8),
            BoxShadow = new BoxShadows(new BoxShadow
            {
                OffsetX = 0,
                OffsetY = 4,
                Blur = 12,
                Spread = 0,
                Color = Color.Parse("#26000000")
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
            Foreground = new SolidColorBrush(Colors.White),
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
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
        }
    }

    public void SetWidget(IWidget widget)
    {
        Widget = widget;
        Config.WidgetId = widget.Id;
        _label.Text = widget.Name;
        LoadWidget();
    }

    public void SetSize(WidgetSize size)
    {
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
            _contentBorder.BorderBrush = new SolidColorBrush(Color.Parse("#0078D4"));
            _contentBorder.BorderThickness = new Thickness(2);
            _deleteButton.IsVisible = true;
        }
        else
        {
            _contentBorder.BorderBrush = GetBorderBrush();
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

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_isEditMode) return;

        _isDragging = false;
        _dragStart = e.GetPosition(this);
        _positionStart = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
        e.Pointer.Capture(this);
        e.Handled = true;

        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isEditMode || e.Pointer.Captured != this) return;

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
        if (e.Pointer.Captured != this) return;

        e.Pointer.Capture(null);

        if (!_isDragging)
        {
            // It was a click, selection already handled in PointerPressed
        }

        _isDragging = false;
        DragStateChanged?.Invoke(this, false);
        e.Handled = true;
    }

    private IBrush GetBackgroundBrush()
    {
        var theme = _themeService.CurrentTheme;
        var color = theme.BackgroundColor;
        color = Color.FromArgb((byte)(255 * 0.92), color.R, color.G, color.B);
        return new SolidColorBrush(color);
    }

    private IBrush GetBorderBrush()
    {
        var theme = _themeService.CurrentTheme;
        return new SolidColorBrush(theme.BorderColor);
    }
}
