using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using VisibleSudoku.Game;

namespace VisibleSudoku.Pages;

public partial class Home : IDisposable
{
    const string GameKey = "vs.game";

    [Inject] IJSRuntime JS { get; set; } = default!;

    GameState? game;
    GameState? saved;
    int selected = -1;
    bool notes, dark, menuOpen = true, busy;
    string message = "";
    readonly HashSet<int> flagged = new();
    readonly bool[] conflicts = new bool[81];
    readonly List<(int[] Values, int[] Notes)> undo = new();
    readonly CancellationTokenSource cts = new();
    int sinceSave;

    string TimeText => FormatTime(game?.Seconds ?? 0);

    static string FormatTime(int s)
    {
        var t = TimeSpan.FromSeconds(s);
        return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
    }

    protected override async Task OnInitializedAsync()
    {
        dark = await JS.InvokeAsync<string?>("vsStorage.get", "vs.theme") == "dark";
        try
        {
            var json = await JS.InvokeAsync<string?>("vsStorage.get", GameKey);
            if (!string.IsNullOrEmpty(json))
            {
                var g = JsonSerializer.Deserialize(json, AppJson.Default.GameState);
                if (g is { Completed: false } && g.Values.Length == 81 && g.Givens.Length == 81
                    && g.Solution.Length == 81 && g.Notes.Length == 81)
                    saved = g;
            }
        }
        catch { saved = null; }
        _ = RunTimer();
    }

    async Task RunTimer()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(cts.Token))
            {
                if (game is null || game.Completed || menuOpen || busy) continue;
                game.Seconds++;
                if (++sinceSave >= 5) await Save();
                StateHasChanged();
            }
        }
        catch (OperationCanceledException) { }
    }

    public void Dispose() => cts.Cancel();

    // ---------- Game flow ----------

    async Task StartNew(Difficulty d)
    {
        busy = true;
        StateHasChanged();
        await Task.Delay(60); // let the "Making a puzzle" screen paint
        var (puzzle, solution) = SudokuEngine.Generate(d, new Random());
        game = new GameState
        {
            Difficulty = d,
            Givens = puzzle,
            Solution = solution,
            Values = (int[])puzzle.Clone(),
            Notes = new int[81],
        };
        saved = null;
        selected = -1; notes = false; message = ""; undo.Clear(); flagged.Clear();
        menuOpen = false; busy = false;
        RecomputeConflicts();
        await Save();
    }

    void Continue()
    {
        game = saved;
        saved = null;
        selected = -1; undo.Clear(); flagged.Clear(); message = "";
        menuOpen = false;
        RecomputeConflicts();
    }

    void OpenMenu() => menuOpen = true;
    void CloseMenu() => menuOpen = false;

    async Task ToggleTheme()
    {
        dark = !dark;
        await JS.InvokeVoidAsync("vsStorage.setTheme", dark ? "dark" : "light");
    }

    async Task Save()
    {
        sinceSave = 0;
        if (game is null) return;
        if (game.Completed) await JS.InvokeVoidAsync("vsStorage.remove", GameKey);
        else await JS.InvokeVoidAsync("vsStorage.set", GameKey, JsonSerializer.Serialize(game, AppJson.Default.GameState));
    }

    // ---------- Input ----------

    void Select(int i) { selected = i; message = ""; }
    void ToggleNotes() => notes = !notes;

    bool CanEdit => game is { Completed: false } && selected >= 0 && game.Givens[selected] == 0;

    void PushUndo() 
    {
        undo.Add(((int[])game!.Values.Clone(), (int[])game.Notes.Clone()));
        if (undo.Count > 300) undo.RemoveAt(0);
    }

    async Task Input(int n)
    {
        if (game is null) return;
        if (selected < 0) { message = "Tap a square first"; return; }
        if (!CanEdit) { message = "That number is fixed"; return; }

        if (notes)
        {
            if (game.Values[selected] != 0) { message = "Erase the number to add notes"; return; }
            PushUndo();
            game.Notes[selected] ^= 1 << n;
            await Changed();
            return;
        }

        PushUndo();
        if (game.Values[selected] == n)
        {
            game.Values[selected] = 0;
        }
        else
        {
            game.Values[selected] = n;
            game.Notes[selected] = 0;
            if (n != game.Solution[selected]) game.Mistakes++;
            foreach (int p in SudokuEngine.Peers[selected]) game.Notes[p] &= ~(1 << n);
        }
        await Changed();
    }

    async Task Erase()
    {
        if (game is null) return;
        if (!CanEdit) { message = selected < 0 ? "Tap a square first" : "That number is fixed"; return; }
        if (game.Values[selected] == 0 && game.Notes[selected] == 0) return;
        PushUndo();
        game.Values[selected] = 0;
        game.Notes[selected] = 0;
        await Changed();
    }

    async Task Undo()
    {
        if (game is null || undo.Count == 0) return;
        var (v, n) = undo[^1];
        undo.RemoveAt(undo.Count - 1);
        game.Values = v; game.Notes = n;
        await Changed();
    }

    async Task Hint()
    {
        if (game is null || game.Completed) return;
        int target = -1;
        bool Needs(int i) => game.Givens[i] == 0 && game.Values[i] != game.Solution[i];
        if (selected >= 0 && Needs(selected)) target = selected;
        else
        {
            var open = Enumerable.Range(0, 81).Where(Needs).ToList();
            if (open.Count == 0) return;
            target = open[Random.Shared.Next(open.Count)];
        }
        PushUndo();
        selected = target;
        game.Values[target] = game.Solution[target];
        game.Notes[target] = 0;
        foreach (int p in SudokuEngine.Peers[target]) game.Notes[p] &= ~(1 << game.Values[target]);
        game.Hints++;
        await Changed();
        message = "Hint: the correct number was filled in";
    }

    async Task Check()
    {
        if (game is null || game.Completed) return;
        flagged.Clear();
        for (int i = 0; i < 81; i++)
            if (game.Values[i] != 0 && game.Values[i] != game.Solution[i]) flagged.Add(i);
        message = flagged.Count == 0 ? "All numbers so far are correct" :
                  flagged.Count == 1 ? "1 number is wrong (marked)" : $"{flagged.Count} numbers are wrong (marked)";
        await Task.CompletedTask;
    }

    async Task Changed()
    {
        if (game is null) return;
        flagged.Clear();
        message = "";
        RecomputeConflicts();
        if (game.Values.SequenceEqual(game.Solution))
        {
            game.Completed = true;
            selected = -1;
        }
        await Save();
    }

    void RecomputeConflicts()
    {
        if (game is null) return;
        for (int i = 0; i < 81; i++)
        {
            conflicts[i] = false;
            int v = game.Values[i];
            if (v == 0) continue;
            foreach (int p in SudokuEngine.Peers[i])
                if (game.Values[p] == v) { conflicts[i] = true; break; }
        }
    }

    async Task OnKey(KeyboardEventArgs e)
    {
        if (game is null || menuOpen || busy || game.Completed) return;
        switch (e.Key)
        {
            case { Length: 1 } when e.Key[0] is >= '1' and <= '9': await Input(e.Key[0] - '0'); break;
            case "Backspace" or "Delete" or "0": await Erase(); break;
            case "ArrowUp": Move(-9); break;
            case "ArrowDown": Move(9); break;
            case "ArrowLeft": Move(-1); break;
            case "ArrowRight": Move(1); break;
            case "n" or "N": notes = !notes; break;
            case "z" or "Z" when e.CtrlKey || e.MetaKey: await Undo(); break;
        }
    }

    void Move(int delta)
    {
        if (selected < 0) { selected = 40; return; }
        int next = selected + delta;
        if (next < 0 || next > 80) return;
        if (Math.Abs(delta) == 1 && next / 9 != selected / 9) return;
        selected = next;
    }

    // ---------- View helpers ----------

    int Remaining(int n) => 9 - (game?.Values.Count(v => v == n) ?? 0);

    string CellClass(int i)
    {
        var g = game!;
        var c = new List<string> { "cell" };
        int col = i % 9, row = i / 9;
        if (col % 3 == 2 && col != 8) c.Add("br");
        if (row % 3 == 2 && row != 8) c.Add("bb");
        c.Add(g.Givens[i] != 0 ? "given" : "entry");

        if (flagged.Contains(i) || conflicts[i]) c.Add("bad");
        else if (i == selected) c.Add("sel");
        else if (selected >= 0 && g.Values[selected] != 0 && g.Values[i] == g.Values[selected]) c.Add("same");
        else if (selected >= 0 && Array.IndexOf(SudokuEngine.Peers[selected], i) >= 0) c.Add("peer");
        return string.Join(' ', c);
    }

    string CellLabel(int i)
    {
        var g = game!;
        string s = $"Row {i / 9 + 1}, column {i % 9 + 1}, ";
        if (g.Values[i] != 0) s += g.Givens[i] != 0 ? $"{g.Values[i]}, given" : $"{g.Values[i]}";
        else if (g.Notes[i] != 0) s += "empty, notes " + string.Join(' ', Enumerable.Range(1, 9).Where(n => (g.Notes[i] >> n & 1) != 0));
        else s += "empty";
        if (conflicts[i]) s += ", conflicts with another number";
        if (flagged.Contains(i)) s += ", wrong";
        if (i == selected) s += ", selected";
        return s;
    }
}
