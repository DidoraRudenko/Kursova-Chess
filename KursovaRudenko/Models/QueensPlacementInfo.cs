using System;
using System.Collections.Generic;

namespace KursovaRudenko.Models;

/// <summary>
/// Надає інформацію щодо кількості розв'язків задачі N ферзів
/// на шаховій дошці розміром N x N.
/// </summary>
public static class QueensPlacementInfo
{
    // Загальна кількість розв'язків задачі N ферзів (OEIS A000170)
    // Обчислено сучасною наукою до N = 27 (проєкт QBiS, 2016 р.)
    private static readonly Dictionary<int, long> TotalSolutions = new()
    {
        [1] = 1,
        [2] = 0,
        [3] = 0,
        [4] = 2,
        [5] = 10,
        [6] = 4,
        [7] = 40,
        [8] = 92,
        [9] = 352,
        [10] = 724,
        [11] = 2680,
        [12] = 14200,
        [13] = 73712,
        [14] = 365596,
        [15] = 2279184,
        [16] = 14772512,
        [17] = 95815104,
        [18] = 666090624,
        [19] = 4968057848L,
        [20] = 39029188884L,
        [21] = 314666222712L,
        [22] = 2691008701644L,
        [23] = 24233937684440L,
        [24] = 227514171973736L,
        [25] = 2207893435808352L,
        [26] = 22317699616364044L,
        [27] = 234907967154122528L
    };

    // Кількість фундаментальних (унікальних) розв'язків без урахування поворотів і симетрій (OEIS A002562)
    private static readonly Dictionary<int, long> UniqueSolutions = new()
    {
        [1] = 1,
        [2] = 0,
        [3] = 0,
        [4] = 1,
        [5] = 2,
        [6] = 1,
        [7] = 6,
        [8] = 12,
        [9] = 46,
        [10] = 92,
        [11] = 341,
        [12] = 1787,
        [13] = 9233,
        [14] = 45752,
        [15] = 285053,
        [16] = 1846955,
        [17] = 11977939,
        [18] = 83263591,
        [19] = 621012754L,
        [20] = 4878666808L,
        [21] = 39333324973L,
        [22] = 336376244042L,
        [23] = 3029242658210L,
        [24] = 28439272956934L,
        [25] = 275986683743434L,
        [26] = 2789712466510289L,
        [27] = 29363495934315694L
    };

    /// <summary>
    /// Отримати текст для загальної кількості розв'язків.
    /// </summary>
    public static string GetTotalSolutionsText(int n)
    {
        if (n <= 0) return "—";

        if (n == 2 || n == 3)
        {
            return "0 (розв'язків не існує)";
        }

        if (TotalSolutions.TryGetValue(n, out long count))
        {
            return count.ToString("N0");
        }

        // Для N > 27 точне число розв'язків у математиці наразі невідоме
        // Асимптотична оцінка Симкіна (2021): розв'язків приблизно (0.143 * n)^n
        double baseVal = 0.143 * n;
        if (baseVal > 1)
        {
            double log10Approx = n * Math.Log10(baseVal);
            int exp = (int)Math.Floor(log10Approx);
            double mantissa = Math.Pow(10, log10Approx - exp);
            return $"~{mantissa:F2} × 10^{exp} (оцінка, N > 27)";
        }

        return "Невідомо (N > 27)";
    }

    /// <summary>
    /// Отримати текст для кількості унікальних (фундаментальних) розв'язків.
    /// </summary>
    public static string GetUniqueSolutionsText(int n)
    {
        if (n <= 0) return "—";

        if (n == 2 || n == 3)
        {
            return "0";
        }

        if (UniqueSolutions.TryGetValue(n, out long count))
        {
            return count.ToString("N0");
        }

        return "Невідомо (N > 27)";
    }

    /// <summary>
    /// Сумісність для виклику загальної кількості розв'язків.
    /// </summary>
    public static string GetSolutionsCountText(int n) => GetTotalSolutionsText(n);
}
