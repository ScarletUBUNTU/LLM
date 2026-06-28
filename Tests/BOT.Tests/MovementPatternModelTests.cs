using Bot;

namespace BOT.Tests;

public class MovementPatternModelTests
{
    [Fact]
    public void LearnPattern_AddsCustomPatternAndPredictsIt()
    {
        var model = new MovementPatternModel();

        model.LearnPattern("zigzag", new List<string> { "UP", "RIGHT", "DOWN", "LEFT", "UP" });

        var (pattern, confidence) = model.PredictPattern(new List<string> { "UP", "RIGHT", "DOWN", "LEFT", "UP" });

        Assert.Equal("zigzag", pattern);
        Assert.True(confidence > 0f);
    }
}
