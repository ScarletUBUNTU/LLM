using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LLM.NPCConversation
{
    public class SkillDirector : MonoBehaviour
    {
        [Header("Questionnaire")]
        public List<PsychologicalQuestion> allQuestions = new List<PsychologicalQuestion>();
        public int minQuestions = 12;
        public int maxQuestions = 15;

        [Header("Skill Pools")]
        public List<BehavioralSkill> goodSkillPool = new List<BehavioralSkill>();
        public List<BehavioralSkill> badSkillPool = new List<BehavioralSkill>();

        [Header("Trait Pools")]
        public List<NPCTrait> goodTraitPool = new List<NPCTrait>();
        public List<NPCTrait> badTraitPool = new List<NPCTrait>();

        [Header("Default NPC Stats")]
        public List<NPCStat> defaultStats = new List<NPCStat>();

        private void OnValidate()
        {
            if (allQuestions == null || allQuestions.Count == 0)
            {
                BuildDefaultQuestionPool();
            }

            if (goodTraitPool == null || goodTraitPool.Count == 0 || badTraitPool == null || badTraitPool.Count == 0)
            {
                BuildDefaultTraitPools();
            }

            if (defaultStats == null || defaultStats.Count == 0)
            {
                BuildDefaultStats();
            }
        }

        public List<PsychologicalQuestion> BuildSurvey()
        {
            var survey = new List<PsychologicalQuestion>(allQuestions);
            Shuffle(survey);
            var count = Mathf.Clamp(UnityEngine.Random.Range(minQuestions, maxQuestions + 1), 1, survey.Count);
            return survey.GetRange(0, count);
        }

        public SkillAnalysisResult AnalyzeResponses(List<QuestionResponse> responses)
        {
            var skillScores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var traitScores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var statAdjustments = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var notes = new List<string>();

            foreach (var response in responses)
            {
                if (response.questionIndex < 0 || response.questionIndex >= allQuestions.Count)
                {
                    continue;
                }

                var question = allQuestions[response.questionIndex];
                var clampedAnswer = Mathf.Clamp(response.answerValue, 1, 5);

                if (clampedAnswer >= 4)
                {
                    var intensity = clampedAnswer - 3;
                    ApplyInfluences(question.agreeInfluences, intensity, skillScores, traitScores, statAdjustments, notes, true);
                }
                else if (clampedAnswer <= 2)
                {
                    var intensity = 3 - clampedAnswer;
                    ApplyInfluences(question.disagreeInfluences, intensity, skillScores, traitScores, statAdjustments, notes, false);
                }
            }

            var result = new SkillAnalysisResult
            {
                goodSkills = PickSkills(skillScores, goodSkillPool, 3),
                badSkills = PickSkills(skillScores, badSkillPool, 2),
                positiveTraits = PickTraits(traitScores, goodTraitPool, 3),
                negativeTraits = PickTraits(traitScores, badTraitPool, 2),
                statChanges = BuildStatChanges(statAdjustments)
            };

            foreach (var skill in result.goodSkills.Concat(result.badSkills))
            {
                if (skill.statBonuses != null)
                {
                    foreach (var bonus in skill.statBonuses)
                    {
                        result.statChanges.Add(new StatBonus
                        {
                            statName = bonus.statName,
                            amount = bonus.amount
                        });
                    }
                }
            }

            result.notes = notes;
            return result;
        }

        public void ApplyOutcomeToProfile(NPCProfile profile, SkillAnalysisResult outcome)
        {
            if (profile == null || outcome == null)
            {
                return;
            }

            profile.assignedSkills.Clear();
            profile.assignedSkills.AddRange(outcome.goodSkills);
            profile.assignedSkills.AddRange(outcome.badSkills);
            profile.assignedTraits.Clear();
            profile.assignedTraits.AddRange(outcome.positiveTraits);
            profile.assignedTraits.AddRange(outcome.negativeTraits);
            profile.ApplyStatChanges(outcome.statChanges);
        }

        private void ApplyInfluences(
            List<QuestionInfluence> influences,
            int intensity,
            Dictionary<string, int> skillScores,
            Dictionary<string, int> traitScores,
            Dictionary<string, int> statAdjustments,
            List<string> notes,
            bool isAgree)
        {
            if (influences == null)
            {
                return;
            }

            foreach (var influence in influences)
            {
                if (!string.IsNullOrEmpty(influence.skillName))
                {
                    if (!skillScores.ContainsKey(influence.skillName))
                    {
                        skillScores[influence.skillName] = 0;
                    }

                    skillScores[influence.skillName] += influence.weight * intensity;
                }

                if (!string.IsNullOrEmpty(influence.traitName))
                {
                    if (!traitScores.ContainsKey(influence.traitName))
                    {
                        traitScores[influence.traitName] = 0;
                    }

                    traitScores[influence.traitName] += influence.traitWeight * intensity;
                }

                if (influence.statBonuses != null)
                {
                    foreach (var bonus in influence.statBonuses)
                    {
                        if (!statAdjustments.ContainsKey(bonus.statName))
                        {
                            statAdjustments[bonus.statName] = 0;
                        }

                        statAdjustments[bonus.statName] += bonus.amount * intensity;
                    }
                }

                var influencedName = !string.IsNullOrEmpty(influence.traitName)
                    ? influence.traitName
                    : influence.skillName;
                notes.Add($"{(isAgree ? "Agree" : "Disagree")} to '{influencedName}' added {influence.weight * intensity} skill/trait points and {influence.statBonuses?.Count ?? 0} stat deltas.");
            }
        }

        private List<BehavioralSkill> PickSkills(Dictionary<string, int> scores, List<BehavioralSkill> pool, int count)
        {
            if (pool == null || pool.Count == 0)
            {
                return new List<BehavioralSkill>();
            }

            var sorted = pool
                .OrderByDescending(skill => scores.TryGetValue(skill.skillName, out var value) ? value : 0)
                .ThenBy(skill => !skill.isGood)
                .ToList();

            return sorted.Take(Mathf.Min(count, sorted.Count)).ToList();
        }

        private List<NPCTrait> PickTraits(Dictionary<string, int> scores, List<NPCTrait> pool, int count)
        {
            if (pool == null || pool.Count == 0)
            {
                return new List<NPCTrait>();
            }

            var sorted = pool
                .OrderByDescending(trait => scores.TryGetValue(trait.traitName, out var value) ? value : 0)
                .ThenBy(trait => !trait.isNegative)
                .ToList();

            return sorted.Take(Mathf.Min(count, sorted.Count)).ToList();
        }

        private List<StatBonus> BuildStatChanges(Dictionary<string, int> statAdjustments)
        {
            return statAdjustments
                .Select(pair => new StatBonus { statName = pair.Key, amount = pair.Value })
                .ToList();
        }

        private void Shuffle<T>(List<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                var temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        private void BuildDefaultStats()
        {
            defaultStats = new List<NPCStat>
            {
                new NPCStat { statName = "Strength", value = 5 },
                new NPCStat { statName = "Empathy", value = 5 },
                new NPCStat { statName = "Intellect", value = 5 },
                new NPCStat { statName = "Willpower", value = 5 },
                new NPCStat { statName = "Charisma", value = 5 }
            };
        }

        private void BuildDefaultTraitPools()
        {
            goodTraitPool = new List<NPCTrait>
            {
                new NPCTrait { traitName = "Brave", description = "Shows courage in difficult situations.", intensity = 0.75f, isNegative = false },
                new NPCTrait { traitName = "Compassionate", description = "Feels empathy for others and helps when needed.", intensity = 0.75f, isNegative = false },
                new NPCTrait { traitName = "Disciplined", description = "Stays focused and follows through on plans.", intensity = 0.75f, isNegative = false },
                new NPCTrait { traitName = "Curious", description = "Seeks out new information and ideas.", intensity = 0.75f, isNegative = false },
                new NPCTrait { traitName = "Dependable", description = "Others can count on this NPC to keep promises.", intensity = 0.75f, isNegative = false }
            };

            badTraitPool = new List<NPCTrait>
            {
                new NPCTrait { traitName = "Impulsive", description = "Acts before thinking through consequences.", intensity = 0.75f, isNegative = true },
                new NPCTrait { traitName = "Stubborn", description = "Resists changing opinions even when presented with new evidence.", intensity = 0.75f, isNegative = true },
                new NPCTrait { traitName = "Aloof", description = "Keeps a distance from others and avoids emotional closeness.", intensity = 0.75f, isNegative = true },
                new NPCTrait { traitName = "Cynical", description = "Doubtful of others' motives and intentions.", intensity = 0.75f, isNegative = true },
                new NPCTrait { traitName = "Pessimistic", description = "Often expects the worst outcomes.", intensity = 0.75f, isNegative = true }
            };
        }

        private void BuildDefaultQuestionPool()
        {
            allQuestions = new List<PsychologicalQuestion>();
            var prompts = new[]
            {
                "I enjoy taking charge when a group needs direction.",
                "I often pay attention to other people’s feelings.",
                "I prefer plans with clear structure rather than unpredictable outcomes.",
                "I feel comfortable expressing my opinion even when it differs from the majority.",
                "I trust my instincts when making decisions.",
                "I enjoy solving puzzles and thinking through complicated problems.",
                "I find it easy to put myself in someone else’s shoes.",
                "I remain calm when plans suddenly change.",
                "I am willing to take risks if the reward is meaningful.",
                "I keep my promises even if it costs me personally.",
                "I like to protect weaker people when they are threatened.",
                "I often question rules that I believe are unfair.",
                "I quickly adapt when I enter a new environment.",
                "I usually prefer to listen before I speak.",
                "I feel energized when I am praised for my contributions.",
                "I am willing to break social norms if I think they are wrong.",
                "I feel guilty if I hurt someone’s feelings by accident.",
                "I can stay focused even under pressure.",
                "I enjoy helping others solve their problems.",
                "I am comfortable making hard choices even if they upset people.",
                "I find it hard to stay still when there is work to be done.",
                "I think carefully before sharing personal information.",
                "I value honesty over harmony.",
                "I like to explore new ideas and learn from them.",
                "I prefer familiar routines to constant change.",
                "I like to build trust by being dependable.",
                "I sometimes act first and think later.",
                "I often notice small details that others miss.",
                "I enjoy debating difficult topics.",
                "I believe loyalty to my group is very important.",
                "I find it easier to forgive someone than hold a grudge.",
                "I am energized by meeting new people.",
                "I think people should be rewarded based on effort.",
                "I feel confident when I speak up for what I believe in.",
                "I prefer to work with a clear set of rules.",
                "I often help others without expecting anything in return.",
                "I find it hard to change my mind once I have made a decision.",
                "I ask questions when I don’t understand something.",
                "I feel uneasy when I see injustice and don’t act.",
                "I tend to take responsibility for my mistakes.",
                "I enjoy being in environments where different opinions are welcome.",
                "I often try to protect my own interests first.",
                "I like to plan ahead for all possible outcomes.",
                "I am quick to apologize when I have hurt someone.",
                "I feel strong when I achieve a goal through effort.",
                "I believe strength comes from discipline and self-control.",
                "I am willing to challenge authority to defend what is right.",
                "I can stay patient even when progress is slow.",
                "I feel responsible for the well-being of people around me.",
                "I find it satisfying to improve on my weaknesses."
            };

            for (var i = 0; i < prompts.Length; i++)
            {
                var prompt = prompts[i];
                var agreeInfluence = new List<QuestionInfluence>();
                var disagreeInfluence = new List<QuestionInfluence>();

                switch (i / 10)
                {
                    case 0:
                        agreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Leadership",
                            weight = 2,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Charisma", amount = 1 } }
                        });
                        disagreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Caution",
                            weight = 1,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Willpower", amount = 1 } }
                        });
                        break;
                    case 1:
                        agreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Empathy",
                            weight = 2,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Empathy", amount = 2 } }
                        });
                        disagreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Coldness",
                            weight = 1,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Intellect", amount = 1 } }
                        });
                        break;
                    case 2:
                        agreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Discipline",
                            weight = 2,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Willpower", amount = 2 } }
                        });
                        disagreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Recklessness",
                            weight = 1,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Strength", amount = 1 } }
                        });
                        break;
                    case 3:
                        agreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Assertiveness",
                            weight = 2,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Charisma", amount = 1 } }
                        });
                        disagreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Deference",
                            weight = 1,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Empathy", amount = 1 } }
                        });
                        break;
                    case 4:
                        agreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Confidence",
                            weight = 2,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Intellect", amount = 1 } }
                        });
                        disagreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Hesitation",
                            weight = 1,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Willpower", amount = 1 } }
                        });
                        break;
                    case 5:
                        agreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Curiosity",
                            weight = 2,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Intellect", amount = 2 } }
                        });
                        disagreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "ClosedMind",
                            weight = 1,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Strength", amount = 1 } }
                        });
                        break;
                    case 6:
                        agreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Compassion",
                            weight = 2,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Empathy", amount = 2 } }
                        });
                        disagreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Indifference",
                            weight = 1,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Charisma", amount = 1 } }
                        });
                        break;
                    case 7:
                        agreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Resilience",
                            weight = 2,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Willpower", amount = 2 } }
                        });
                        disagreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Fragility",
                            weight = 1,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Empathy", amount = 1 } }
                        });
                        break;
                    case 8:
                        agreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "RiskTaking",
                            weight = 2,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Strength", amount = 1 } }
                        });
                        disagreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Prudence",
                            weight = 1,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Willpower", amount = 1 } }
                        });
                        break;
                    default:
                        agreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "Integrity",
                            weight = 2,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Charisma", amount = 1 } }
                        });
                        disagreeInfluence.Add(new QuestionInfluence
                        {
                            skillName = "SelfInterest",
                            weight = 1,
                            statBonuses = new List<StatBonus> { new StatBonus { statName = "Intellect", amount = 1 } }
                        });
                        break;
                }

                allQuestions.Add(new PsychologicalQuestion
                {
                    questionText = prompt,
                    agreeInfluences = agreeInfluence,
                    disagreeInfluences = disagreeInfluence
                });
            }
        }
    }

    [Serializable]
    public class NPCStat
    {
        public string statName = "Stat";
        public int value = 0;

        public override string ToString()
        {
            return $"{statName}: {value}";
        }
    }

    [Serializable]
    public class PsychologicalQuestion
    {
        [TextArea(2, 4)]
        public string questionText;
        public List<QuestionInfluence> agreeInfluences = new List<QuestionInfluence>();
        public List<QuestionInfluence> disagreeInfluences = new List<QuestionInfluence>();
    }

    [Serializable]
    public class QuestionInfluence
    {
        public string skillName = "Skill";
        public int weight = 1;
        public string traitName = "";
        public int traitWeight = 1;
        public List<StatBonus> statBonuses = new List<StatBonus>();
    }

    [Serializable]
    public class QuestionResponse
    {
        public int questionIndex;
        public int answerValue;
    }

    [Serializable]
    public class SkillAnalysisResult
    {
        public List<BehavioralSkill> goodSkills = new List<BehavioralSkill>();
        public List<BehavioralSkill> badSkills = new List<BehavioralSkill>();
        public List<NPCTrait> positiveTraits = new List<NPCTrait>();
        public List<NPCTrait> negativeTraits = new List<NPCTrait>();
        public List<StatBonus> statChanges = new List<StatBonus>();
        public List<string> notes = new List<string>();
    }
}
