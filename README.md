# VisibleSudoku

<img src="wwwroot/icon-192.png" alt="VisibleSudoku icon" width="96" align="right" />

Sudoku for people with poor eyesight: huge bold digits, high-contrast light and dark themes, large buttons,
four difficulty levels, and a layout that fits an iPhone screen without scrolling.

**Play it:** https://kreaton.github.io/VisibleSudoku/

## Run
    dotnet run            # needs the .NET 10 SDK
    dotnet publish -c Release -o out   # static files in out/wwwroot — host on any HTTPS static host

On iPhone: open the site in Safari → Share → Add to Home Screen for full-screen play.

## Difficulty
- Easy: 42+ clues; solvable with "naked singles" only
- Medium: ~34 clues; solvable with naked + hidden singles
- Hard: ~29 clues; unique solution
- Expert: ~24 clues; unique solution that is not solvable by singles alone

## Deployment
Every push to `main` builds the app and deploys it to GitHub Pages via
[.github/workflows/pages.yml](.github/workflows/pages.yml).

## License
[MIT](LICENSE)
