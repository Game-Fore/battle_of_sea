# battle_of_sea

Quick run (development):

- `dotnet run --project BattleOfSea.csproj -c Debug`

Demo mode:
- By default the app runs in **demo mode** (no backend required) and will seed example rooms and open a demo dialog on startup.
- To disable demo mode when running locally set `BATTLEOFSEA_DEMO=0` in your environment before running.

Note: if your machine does not have .NET 7 installed, this project now targets `net9.0` so `dotnet run` should work if you have .NET 9 installed.
