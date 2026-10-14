using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media;

namespace KursovaRudenko.ViewModels;

public partial class CellViewModel : ObservableObject
{
    [ObservableProperty]
    private IBrush _backgroundColor;

    [ObservableProperty]
    private bool _hasQueen;

    // Прозорість клітинки (затемнення фону при конфліктах)
    [ObservableProperty]
    private double _cellOpacity = 1.0;

    // Прозорість ферзя (0.0 - прихований, 1.0 - видимий)
    [ObservableProperty]
    private double _queenOpacity = 0.0;

    // Масштаб ферзя (для ефекту пружного збільшення)
    [ObservableProperty]
    private double _queenScale = 0.0;

    // Цифра атак/конфліктів (null = не показувати нічого)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasConflictWeight))]
    private int? _conflictWeight = null;

// Прапорець для XAML, щоб автоматично ховати блок, коли цифри немає
    public bool HasConflictWeight => ConflictWeight.HasValue;

    // Синонім для сумісності
    public IBrush Color => BackgroundColor;
    

    public CellViewModel(IBrush color)
    {
        BackgroundColor = color;
        HasQueen = false;
        QueenOpacity = 0.0;
        QueenScale = 0.0;
        CellOpacity = 1.0;
    }
}