using Avalonia;

namespace KursovaRudenko.Models;

public class AttackArrow
{
    // Початкова точка (ферзь-нападник)
    public Point StartPoint { get; set; }

    // Кінцева точка (обраний жовтий ферзь)
    public Point EndPoint { get; set; }
}