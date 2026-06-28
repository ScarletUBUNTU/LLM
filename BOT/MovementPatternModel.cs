using System;
using System.Collections.Generic;
using System.Linq;

//   "LIGHT1": "Attack",
//   "HEAVY1": "Heavy Attack",
//   "SPECIAL1": "Special Move 1",
//   "SPECIAL2": "Special Move 2",
//   "ABILITY1": "Use Ability 1",
//   "ABILITY2": "Use Ability 2",

namespace Bot
{
    public class MovementPatternModel
    {
        public static readonly string[] Actions = { "FORWARD", "BACKWARD", "LEFT", "RIGHT", "STAY", "DODGE", "JUMP", "HEAL", "USE_ITEM", "BLOCK", "ACTION", "ATTACK" };

        private static readonly Random Random = new Random();

        private readonly Dictionary<string, float[]> patternVectors = new Dictionary<string, float[]>();
        private readonly Dictionary<string, Dictionary<string, int>> transitionCounts = new Dictionary<string, Dictionary<string, int>>();

        private static readonly Dictionary<string, string[]> DefaultPatterns = new Dictionary<string, string[]>
        {
            { "move forward", new[] { "FORWARD", "FORWARD", "FORWARD" } },
            { "strafe right", new[] { "RIGHT", "RIGHT", "RIGHT" } },
            { "retreat", new[] { "BACKWARD", "BACKWARD", "BACKWARD" } },
            { "do nothing", new[] { "STAY", "STAY", "STAY" } },
            { "guard", new[] { "BLOCK", "BLOCK", "STAY" } },
            { "circle", new[] { "FORWARD", "RIGHT", "BACKWARD", "LEFT" } },
            { "ambush", new[] { "STAY", "STAY", "ATTACK" } },
            { "dodge", new[] { "DODGE", "DODGE", "STAY", } },
            { "use items", new[] { "STAY", "STAY", "USE_ITEM" } },
            { "jump", new[] { "JUMP", "JUMP", "JUMP" } },
            { "heal", new[] { "STAY", "STAY", "HEAL" } },
            { "attack", new[] { "ATTACK", "ATTACK", "ATTACK" } },
            { "use ability", new[] { "STAY", "STAY", "ACTION" } },
            { "dodge attack", new[] { "DODGE", "DODGE", "ATTACK" } },
            { "jump attack", new[] { "JUMP", "ATTACK", "ATTACK" } },
            { "heal and attack", new[] { "HEAL", "ATTACK", "HEAL" } },
            { "circle and attack", new[] { "FORWARD", "RIGHT", "ATTACK", "BACKWARD", "LEFT" } },
            { "ambush and heal", new[] { "STAY", "STAY", "ATTACK", "HEAL" } },
            { "dodge and use item", new[] { "DODGE", "USE_ITEM", "DODGE" } },
            { "jump and use ability", new[] { "JUMP", "ACTION", "JUMP" } },
            { "retreat and heal", new[] { "BACKWARD", "HEAL", "BACKWARD" } },
            { "strafe and attack", new[] { "RIGHT", "ATTACK", "LEFT" } },
            { "guard and dodge", new[] { "BLOCK", "DODGE", "BLOCK" } },
            { "circle and use item", new[] { "FORWARD", "USE_ITEM", "RIGHT", "BACKWARD", "LEFT" } },
            { "ambush and use ability", new[] { "STAY", "ACTION", "STAY" } },
            { "dodge and heal", new[] { "DODGE", "HEAL", "DODGE" } },
            { "jump and attack", new[] { "JUMP", "ATTACK", "JUMP" } },
            { "heal and use item", new[] { "HEAL", "USE_ITEM", "HEAL" } },
            { "circle and dodge", new[] { "FORWARD", "DODGE", "RIGHT", "BACKWARD", "LEFT" } },
            { "ambush and jump", new[] { "STAY", "JUMP", "STAY" } },
            { "dodge and attack", new[] { "DODGE", "ATTACK", "DODGE" } },
            { "jump and heal", new[] { "JUMP", "HEAL", "JUMP" } },
            { "heal and use ability", new[] { "HEAL", "ACTION", "HEAL" } },
            { "counteract", new[] { "BLOCK", "ATTACK", } }
        };

        public MovementPatternModel()
        {
            TrainDefaultModel();
        }

        public void TrainDefaultModel(int samplesPerPattern = 40)
        {
            patternVectors.Clear();
            foreach (var kvp in DefaultPatterns)
            {
                for (var sampleIndex = 0; sampleIndex < samplesPerPattern; sampleIndex++)
                {
                    var sequence = GenerateNoisySequence(kvp.Value, sampleIndex);
                    AddTrainingSequence(kvp.Key, sequence);
                }
            }

            FinalizeTraining();
        }

        public void LearnPattern(string patternName, IEnumerable<string> sequence)
        {
            if (string.IsNullOrWhiteSpace(patternName))
            {
                throw new ArgumentException("Pattern name cannot be empty.", nameof(patternName));
            }

            var normalizedPatternName = patternName.Trim();
            var normalizedSequence = sequence
                .Select(action => action.Trim().ToUpperInvariant())
                .Where(action => Actions.Contains(action))
                .ToList();

            if (normalizedSequence.Count == 0)
            {
                return;
            }

            AddTrainingSequence(normalizedPatternName, normalizedSequence);
            FinalizeTraining();
        }

        public void LearnPattern(string patternName, string rawText)
        {
            LearnPattern(patternName, ParseSequence(rawText));
        }

        public void AddTrainingSequence(string pattern, List<string> sequence)
        {
            if (!patternVectors.ContainsKey(pattern))
            {
                patternVectors[pattern] = new float[Actions.Length];
            }

            var vector = patternVectors[pattern];
            foreach (var action in sequence)
            {
                var normalizedAction = action.Trim().ToUpperInvariant();
                var index = Array.IndexOf(Actions, normalizedAction);
                if (index >= 0)
                {
                    vector[index] += 1f;
                }
            }

            AddTransitionCounts(sequence);
        }

        public void FinalizeTraining()
        {
            foreach (var key in patternVectors.Keys.ToList())
            {
                var vector = patternVectors[key];
                var magnitude = MathF.Sqrt(vector.Sum(value => value * value));
                if (magnitude > 0)
                {
                    for (var i = 0; i < vector.Length; i++)
                    {
                        vector[i] /= magnitude;
                    }
                }
            }
        }

        public (string pattern, float confidence) PredictPattern(List<string> sequence)
        {
            if (patternVectors.Count == 0)
            {
                return ("unknown", 0f);
            }

            var queryVector = new float[Actions.Length];
            foreach (var action in sequence)
            {
                var normalizedAction = action.Trim().ToUpperInvariant();
                var index = Array.IndexOf(Actions, normalizedAction);
                if (index >= 0)
                {
                    queryVector[index] += 1f;
                }
            }

            var magnitude = MathF.Sqrt(queryVector.Sum(value => value * value));
            if (magnitude == 0f)
            {
                return ("unknown", 0f);
            }

            for (var i = 0; i < queryVector.Length; i++)
            {
                queryVector[i] /= magnitude;
            }

            string bestPattern = "unknown";
            var bestScore = 0f;
            foreach (var kvp in patternVectors)
            {
                var candidate = kvp.Key;
                var vector = kvp.Value;
                var score = 0f;
                for (var i = 0; i < vector.Length; i++)
                {
                    score += vector[i] * queryVector[i];
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPattern = candidate;
                }
            }

            return (bestPattern, Math.Clamp(bestScore, 0f, 1f));
        }

        public string PredictNextAction(List<string> sequence)
        {
            if (sequence == null || sequence.Count == 0)
            {
                return "STAY";
            }

            var lastAction = sequence.Last().Trim().ToUpperInvariant();
            if (!Actions.Contains(lastAction))
            {
                return "STAY";
            }

            if (transitionCounts.TryGetValue(lastAction, out var nextCounts) && nextCounts.Count > 0)
            {
                return nextCounts.OrderByDescending(kv => kv.Value).First().Key;
            }

            var fallback = transitionCounts
                .SelectMany(kvp => kvp.Value)
                .GroupBy(kv => kv.Key)
                .OrderByDescending(g => g.Sum(kv => kv.Value))
                .Select(g => g.Key)
                .FirstOrDefault();

            return fallback ?? "STAY";
        }

        public static string BuildReaction(string nextAction)
        {
            switch (nextAction)
            {
                case "ATTACK":
                    return "Prepare to block or counterattack.";
                case "DODGE":
                    return "Evade the incoming threat.";
                case "HEAL":
                    return "Recover health and stay defensive.";
                case "USE_ITEM":
                    return "Use an item now.";
                case "ACTION":
                    return "Execute a special ability.";
                case "BLOCK":
                    return "Hold your guard.";
                case "STAY":
                    return "Hold position.";
                case "FORWARD":
                case "BACKWARD":
                case "LEFT":
                case "RIGHT":
                    return $"Move {nextAction.ToLowerInvariant()}.";
                case "JUMP":
                    return "Jump to avoid danger.";
                default:
                    return "Continue with the current plan.";
            }
        }

        private void AddTransitionCounts(List<string> sequence)
        {
            for (var i = 0; i + 1 < sequence.Count; i++)
            {
                var current = sequence[i].Trim().ToUpperInvariant();
                var next = sequence[i + 1].Trim().ToUpperInvariant();
                if (!Actions.Contains(current) || !Actions.Contains(next))
                {
                    continue;
                }

                if (!transitionCounts.ContainsKey(current))
                {
                    transitionCounts[current] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                }

                if (!transitionCounts[current].ContainsKey(next))
                {
                    transitionCounts[current][next] = 0;
                }

                transitionCounts[current][next]++;
            }
        }

        public static List<string> ParseSequence(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return new List<string>();
            }

            var tokens = rawText.Split(new[] { ' ', ',', ';', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            return tokens.Select(token => token.Trim().ToUpperInvariant()).Where(token => Actions.Contains(token)).ToList();
        }

        public static List<string> GenerateRandomMovementSequence(int maxLength = 100)
        {
            if (maxLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLength), "maxLength must be greater than zero.");
            }

            var length = Random.Next(1, Math.Min(maxLength, 10) + 1);
            var sequence = new List<string>(length + 1);
            for (var i = 0; i < length; i++)
            {
                sequence.Add(Actions[Random.Next(Actions.Length)]);
            }

            sequence.Add("END");
            return sequence;
        }

        private static List<string> GenerateNoisySequence(string[] basePattern, int sampleIndex)
        {
            var sequence = new List<string>(basePattern);
            var random = new Random(sampleIndex * 31 + basePattern.Length);
            for (var i = 0; i < sequence.Count; i++)
            {
                if (random.NextDouble() < 0.15)
                {
                    sequence[i] = Actions[random.Next(Actions.Length)];
                }
            }

            if (random.NextDouble() < 0.2)
            {
                sequence.Add(Actions[random.Next(Actions.Length)]);
            }

            return sequence;
        }
    }
}
