using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KursovaRudenko.Models;

public class MinConflictsStepInfo
{
    // 1. Поточний стан дошки (де зараз стоїть кожен ферзь)
    public int[] Queens { get; init; } = Array.Empty<int>();
    
    // 2. Список номерів рядків, у яких ферзі б'ють один одного
    public int[] ConflictedRows { get; init; } = Array.Empty<int>();
    
    // 3. Якого саме проблемного ферзя (рядок) алгоритм обрав для ходу
    public int SelectedRow { get; init; }
    
    // 4. Старий стовпець цього ферзя (звідки він йде)
    public int OldCol { get; init; }
    
    // 5. Оцінка конфліктів для кожної клітинки цього рядка (наприклад: [2, 3, 0, 1])
    public int[] RowConflicts { get; init; } = Array.Empty<int>();
    
    // 6. Новий стовпець із мінімумом конфліктів (куди ферзь перейде)
    public int BestCol { get; init; }
    
    // 7. Чи досягнуто розв'язку (конфліктів більше немає)
    public bool IsSolved { get; init; }
}

public class MinConflictsSolver
{
    private int[] _queens = Array.Empty<int>();
    private int[] _cols = Array.Empty<int>();
    private int[] _diag1 = Array.Empty<int>();
    private int[] _diag2 = Array.Empty<int>();
    private Random _random = new Random();

    // Захист від нескінченного циклу: максимальна кількість "перезапусків" дошки
    private const int MaxRestarts = 1000;

    public async Task<int[]> SolveVisualAsync(int n, Func<MinConflictsStepInfo, Task> onStep, CancellationToken cancellationToken = default)
{
    // Ініціалізація дошки (як і в звичайному Solve)
    _queens = new int[n];
    _cols = new int[n];
    _diag1 = new int[2 * n - 1];
    _diag2 = new int[2 * n - 1];
    int[] bestCols = new int[n];
    int[] conflictingRows = new int[n];

    InitializeBoard(n, bestCols);

    int maxIterations = Math.Max(1000, n * 5);

    for (int restart = 0; restart < MaxRestarts; restart++)
    {
        for (int iter = 0; iter < maxIterations; iter++)
        {
            // Перевіряємо, чи користувач не натиснув "Стоп"
            cancellationToken.ThrowIfCancellationRequested();

            // 1. Рахуємо конфлікти
            int conflictingCount = 0;
            for (int r = 0; r < n; r++)
            {
                int c = _queens[r];
                if (_cols[c] > 1 || _diag1[r + c] > 1 || _diag2[r - c + n - 1] > 1)
                {
                    conflictingRows[conflictingCount++] = r;
                }
            }

            // Якщо розв'язок знайдено!
            if (conflictingCount == 0)
            {
                // Відправляємо фінальний крок успіху в інтерфейс
                await onStep(new MinConflictsStepInfo
                {
                    Queens = (int[])_queens.Clone(),
                    IsSolved = true
                });

                return _queens;
            }

            // 2. Обираємо випадкового проблемного ферзя
            int randomRow = conflictingRows[_random.Next(conflictingCount)];
            int oldCol = _queens[randomRow];

            // Тимчасово прибираємо його з підрахунку
            _cols[oldCol]--;
            _diag1[randomRow + oldCol]--;
            _diag2[randomRow - oldCol + n - 1]--;

            // 3. Рахуємо конфлікти для ВСІХ колонок цього рядка
            int[] rowConflicts = new int[n];
            int minConflicts = int.MaxValue;
            int bestCount = 0;

            for (int c = 0; c < n; c++)
            {
                int conflicts = _cols[c] + _diag1[randomRow + c] + _diag2[randomRow - c + n - 1];
                rowConflicts[c] = conflicts;

                if (conflicts < minConflicts)
                {
                    minConflicts = conflicts;
                    bestCount = 0;
                    bestCols[bestCount++] = c;
                }
                else if (conflicts == minConflicts)
                {
                    bestCols[bestCount++] = c;
                }
            }

            int newCol = bestCols[_random.Next(bestCount)];

            // ---------------------------------------------------------
            // ОСЬ ТУТ МАГІЯ: Формуємо зліпок кроку і відправляємо у ViewModel!
            // ---------------------------------------------------------
            var stepInfo = new MinConflictsStepInfo
            {
                Queens = (int[])_queens.Clone(),
                ConflictedRows = conflictingRows.Take(conflictingCount).ToArray(),
                SelectedRow = randomRow,
                OldCol = oldCol,
                RowConflicts = rowConflicts,
                BestCol = newCol,
                IsSolved = false
            };

            // Алгоритм зупиняється і ЧЕКАЄ (await), поки інтерфейс програє анімацію!
            await onStep(stepInfo);

            // 4. Застосовуємо хід і переходимо до наступної ітерації
            _queens[randomRow] = newCol;
            _cols[newCol]++;
            _diag1[randomRow + newCol]++;
            _diag2[randomRow - newCol + n - 1]++;
        }

        InitializeBoard(n, bestCols);
    }

    throw new InvalidOperationException("Розв'язок не знайдено за ліміт спроб.");
}
    
    
    
    public int[] Solve(int n)
    {
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n), "Розмір дошки не може бути від'ємним.");

        // Тривіальні випадки: порожня дошка та дошка 1x1
        if (n == 0)
            return Array.Empty<int>();

        if (n == 1)
            return new int[] { 0 };

        // Для n = 2 і n = 3 розв'язку не існує в принципі (доведено математично)
        if (n == 2 || n == 3)
            throw new InvalidOperationException(
                $"Розв'язку задачі {n}-ферзів не існує.");

        _queens = new int[n];
        _cols = new int[n];
        _diag1 = new int[2 * n - 1];
        _diag2 = new int[2 * n - 1];
        int[] bestCols = new int[n];
        int[] conflictingRows = new int[n];

        // 1. Початкова жадібна розстановка з нуля
        InitializeBoard(n, bestCols);

        // Максимальна кількість ітерацій перед тим, як зробити рестарт
        int maxIterations = Math.Max(1000, n * 5);

        for (int restart = 0; restart < MaxRestarts; restart++)
        {
            // 2. Ітеративний цикл мінімізації конфліктів
            for (int iter = 0; iter < maxIterations; iter++)
            {
                int conflictingCount = 0;

                for (int r = 0; r < n; r++)
                {
                    int c = _queens[r];
                    if (_cols[c] > 1 || _diag1[r + c] > 1 || _diag2[r - c + n - 1] > 1)
                    {
                        conflictingRows[conflictingCount++] = r;
                    }
                }

                // Успіх! Конфліктів немає
                if (conflictingCount == 0)
                {
                    return _queens;
                }

                // Вибираємо випадкового проблемного ферзя
                int randomRow = conflictingRows[_random.Next(conflictingCount)];
                int oldCol = _queens[randomRow];

                _cols[oldCol]--;
                _diag1[randomRow + oldCol]--;
                _diag2[randomRow - oldCol + n - 1]--;

                int minConflicts = int.MaxValue;
                int bestCount = 0;

                for (int c = 0; c < n; c++)
                {
                    int conflicts = _cols[c] + _diag1[randomRow + c] + _diag2[randomRow - c + n - 1];
                    if (conflicts < minConflicts)
                    {
                        minConflicts = conflicts;
                        bestCount = 0;
                        bestCols[bestCount++] = c;
                    }
                    else if (conflicts == minConflicts)
                    {
                        bestCols[bestCount++] = c;
                    }
                }

                int newCol = bestCols[_random.Next(bestCount)];
                _queens[randomRow] = newCol;

                _cols[newCol]++;
                _diag1[randomRow + newCol]++;
                _diag2[randomRow - newCol + n - 1]++;
            }

            // Якщо ми застрягли (перевищили ліміт ітерацій) —
            // робимо повний перезапуск (жорсткий рестарт)
            InitializeBoard(n, bestCols);
        }

        throw new InvalidOperationException(
            $"Не вдалося знайти розв'язок для n={n} за {MaxRestarts} рестартів.");
    }

    private void InitializeBoard(int n, int[] bestCols)
    {
        Array.Clear(_cols, 0, _cols.Length);
        Array.Clear(_diag1, 0, _diag1.Length);
        Array.Clear(_diag2, 0, _diag2.Length);

        for (int r = 0; r < n; r++)
        {
            int minConflicts = int.MaxValue;
            int bestCount = 0;

            for (int c = 0; c < n; c++)
            {
                int conflicts = _cols[c] + _diag1[r + c] + _diag2[r - c + n - 1];
                if (conflicts < minConflicts)
                {
                    minConflicts = conflicts;
                    bestCount = 0;
                    bestCols[bestCount++] = c;
                }
                else if (conflicts == minConflicts)
                {
                    bestCols[bestCount++] = c;
                }
            }

            int selectedCol = bestCols[_random.Next(bestCount)];
            _queens[r] = selectedCol;

            _cols[selectedCol]++;
            _diag1[r + selectedCol]++;
            _diag2[r - selectedCol + n - 1]++;
        }
    }
}
