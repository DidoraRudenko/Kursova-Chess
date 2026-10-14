using System;
using System.Diagnostics;

namespace KursovaRudenko.Models;

public class MathSolver
{
    // Змінні для статистики Математики
    private double _mathTotalTimeMs = 0;
    private int _mathAttempts = 0;

    public int Attempts => _mathAttempts;
    public double AverageTimeMs => _mathAttempts == 0 ? 0 : _mathTotalTimeMs / _mathAttempts;

    /// <summary>
    /// Явний (безбектрекінговий) розв'язок задачі про N ферзів.
    /// Базується на конструкції Hoffman, Loessi, Moore (1969).
    /// Повертає масив finalSolution, де finalSolution[рядок] = стовпець.
    /// </summary>
    public int[] Solve(int n)
    {
        if (n == 1)
        {
            _mathAttempts++;
            return new int[] { 0 };
        }

        if (n < 4) throw new ArgumentException($"Для N = {n} розв'язків не існує.");

        var sw = Stopwatch.StartNew();

        int[] finalSolution = new int[n];
        SolveRecursive(n, finalSolution);

        sw.Stop();
        _mathAttempts++;
        _mathTotalTimeMs += sw.Elapsed.TotalMilliseconds;

        return finalSolution;
    }

    // Рекурсивно зводить N до парного випадку.
    // Якщо N непарне — ставимо ферзя в останню клітину головної діагоналі
    // (result[n-1] = n-1) і розв'язуємо задачу для N-1 (яке вже парне).
    private void SolveRecursive(int n, int[] result)
    {
        if (n % 2 != 0)
        {
            result[n - 1] = n - 1;
            SolveRecursive(n - 1, result);
        }
        else if (n % 6 != 0)
        {
            // n mod 6 == 2 або 4
            SolveEvenNotMultipleOf6(n, result);
        }
        else
        {
            // n mod 6 == 0
            SolveEvenMultipleOf6(n, result);
        }
    }

    // Випадок: N парне, N mod 6 != 0 (тобто N mod 6 == 2 або 4)
    private void SolveEvenNotMultipleOf6(int n, int[] result)
    {
        int half = n / 2;

        int GetY(int j) => (2 * (j - 1) + half - 1) % n; // j = 1..half

        for (int j = 1; j <= half; j++)
        {
            int row = n - j;
            int col = (n - 1) - GetY(j);
            result[row] = col;
        }
        for (int j = 1; j <= half; j++)
        {
            int row = j - 1;
            int col = GetY(j);
            result[row] = col;
        }
    }

    // Випадок: N парне, N mod 6 == 0
    private void SolveEvenMultipleOf6(int n, int[] result)
    {
        int half = n / 2;

        for (int j = 1; j <= half; j++)
        {
            int row = j - 1;
            int col = 2 * j - 1;
            result[row] = col;
        }
        for (int j = 1; j <= half; j++)
        {
            int row = half + j - 1;
            int col = 2 * j - 2;
            result[row] = col;
        }
    }
}
