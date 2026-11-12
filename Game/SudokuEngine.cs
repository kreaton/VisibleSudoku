using System.Numerics;

namespace VisibleSudoku.Game;

public enum Difficulty { Easy, Medium, Hard, Expert }

/// <summary>Sudoku generator and solver. Cells are indexed 0..80 (row-major); 0 means empty.</summary>
public static class SudokuEngine
{
    const int AllDigits = 0x3FE; // bits 1..9

    public static readonly int[][] Peers = BuildPeers();
    static readonly int[][] Units = BuildUnits();

    public static int Row(int i) => i / 9;
    public static int Col(int i) => i % 9;
    public static int Box(int i) => i / 27 * 3 + i % 9 / 3;

    public static (int[] Puzzle, int[] Solution) Generate(Difficulty difficulty, Random rng)
    {
        var solution = RandomSolution(rng);

        // Expert puzzles should not be solvable with simple singles; try a few times.
        int attempts = difficulty == Difficulty.Expert ? 6 : 1;
        int[] best = Carve(solution, difficulty, rng);
        for (int a = 1; a < attempts && SolveBySingles(best, hidden: true); a++)
            best = Carve(solution, difficulty, rng);

        return (best, solution);
    }

    static int[] Carve(int[] solution, Difficulty d, Random rng)
    {
        int target = d switch { Difficulty.Easy => 42, Difficulty.Medium => 34, Difficulty.Hard => 29, _ => 24 };
        var puzzle = (int[])solution.Clone();
        var order = Enumerable.Range(0, 81).OrderBy(_ => rng.Next()).ToArray();
        int clues = 81;

        foreach (int i in order)
        {
            if (clues <= target) break;
            int old = puzzle[i];
            puzzle[i] = 0;
            bool ok = CountSolutions(puzzle, 2) == 1;
            if (ok && d == Difficulty.Easy) ok = SolveBySingles(puzzle, hidden: false);
            if (ok && d == Difficulty.Medium) ok = SolveBySingles(puzzle, hidden: true);
            if (ok) clues--; else puzzle[i] = old;
        }
        return puzzle;
    }

    // ---------- Solver helpers ----------

    static void Masks(int[] g, int[] rows, int[] cols, int[] boxes)
    {
        for (int i = 0; i < 81; i++)
        {
            if (g[i] == 0) continue;
            int bit = 1 << g[i];
            rows[Row(i)] |= bit; cols[Col(i)] |= bit; boxes[Box(i)] |= bit;
        }
    }

    public static int CountSolutions(int[] puzzle, int limit)
    {
        var g = (int[])puzzle.Clone();
        int[] r = new int[9], c = new int[9], b = new int[9];
        Masks(g, r, c, b);
        return Count(g, r, c, b, limit);
    }

    static int Count(int[] g, int[] r, int[] c, int[] b, int limit)
    {
        int best = -1, bestMask = 0, bestCount = 10;
        for (int i = 0; i < 81; i++)
        {
            if (g[i] != 0) continue;
            int mask = ~(r[i / 9] | c[i % 9] | b[Box(i)]) & AllDigits;
            int n = BitOperations.PopCount((uint)mask);
            if (n == 0) return 0;
            if (n < bestCount) { best = i; bestMask = mask; bestCount = n; if (n == 1) break; }
        }
        if (best < 0) return 1;

        int total = 0, row = best / 9, col = best % 9, box = Box(best);
        for (int v = 1; v <= 9; v++)
        {
            if ((bestMask >> v & 1) == 0) continue;
            int bit = 1 << v;
            g[best] = v; r[row] |= bit; c[col] |= bit; b[box] |= bit;
            total += Count(g, r, c, b, limit);
            g[best] = 0; r[row] &= ~bit; c[col] &= ~bit; b[box] &= ~bit;
            if (total >= limit) break;
        }
        return total;
    }

    static int[] RandomSolution(Random rng)
    {
        var g = new int[81];
        int[] r = new int[9], c = new int[9], b = new int[9];
        Fill(g, r, c, b, rng);
        return g;
    }

    static bool Fill(int[] g, int[] r, int[] c, int[] b, Random rng)
    {
        int best = -1, bestMask = 0, bestCount = 10;
        for (int i = 0; i < 81; i++)
        {
            if (g[i] != 0) continue;
            int mask = ~(r[i / 9] | c[i % 9] | b[Box(i)]) & AllDigits;
            int n = BitOperations.PopCount((uint)mask);
            if (n == 0) return false;
            if (n < bestCount) { best = i; bestMask = mask; bestCount = n; if (n == 1) break; }
        }
        if (best < 0) return true;

        var digits = new List<int>();
        for (int v = 1; v <= 9; v++) if ((bestMask >> v & 1) != 0) digits.Add(v);
        for (int k = digits.Count - 1; k > 0; k--) { int j = rng.Next(k + 1); (digits[k], digits[j]) = (digits[j], digits[k]); }

        int row = best / 9, col = best % 9, box = Box(best);
        foreach (int v in digits)
        {
            int bit = 1 << v;
            g[best] = v; r[row] |= bit; c[col] |= bit; b[box] |= bit;
            if (Fill(g, r, c, b, rng)) return true;
            g[best] = 0; r[row] &= ~bit; c[col] &= ~bit; b[box] &= ~bit;
        }
        return false;
    }

    /// <summary>True if the puzzle can be completed using only naked singles (and hidden singles if requested).</summary>
    public static bool SolveBySingles(int[] puzzle, bool hidden)
    {
        var g = (int[])puzzle.Clone();
        int[] r = new int[9], c = new int[9], b = new int[9];
        Masks(g, r, c, b);

        void Place(int i, int v)
        {
            int bit = 1 << v;
            g[i] = v; r[i / 9] |= bit; c[i % 9] |= bit; b[Box(i)] |= bit;
        }
        int Cand(int i) => ~(r[i / 9] | c[i % 9] | b[Box(i)]) & AllDigits;

        bool progress = true;
        while (progress)
        {
            progress = false;
            for (int i = 0; i < 81; i++)
            {
                if (g[i] != 0) continue;
                int m = Cand(i);
                if (m == 0) return false;
                if ((m & (m - 1)) == 0) { Place(i, BitOperations.TrailingZeroCount(m)); progress = true; }
            }
            if (progress || !hidden) continue;

            foreach (var unit in Units)
            {
                for (int v = 1; v <= 9 && !progress; v++)
                {
                    int bit = 1 << v, spot = -1, n = 0;
                    bool placed = false;
                    foreach (int i in unit)
                    {
                        if (g[i] == v) { placed = true; break; }
                        if (g[i] == 0 && (Cand(i) & bit) != 0) { spot = i; n++; }
                    }
                    if (!placed && n == 1) { Place(spot, v); progress = true; }
                }
                if (progress) break;
            }
        }
        return g.All(x => x != 0);
    }

    static int[][] BuildPeers()
    {
        var res = new int[81][];
        for (int i = 0; i < 81; i++)
            res[i] = Enumerable.Range(0, 81)
                .Where(j => j != i && (Row(i) == Row(j) || Col(i) == Col(j) || Box(i) == Box(j)))
                .ToArray();
        return res;
    }

    static int[][] BuildUnits()
    {
        var u = new List<int[]>();
        for (int k = 0; k < 9; k++)
        {
            u.Add(Enumerable.Range(0, 81).Where(i => Row(i) == k).ToArray());
            u.Add(Enumerable.Range(0, 81).Where(i => Col(i) == k).ToArray());
            u.Add(Enumerable.Range(0, 81).Where(i => Box(i) == k).ToArray());
        }
        return u.ToArray();
    }
}
