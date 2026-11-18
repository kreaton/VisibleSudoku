# VisibleSudoku

[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/kreaton/VisibleSudoku?style=flat&logo=github&color=yellow)](https://github.com/kreaton/VisibleSudoku/stargazers)

⭐ If VisibleSudoku earns a place on your machine, [star it on GitHub](https://github.com/kreaton/VisibleSudoku) — it's free, made by one developer, and stars are how other people find it.

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

## License & Authorship

This project is distributed under the terms of the [MIT License](LICENSE).

**AI authorship note:** Most of the source code in this repository was generated with AI coding agents (Claude Code), under my direction and review. Purely AI-generated material may not be eligible for copyright protection in some jurisdictions. To the extent any part of this project is protected by copyright, it is licensed under the MIT License, and the license's warranty and liability disclaimers apply to the project as a whole.

<!-- notices:start -->
### Independent development

VisibleSudoku is an independent, personal project developed by Per Hejndorf. It is not affiliated with, endorsed by or sponsored by Microsoft or any other rights holder. Product names and logos are trademarks of their respective owners and are used only to identify the services and tools the project works with.

### Third-party notices and asset provenance

[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) lists every third-party library, font and online service the project uses, with version, licence, source and whether it was modified. It also records where the icons, images, audio and video came from and how they are licensed.
<!-- notices:end -->
