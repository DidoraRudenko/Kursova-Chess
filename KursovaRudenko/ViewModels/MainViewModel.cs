using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KursovaRudenko.Models;

namespace KursovaRudenko.ViewModels;



public partial class MainViewModel : ObservableObject
{
    // ================= 1. ВЛАСТИВОСТІ СТАНУ =================
    public ObservableCollection<AttackArrow> AttackArrows { get; } = new();
    
    [ObservableProperty]
    private int _boardSize = 8;

    [ObservableProperty]
    private string _executionTimeMessage = "Час виконання: -";

    [ObservableProperty]
    private string _averageTimeMessage = "";

    [ObservableProperty]
    private string _solutionsCountMessage = "";

    [ObservableProperty]
    private string _uniqueSolutionsCountMessage = "";

    // Налаштування та стан симуляції
    [ObservableProperty]
    private int _animationSpeedMs = 400;

    [ObservableProperty]
    private string _pauseResumeButtonText = "Пауза";

    [ObservableProperty]
    private string _currentStepDescription = "Очікування запуску...";

    public ObservableCollection<CellViewModel> Cells { get; } = new();

    // Статистика бенчмарків
    private double _minConflictsTotalTimeMs = 0;
    private int _minConflictsAttempts = 0;
    private double _mathTotalTimeMs = 0;
    private int _mathAttempts = 0;

    // Керування асинхронним потоком анімації
    private CancellationTokenSource? _simulationCts;
    private TaskCompletionSource<bool>? _stepTcs;
    private bool _isPaused = false;
    private bool _isSimulationRunning = false;
    private readonly Random _random = new();

    // ================= 2. КОНСТРУКТОР ТА ПОДІЇ =================

    public MainViewModel()
    {
        GenerateBoard();
        UpdatePlacementInfo();
    }

    partial void OnBoardSizeChanged(int value)
    {
        StopSimulation();

        _minConflictsTotalTimeMs = 0;
        _minConflictsAttempts = 0;
        _mathTotalTimeMs = 0;
        _mathAttempts = 0;

        ExecutionTimeMessage = "Час виконання: -";
        AverageTimeMessage = "";
        CurrentStepDescription = "Розмір дошки змінено. Натисніть 'Почати симуляцію'.";

        UpdatePlacementInfo();
        GenerateBoard();
    }

    private void UpdatePlacementInfo()
    {
        SolutionsCountMessage = $"Всі можливі розв'язки: {QueensPlacementInfo.GetTotalSolutionsText(BoardSize)}";
        UniqueSolutionsCountMessage = $"Унікальні розв'язки: {QueensPlacementInfo.GetUniqueSolutionsText(BoardSize)}";
    }

    // ================= 3. ГЕНЕРАЦІЯ ТА ВІДМАЛЬОВКА ДОШКИ =================

    [RelayCommand]
    private void CreateBoard() => GenerateBoard();

    [RelayCommand]
    private void GenerateBoard()
    {
        StopSimulation();
        Cells.Clear();

        if (BoardSize > 100)
        {
            ExecutionTimeMessage = $"Увага: Графіка вимкнена для N={BoardSize}. Працює лише розрахунок!";
            return;
        }

        for (int row = 0; row < BoardSize; row++)
        {
            for (int col = 0; col < BoardSize; col++)
            {
                bool isWhite = (row + col) % 2 == 0;
                var baseColor = isWhite ? Brushes.Wheat : Brushes.SaddleBrown;
                Cells.Add(new CellViewModel(baseColor));
            }
        }
    }

    private void ResetBoardVisuals()
    {
        for (int row = 0; row < BoardSize; row++)
        {
            for (int col = 0; col < BoardSize; col++)
            {
                var cell = Cells[row * BoardSize + col];
                bool isWhite = (row + col) % 2 == 0;
                cell.BackgroundColor = isWhite ? Brushes.Wheat : Brushes.SaddleBrown;
                cell.CellOpacity = 1.0;
                cell.ConflictWeight = null;
                cell.QueenScale = cell.HasQueen ? 1.0 : 0.0;
            }
        }
    }

    private void ClearQueens()
    {
        foreach (var cell in Cells)
        {
            cell.HasQueen = false;
            cell.QueenOpacity = 0.0;
            cell.QueenScale = 0.0;
            cell.ConflictWeight = null;
        }
    }

    private void DrawSolution(int[] solution)
    {
        ClearQueens();
        ResetBoardVisuals();

        for (int row = 0; row < BoardSize; row++)
        {
            int col = solution[row];
            int cellIndex = (row * BoardSize) + col;
            var cell = Cells[cellIndex];
            cell.HasQueen = true;
            cell.QueenOpacity = 1.0;
            cell.QueenScale = 1.0;
        }
    }

    // ================= 4. НАОЧНА СИМУЛЯЦІЯ ЕВРИСТИКИ =================

    [RelayCommand]
    private async Task StartVisualSimulationAsync()
    {
        if (_isSimulationRunning) return;

        if (BoardSize < 4)
        {
            CurrentStepDescription = $"Для N={BoardSize} розв'язку не існує!";
            return;
        }

        if (BoardSize > 30)
        {
            CurrentStepDescription = "Увага: Візуалізація оптимізована для N ≤ 30. Для більших N використовуйте швидкий розрахунок.";
            return;
        }

        _isSimulationRunning = true;
        _isPaused = false;
        PauseResumeButtonText = "Пауза";
        _simulationCts = new CancellationTokenSource();
        var token = _simulationCts.Token;

        try
        {
            CurrentStepDescription = "Початкова розстановка ферзів...";
            ClearQueens();
            ResetBoardVisuals();

            int n = BoardSize;
            int[] queens = new int[n];
            int[] cols = new int[n];
            int[] diag1 = new int[2 * n - 1];
            int[] diag2 = new int[2 * n - 1];
            int[] conflictingRows = new int[n];
            int[] bestCols = new int[n];

            // 1. Початкова жадібна розстановка з плавною появою
            for (int r = 0; r < n; r++)
            {
                token.ThrowIfCancellationRequested();

                int selectedCol = _random.Next(n);
                queens[r] = selectedCol;
                cols[selectedCol]++;
                diag1[r + selectedCol]++;
                diag2[r - selectedCol + n - 1]++;

                var cell = Cells[r * n + selectedCol];
                cell.HasQueen = true;
                cell.QueenOpacity = 1.0;
                cell.QueenScale = 1.0;

                await WaitStepDelayAsync(token);
            }

            int maxIterations = Math.Max(1000, n * 8);

            // 2. Основний цикл мінімізації конфліктів
            for (int iter = 1; iter <= maxIterations; iter++)
            {
                token.ThrowIfCancellationRequested();
                ResetBoardVisuals();

                // Підрахунок конфліктних ферзів
                int conflictingCount = 0;
                for (int r = 0; r < n; r++)
                {
                    int c = queens[r];
                    if (cols[c] > 1 || diag1[r + c] > 1 || diag2[r - c + n - 1] > 1)
                    {
                        conflictingRows[conflictingCount++] = r;
                    }
                }

                // УСПІХ: Конфліктів немає!
                if (conflictingCount == 0)
                {
                    CurrentStepDescription = $"🎉 Успіх! Розв'язок знайдено за {iter} ітерацій без жодного конфлікту!";
                    for (int r = 0; r < n; r++)
                    {
                        Cells[r * n + queens[r]].BackgroundColor = Brushes.LimeGreen;
                    }
                    return;
                }

                // ФАЗА 1: ЗАТЕМНЕННЯ ТА ПІДСВІЧУВАННЯ КОНФЛІКТІВ
                CurrentStepDescription = $"[Ітерація {iter}] Знайдено {conflictingCount} ферзів під боєм. Виділено червоним.";
                
                // Затемнюємо всі клітинки дошки
                foreach (var cell in Cells) cell.CellOpacity = 0.35;

                AttackArrows.Clear();

                // Виділяємо конфліктних ферзів червоним
                for (int i = 0; i < conflictingCount; i++)
                {
                    
                    int r = conflictingRows[i];
                    int c = queens[r];
                    int attacks = cols[c] + diag1[r + c] + diag2[r - c + n - 1] - 3;
                    var cell = Cells[r * n + c];
                    cell.ConflictWeight = attacks;
                    cell.CellOpacity = 1.0;
                    cell.BackgroundColor = Brushes.Crimson;
                }

                await WaitStepDelayAsync(token);

                for (int i = 0; i < conflictingCount; i++)
                {
                    int r = conflictingRows[i];
                    int c = queens[r];
                    Cells[r * n + c].ConflictWeight = null;
                }
                
                // ФАЗА 2: ВИБІР ОДНОГО ПРОБЛЕМНОГО ФЕРЗЯ
                // 1. ОЧИЩАЄМО ЧЕРВОНИЙ КОЛІР (повертаємо звичайний колір клітинок)
                ResetBoardVisuals();
                //AttackArrows.Clear();
                
                
                
                int randomRow = conflictingRows[_random.Next(conflictingCount)];
                int oldCol = queens[randomRow];
                
                // 2. ПІДСВІЧУЄМО ОБРАНОГО ФЕРЗЯ ЖОВТИМ
                int rSel = randomRow;
                int cSel = queens[rSel];

                CurrentStepDescription = $"Обрано ферзя в рядку {randomRow + 1} (стовпець {oldCol + 1}) для пошуку кращої клітинки.";
                var selectedCell = Cells[randomRow * n + oldCol];
                selectedCell.BackgroundColor = Brushes.Gold;
                selectedCell.QueenScale = 1.25;

                // 3. ЗАТЕМНЮЄМО ВСЕ, КРІМ ОБРАНОГО ТА ЙОГО НАПАДНИКІВ
                foreach (var cell in Cells) cell.CellOpacity = 0.25;
                selectedCell.CellOpacity = 1.0;
// 4. ШУКАЄМО ТІЛЬКИ ТИХ ФЕРЗІВ, ЯКІ АТАКУЮТЬ ОБРАНОГО
                const double CellSize = 60.0; // розмір клітинки в пікселях
                for (int r = 0; r < n; r++)
                {
                    if (r == rSel) continue; // самого себе не перевіряємо
                    int c = queens[r];
                    // Перевірка: вертикаль або одна з двох діагоналей
                    bool isAttacking = (c == cSel) || 
                                       ((r + c) == (rSel + cSel)) || 
                                       ((r - c) == (rSel - cSel));
                    if (isAttacking)
                    {
                        // Підсвічуємо нападника РОЖЕВИМ
                        var attackerCell = Cells[r * n + c];
                        attackerCell.BackgroundColor = Brushes.HotPink;
                        attackerCell.CellOpacity = 1.0;
                        // Координати центрів клітинок для стрілочки
                        double startX = c * CellSize + CellSize / 2;
                        double startY = r * CellSize + CellSize / 2;
                        double endX = cSel * CellSize + CellSize / 2;
                        double endY = rSel * CellSize + CellSize / 2;
                        // Додаємо лінію атаки
                        AttackArrows.Add(new AttackArrow
                        {
                            StartPoint = new Point(startX, startY),
                            EndPoint = new Point(endX, endY)
                        });
                    }
                }
                
                
                // Список рядків ферзів-нападників для поточної ітерації
                List<int> currentAttackers = new();

                for (int r = 0; r < n; r++)
                {
                    if (r == rSel) continue;
                    int c = queens[r];

                    bool isAttacking = (c == cSel) || 
                                       ((r + c) == (rSel + cSel)) || 
                                       ((r - c) == (rSel - cSel));

                    if (isAttacking)
                    {
                        currentAttackers.Add(r); // <-- Запам'ятовуємо нападника

                        var attackerCell = Cells[r * n + c];
                        attackerCell.BackgroundColor = Brushes.HotPink;
                        attackerCell.CellOpacity = 1.0;

                    }
                }

                CurrentStepDescription = $"Обрано ферзя на ({rSel + 1}, {cSel + 1}). Рожевим підсвічено ферзів, які його атакують.";
// ПАУЗА: Користувач бачить жовтого ферзя, рожевих нападників і стрілочки між ними
                await WaitStepDelayAsync(token);
// Очищаємо стрілочки перед переходом до сканування рядка

                
                
                await WaitStepDelayAsync(token);

                // Тимчасово прибираємо ферзя з лічильників
                cols[oldCol]--;
                diag1[randomRow + oldCol]--;
                diag2[randomRow - oldCol + n - 1]--;

                // ФАЗА 3: СКАНУВАННЯ РЯДКА ТА ПОКАЗ ОЦІНОК КОНФЛІКТІВ
                int minConflicts = int.MaxValue;
                int bestCount = 0;

                for (int c = 0; c < n; c++)
                {
                    int conflicts = cols[c] + diag1[randomRow + c] + diag2[randomRow - c + n - 1];
                    var cell = Cells[randomRow * n + c];
                    cell.CellOpacity = 1.0;
                    cell.ConflictWeight = conflicts; // Виводимо цифру в клітинку

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
                CurrentStepDescription = $"Сканування рядка {randomRow + 1}: мінімум конфліктів = {minConflicts}. Найкращий стовпець: {newCol + 1}.";

                await WaitStepDelayAsync(token);

                // ФАЗА 4: ПЕРЕМІЩЕННЯ ФЕРЗЯ
                // Знімаємо цифри оцінок
                for (int c = 0; c < n; c++) Cells[randomRow * n + c].ConflictWeight = null;
                
                AttackArrows.Clear();
                
                // Прибираємо зі старої клітинки
                selectedCell.HasQueen = false;
                selectedCell.QueenOpacity = 0.0;
                selectedCell.QueenScale = 0.0;

                // Ставимо в нову
                var targetCell = Cells[randomRow * n + newCol];
                targetCell.HasQueen = true;
                targetCell.QueenOpacity = 1.0;
                targetCell.QueenScale = 1.0;
                targetCell.BackgroundColor = Brushes.LightGreen;

                foreach (int attRow in currentAttackers)
                {
                    int attCol = queens[attRow];
                    var attCell = Cells[attRow * n + attCol];
    
                    // Повертаємо клітинці нападника її початковий колір шахівниці
                    bool isCellWhite = (attRow + attCol) % 2 == 0;
                    attCell.BackgroundColor = isCellWhite ? Brushes.Wheat : Brushes.SaddleBrown;
                    attCell.CellOpacity = 0.35; 
                }
                currentAttackers.Clear();
                
                // Оновлюємо лічильники
                queens[randomRow] = newCol;
                cols[newCol]++;
                diag1[randomRow + newCol]++;
                diag2[randomRow - newCol + n - 1]++;

                CurrentStepDescription = $"Ферзя успішно переміщено в стовпець {newCol + 1}.";
                await WaitStepDelayAsync(token);
            }

            CurrentStepDescription = "Досягнуто ліміту ітерацій. Спробуйте перезапустити симуляцію.";
        }
        catch (OperationCanceledException)
        {
            CurrentStepDescription = "Симуляцію зупинено користувачем.";
        }
        finally
        {
            _isSimulationRunning = false;
            _isPaused = false;
            PauseResumeButtonText = "Пауза";
            ResetBoardVisuals();
        }
    }

    // ================= 5. КЕРУВАННЯ ПАУЗОЮ, КРОКАМИ ТА СТОПОМ =================

    private async Task WaitStepDelayAsync(CancellationToken token)
    {
        if (_isPaused)
        {
            _stepTcs = new TaskCompletionSource<bool>();
            using (token.Register(() => _stepTcs.TrySetCanceled()))
            {
                await _stepTcs.Task;
            }
        }
        else
        {
            await Task.Delay(AnimationSpeedMs, token);
        }
    }

    [RelayCommand]
    private void TogglePause()
    {
        if (!_isSimulationRunning) return;

        _isPaused = !_isPaused;
        PauseResumeButtonText = _isPaused ? "Продовжити" : "Пауза";

        if (!_isPaused)
        {
            // Якщо зняли з паузи — розблоковуємо очікування
            _stepTcs?.TrySetResult(true);
        }
    }

    [RelayCommand]
    private void StepForward()
    {
        if (!_isSimulationRunning) return;

        // Ставимо на паузу, якщо ще не стоїть
        _isPaused = true;
        PauseResumeButtonText = "Продовжити";

        // Пропускаємо рівно один крок
        _stepTcs?.TrySetResult(true);
    }

    [RelayCommand]
    private void StopSimulation()
    {
        _simulationCts?.Cancel();
        _stepTcs?.TrySetCanceled();
        _isSimulationRunning = false;
        _isPaused = false;
        PauseResumeButtonText = "Пауза";
    }

    // ================= 6. ШВИДКІ АЛГОРИТМИ (БЕНЧМАРКИ) =================

    [RelayCommand]
    private void SolveMinConflicts()
    {
        StopSimulation();

        if (BoardSize == 2 || BoardSize == 3)
        {
            ExecutionTimeMessage = $"Для N={BoardSize} розв'язків не існує!";
            return;
        }

        var solver = new MinConflictsSolver();
        var stopwatch = Stopwatch.StartNew();
        int[] solution = solver.Solve(BoardSize);
        stopwatch.Stop();

        double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
        _minConflictsTotalTimeMs += elapsedMs;
        _minConflictsAttempts++;
        double averageMs = _minConflictsTotalTimeMs / _minConflictsAttempts;

        string log = $"[Min-Conflicts N={BoardSize}] Знайдено за: {elapsedMs:F4} мс";
        ExecutionTimeMessage = log;
        AverageTimeMessage = $"Середній час (Евристика): {averageMs:F4} мс\nСпроб: {_minConflictsAttempts}";

        if (BoardSize <= 100) DrawSolution(solution);
        else ExecutionTimeMessage += "\n(Відмальовку пропущено для N > 100)";
    }

    [RelayCommand]
    private void SolveMath()
    {
        StopSimulation();

        if (BoardSize < 4 && BoardSize != 1)
        {
            ExecutionTimeMessage = $"Для N={BoardSize} розв'язків не існує!";
            return;
        }

        var solver = new MathSolver();
        var stopwatch = Stopwatch.StartNew();
        int[] solution = solver.Solve(BoardSize);
        stopwatch.Stop();

        double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
        _mathTotalTimeMs += elapsedMs;
        _mathAttempts++;
        double averageMs = _mathTotalTimeMs / _mathAttempts;

        string log = $"[Математичний підхід N={BoardSize}] Згенеровано за: {elapsedMs:F4} мс";
        ExecutionTimeMessage = log;
        AverageTimeMessage = $"Середній час (Математика): {averageMs:F4} мс\nСпроб: {_mathAttempts}";

        if (BoardSize <= 100) DrawSolution(solution);
        else ExecutionTimeMessage += "\n(Відмальовку пропущено для N > 100)";
    }
}