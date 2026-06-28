# Bot Local SLM (C#)

This folder now contains a local C# movement-pattern analyzer for BOT-NPC behavior.
It is designed to run without any Python or OpenAI dependency.

## Files

- `Bot.csproj` — C# project file.
- `Program.cs` — Console app entry point.
- `MovementPatternModel.cs` — Local movement model and prediction logic.

## Usage

1. Open PowerShell in `c:\Users\UBUNTU\Documents\LLM model for games\LLM\BOT`.
2. Run:

```powershell
dotnet run --project Bot.csproj
```

3. Enter a movement sequence such as:

```
UP RIGHT DOWN LEFT STAY CIRCLE
```

4. To teach the bot a new custom pattern, use:

```text
train guard UP RIGHT STAY LEFT DOWN
```

5. You can then test that pattern again by typing a matching sequence, or type `exit` to quit.

## Notes

- No OpenAI API key is required.
- Python files in this folder are legacy artifacts and should not be used.
- Extend `MovementPatternModel.cs` to add new movement patterns or NPC behavior.
