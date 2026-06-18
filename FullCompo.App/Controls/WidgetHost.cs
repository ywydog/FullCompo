using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using FullCompo.App.Views;
using FullCompo.Core.Abstractions;
using FullCompo.Shared.Models;

namespace FullCompo.App.Controls;

public class WidgetHost : Border
{
    private readonly PanelWindow _panelWindow;
    private readonly WidgetInstanceConfig _config;
    private readonly IWidget _widget;
    private Control _widgetView = null!;
    private bool _isDragging;
    private Point _dragStartPosition;
    private int _dragStartRow;
    private int _dragStartColumn;

    public WidgetHost(PanelWindow panelWindow, WidgetInstanceConfig config, IWidget widget)
    {
        _panelWindow = panelWindow;
        _config = config;
        _widget = widget;

        BorderThickness = new Thickness(0);
        CornerRadius = new CornerRadius(4);
        Background = Brushes.Transparent;
        Padding = new Thickness(2);

        this.PointerPressed += OnPointerPressed;
        this.PointerMoved += OnPointerMoved;
        this.PointerReleased += OnPointerReleased;
        this.PointerEntered += OnPointerEntered;
        this.PointerExited += OnPointerExited;

        SetupContextMenu();
    }

    public void SetWidgetView(Control view)
    {
        _widgetView = view;
        Child = view;
    }

    public void SetEditMode(bool isEditMode)
    {
        IsHitTestVisible = isEditMode;
        Cursor = isEditMode ? new Cursor(StandardCursorType.Hand) : Cursor.Default;

        if (isEditMode)
        {
            BorderBrush = new SolidColorBrush(Colors.White);
            BorderThickness = new Thickness(1);
            Background = new SolidColorBrush(Color.Parse("#22FFFFFF"));
        }
        else
        {
            BorderBrush = null;
            BorderThickness = new Thickness(0);
            Background = Brushes.Transparent;
        }
    }

    private void SetupContextMenu()
    {
        var contextMenu = new ContextMenu();

        var settingsItem = new MenuItem { Header = "组件设置" };
        settingsItem.Click += (_, _) => OpenSettings();

        var resizeItem = new MenuItem { Header = "切换尺寸" };
        SetupResizeMenu(resizeItem);

        var deleteItem = new MenuItem { Header = "删除组件" };
        deleteItem.Click += (_, _) => _panelWindow.RemoveWidget(_config);

        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(resizeItem);
        contextMenu.Items.Add(deleteItem);

        ContextMenu = contextMenu;
    }

    private void SetupResizeMenu(MenuItem parent)
    {
        foreach (var size in _widget.SupportedSizes)
        {
            var item = new MenuItem { Header = $"{size.Name} ({size.Columns}x{size.Rows})" };
            var capturedSize = size;
            item.Click += (_, _) => ResizeWidget(capturedSize);
            parent.Items.Add(item);
        }
    }

    private void ResizeWidget(WidgetSize size)
    {
        _config.ColumnSpan = size.Columns;
        _config.RowSpan = size.Rows;
        _panelWindow.ReloadLayout();
        _panelWindow.SaveLayout();
    }

    private void OpenSettings()
    {
        var settingsView = _widget.CreateSettingsView(_config.Settings);
        if (settingsView == null) return;

        var dialog = new SettingsDialog(_widget.Name, settingsView);
        dialog.Show(_panelWindow);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_panelWindow.IsEditMode) return;

        _isDragging = true;
        _dragStartPosition = e.GetPosition(_panelWindow);
        _dragStartRow = _config.Row;
        _dragStartColumn = _config.Column;
        e.Pointer.Capture(this);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging) return;

        var currentPosition = e.GetPosition(_panelWindow);
        var delta = currentPosition - _dragStartPosition;

        var cellWidth = _panelWindow.Config.CellWidth + _panelWindow.Config.Spacing;
        var cellHeight = _panelWindow.Config.CellHeight + _panelWindow.Config.Spacing;

        var deltaColumns = (int)Math.Round(delta.X / cellWidth);
        var deltaRows = (int)Math.Round(delta.Y / cellHeight);

        var newColumn = Math.Max(0, _dragStartColumn + deltaColumns);
        var newRow = Math.Max(0, _dragStartRow + deltaRows);

        if (newColumn != _config.Column || newRow != _config.Row)
        {
            _config.Column = newColumn;
            _config.Row = newRow;
            _panelWindow.ReloadLayout();
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging) return;

        _isDragging = false;
        e.Pointer.Capture(null);
        _panelWindow.SaveLayout();
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (_panelWindow.IsEditMode)
        {
            Background = new SolidColorBrush(Color.Parse("#44FFFFFF"));
        }
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (_panelWindow.IsEditMode)
        {
            Background = new SolidColorBrush(Color.Parse("#22FFFFFF"));
        }
    }
}
