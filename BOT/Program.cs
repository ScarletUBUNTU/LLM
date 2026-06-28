using System;
using System.Collections.Generic;

namespace Bot
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var model = new MovementPatternModel();

            Console.WriteLine("Local BOT-NPC movement analyzer");
            Console.WriteLine("Enter a movement sequence (\"FORWARD\", \"BACKWARD\", \"LEFT\", \"RIGHT\", \"STAY\", \"DODGE\", \"JUMP\", \"HEAL\", \"USE_ITEM\", \"BLOCK\", \"ACTION\", \"ATTACK\"), type 'train <pattern-name> <sequence>' to teach a new pattern, type 'RND' to generate a random sequence, or type exit:");

            while (true)
            {
                Console.Write("Sequence> ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                var trimmedInput = input.Trim();
                if (trimmedInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (trimmedInput.StartsWith("train ", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = trimmedInput.Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 3)
                    {
                        Console.WriteLine("Usage: train <pattern-name> <sequence>");
                        continue;
                    }

                    var patternName = parts[1];
                    var sequenceText = parts[2];
                    var learnedSequence = MovementPatternModel.ParseSequence(sequenceText);

                    if (learnedSequence.Count == 0)
                    {
                        Console.WriteLine("No valid moves found. Try something like: train guard FORWARD RIGHT STAY LEFT BACKWARD");
                        continue;
                    }

                    model.LearnPattern(patternName, learnedSequence);
                    Console.WriteLine($"Learned pattern '{patternName}' from {learnedSequence.Count} moves.");
                    Console.WriteLine();
                    continue;
                }

                if (trimmedInput.Equals("RND", StringComparison.OrdinalIgnoreCase))
                {
                    for (var i = 1; i <= 20; i++)
                    {
                        var randomSequence = MovementPatternModel.GenerateRandomMovementSequence();
                        Console.WriteLine($"Random sequence #{i}:");
                        Console.WriteLine(string.Join(" ", randomSequence));

                        var (patternRND, confidenceRND) = model.PredictPattern(randomSequence);
                        var nextMove = model.PredictNextAction(randomSequence);
                        var reaction = MovementPatternModel.BuildReaction(nextMove);

                        Console.WriteLine($"Predicted pattern: {patternRND}");
                        Console.WriteLine($"Confidence: {confidenceRND:F2}");
                        Console.WriteLine($"Next action: {nextMove}");
                        Console.WriteLine($"Reaction: {reaction}");
                        Console.WriteLine();
                    }

                    continue;
                }

                var sequence = MovementPatternModel.ParseSequence(trimmedInput);
                var (pattern, confidence) = model.PredictPattern(sequence);
                var nextAction = model.PredictNextAction(sequence);
                var reactionText = MovementPatternModel.BuildReaction(nextAction);
                Console.WriteLine($"Predicted pattern: {pattern}");
                Console.WriteLine($"Confidence: {confidence:F2}");
                Console.WriteLine($"Next action: {nextAction}");
                Console.WriteLine($"Reaction: {reactionText}");
                Console.WriteLine();
            }
        }
    }
}
