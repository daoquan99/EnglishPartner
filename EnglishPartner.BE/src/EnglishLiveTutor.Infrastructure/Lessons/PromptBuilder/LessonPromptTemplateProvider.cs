using System.Text;
using EnglishLiveTutor.Domain.Lessons.Enums;

namespace EnglishLiveTutor.Infrastructure.Lessons.PromptBuilder;

public interface ILessonPromptTemplateProvider
{
    string TemplateVersion { get; }
    string BuildExerciseTypeRules(IReadOnlyList<string> types, int targetCount);
    string BuildExerciseTypeSpecifications(IReadOnlyList<string> types);
    string BuildConcreteExerciseExamples(IReadOnlyList<string> types, string topic, string cefrLevel);
}

public sealed class LessonPromptTemplateProvider : ILessonPromptTemplateProvider
{
    public string TemplateVersion => "1.0";

    public string BuildExerciseTypeRules(IReadOnlyList<string> types, int targetCount)
    {
        var sb = new StringBuilder();
        var uniqueTypes = types.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        string allowedTypesStr = uniqueTypes.Count > 0 ? string.Join(", ", uniqueTypes) : "the requested types";

        sb.AppendLine($"1. Every exercise MUST have exact PascalCase 'type' chosen strictly from [{allowedTypesStr}], sequential 'order' starting from 1 to {targetCount}, unique alphanumeric 'key' (e.g. 'ex_01_item'), clear 'title', 'instructions', 'maxPoints' (0 for Flashcards, 10 for scored exercises), 'gradingMode', 'content' object, and 'grading' object.");

        int ruleNum = 2;
        foreach (var type in uniqueTypes)
        {
            switch (type.ToLowerInvariant())
            {
                case "sentenceunscramble":
                    sb.AppendLine($"{ruleNum}. For SentenceUnscramble: 'content.tokens' MUST be an array of objects with 'id' (e.g. 't-1') and 'text'. 'grading.validSequences' MUST be an array of string arrays, where each inner array lists token IDs in the grammatically correct word order.");
                    ruleNum++;
                    break;
                case "multiplechoice":
                    sb.AppendLine($"{ruleNum}. For MultipleChoice: 'content.options' MUST have 'id' and 'text'. 'grading.correctOptionIds' MUST list the correct option ID(s).");
                    ruleNum++;
                    break;
                case "fillintheblanks":
                    sb.AppendLine($"{ruleNum}. For FillInTheBlanks: 'content.textWithBlanks' MUST contain markers like [blank-1], [blank-2]. 'content.blanks' defines each blank with 'id' and 'hint'. 'grading.blankAnswers' defines 'acceptedAnswers' for each blank id.");
                    ruleNum++;
                    break;
                case "flashcards":
                    sb.AppendLine($"{ruleNum}. For Flashcards: 'content.cards' MUST be an array of items with 'id', 'term', 'ipa', 'meaningVi', 'exampleEn', and 'exampleVi'. 'maxPoints' MUST be 0 and 'gradingMode' MUST be 'SelfAssessment'.");
                    ruleNum++;
                    break;
                case "matching":
                    sb.AppendLine($"{ruleNum}. For Matching: 'content.leftItems' and 'content.rightItems' with 'id' and 'text'. 'grading.correctPairs' with 'leftId' and 'rightId'.");
                    ruleNum++;
                    break;
                case "translation":
                    sb.AppendLine($"{ruleNum}. For Translation: 'content.direction' ('ViToEn' or 'EnToVi'), 'content.sourceText', 'grading.rubricCode' ('LESSON_TRANSLATION'), 'grading.referenceTranslations'.");
                    ruleNum++;
                    break;
                case "truefalse":
                    sb.AppendLine($"{ruleNum}. For TrueFalse: 'content.context', 'content.statements' with 'id' and 'text'. 'grading.statementAnswers' mapping statement ID to true/false boolean.");
                    ruleNum++;
                    break;
                case "errorcorrection":
                    sb.AppendLine($"{ruleNum}. For ErrorCorrection: 'content.incorrectSentence', 'content.instructions', 'grading.referenceCorrections', 'grading.targetErrors'.");
                    ruleNum++;
                    break;
                case "spellingpractice":
                    sb.AppendLine($"{ruleNum}. For SpellingPractice: 'content.prompt' (in Vietnamese), 'content.definition' (in Vietnamese), 'content.exampleSentence' with blank '________', 'content.hint' (in Vietnamese), 'grading.targetWord', and 'grading.acceptedSpellings'.");
                    ruleNum++;
                    break;
                case "speakingpractice":
                    sb.AppendLine($"{ruleNum}. For SpeakingPractice: 'content.topic', 'content.instructions', 'content.targetDurationSeconds', 'content.suggestedPrompts', 'grading.rubricCode' ('LESSON_SPEAKING').");
                    ruleNum++;
                    break;
                case "readingcomprehension":
                    sb.AppendLine($"{ruleNum}. For ReadingComprehension: 'content.passage' and 'content.subQuestions' array with 'id', 'questionKind', 'order', 'questionText', 'options', and 'grading'.");
                    ruleNum++;
                    break;
                case "listeningcomprehension":
                    sb.AppendLine($"{ruleNum}. For ListeningComprehension: 'content.transcript' and 'content.subQuestions' array with 'id', 'questionKind', 'order', 'questionText', 'options', and 'grading'.");
                    ruleNum++;
                    break;
                case "dictation":
                    sb.AppendLine($"{ruleNum}. For Dictation: 'content.hint' and 'grading.referenceTranscript', 'grading.caseSensitive', 'grading.ignorePunctuation'.");
                    ruleNum++;
                    break;
                case "shadowing":
                    sb.AppendLine($"{ruleNum}. For Shadowing: 'content.transcript' to repeat aloud, 'content.speechRateWpm' (e.g. 110-140), 'grading.rubricCode' ('LESSON_SHADOWING'), 'grading.rubricVersion' (1).");
                    ruleNum++;
                    break;
                case "conversationroleplay":
                    sb.AppendLine($"{ruleNum}. For ConversationRolePlay: 'content.scenario', 'content.aiRole', 'content.learnerRole', 'content.initialGreeting', 'content.minTurns', 'content.maxTurns', 'grading.rubricCode' ('LESSON_CONVERSATION_TEXT'), 'grading.rubricVersion' (1).");
                    ruleNum++;
                    break;
                case "writingpractice":
                    sb.AppendLine($"{ruleNum}. For WritingPractice: 'content.prompt', 'content.minWords', 'content.maxWords', 'grading.rubricCode' ('LESSON_WRITING'), 'grading.guidelines'.");
                    ruleNum++;
                    break;
            }
        }

        return sb.ToString().TrimEnd();
    }

    public string BuildExerciseTypeSpecifications(IReadOnlyList<string> types)
    {
        var sb = new StringBuilder();
        var uniqueTypes = types.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var type in uniqueTypes)
        {
            switch (type.ToLowerInvariant())
            {
                case "sentenceunscramble":
                    sb.AppendLine(@"### SentenceUnscramble:
- type: ""SentenceUnscramble""
- maxPoints: 10
- gradingMode: ""Deterministic""
- content: {
    ""prompt"": ""Rearrange tokens into a grammatically correct sentence:"",
    ""tokens"": [
      { ""id"": ""t-1"", ""text"": ""word1"" },
      { ""id"": ""t-2"", ""text"": ""word2"" },
      { ""id"": ""t-3"", ""text"": ""word3"" }
    ]
  }
- grading: {
    ""validSequences"": [
      [""t-1"", ""t-2"", ""t-3""]
    ],
    ""explanation"": ""Grammar explanation...""
  }");
                    break;

                case "multiplechoice":
                    sb.AppendLine(@"### MultipleChoice:
- type: ""MultipleChoice""
- maxPoints: 10
- gradingMode: ""Deterministic""
- content: {
    ""question"": ""Question statement or fill-in prompt?"",
    ""selectionMode"": ""Single"", // or ""Multiple""
    ""options"": [
      { ""id"": ""opt-a"", ""text"": ""Option A"" },
      { ""id"": ""opt-b"", ""text"": ""Option B"" },
      { ""id"": ""opt-c"", ""text"": ""Option C"" },
      { ""id"": ""opt-d"", ""text"": ""Option D"" }
    ]
  }
- grading: {
    ""correctOptionIds"": [""opt-b""],
    ""explanation"": ""Why this option is correct...""
  }");
                    break;

                case "fillintheblanks":
                    sb.AppendLine(@"### FillInTheBlanks:
- type: ""FillInTheBlanks""
- maxPoints: 10
- gradingMode: ""Deterministic""
- content: {
    ""textWithBlanks"": ""Text containing [blank-1] and [blank-2]."",
    ""blanks"": [
      { ""id"": ""blank-1"", ""hint"": ""first clue"", ""maxLength"": 20 },
      { ""id"": ""blank-2"", ""hint"": ""second clue"", ""maxLength"": 20 }
    ]
  }
- grading: {
    ""isOpenEnded"": false,
    ""blankAnswers"": {
      ""blank-1"": { ""acceptedAnswers"": [""word1"", ""variant1""], ""caseSensitive"": false },
      ""blank-2"": { ""acceptedAnswers"": [""word2""], ""caseSensitive"": false }
    },
    ""explanation"": ""Vocabulary and grammar notes...""
  }");
                    break;

                case "flashcards":
                    sb.AppendLine(@"### Flashcards:
- type: ""Flashcards""
- maxPoints: 0
- gradingMode: ""SelfAssessment""
- content: {
    ""cards"": [
      {
        ""id"": ""c-1"",
        ""term"": ""English word"",
        ""ipa"": ""/phonetics/"",
        ""meaningVi"": ""Nghĩa tiếng Việt"",
        ""exampleEn"": ""Example sentence in English."",
        ""exampleVi"": ""Câu ví dụ dịch sang tiếng Việt."",
        ""audioAssetId"": null
      }
    ]
  }
- grading: {}");
                    break;

                case "matching":
                    sb.AppendLine(@"### Matching:
- type: ""Matching""
- maxPoints: 10
- gradingMode: ""Deterministic""
- content: {
    ""leftItems"": [
      { ""id"": ""l-1"", ""text"": ""Phrase 1"" },
      { ""id"": ""l-2"", ""text"": ""Phrase 2"" }
    ],
    ""rightItems"": [
      { ""id"": ""r-1"", ""text"": ""Meaning 1"" },
      { ""id"": ""r-2"", ""text"": ""Meaning 2"" }
    ]
  }
- grading: {
    ""correctPairs"": [
      { ""leftId"": ""l-1"", ""rightId"": ""r-1"" },
      { ""leftId"": ""l-2"", ""rightId"": ""r-2"" }
    ],
    ""explanation"": ""Matching explanation...""
  }");
                    break;

                case "translation":
                    sb.AppendLine(@"### Translation:
- type: ""Translation""
- maxPoints: 10
- gradingMode: ""AiRubric""
- content: {
    ""direction"": ""ViToEn"",
    ""sourceText"": ""Câu tiếng Việt cần dịch..."",
    ""context"": ""Context description""
  }
- grading: {
    ""rubricCode"": ""LESSON_TRANSLATION"",
    ""rubricVersion"": 1,
    ""referenceTranslations"": [
      ""Natural English translation option 1"",
      ""Natural English translation option 2""
    ]
  }");
                    break;

                case "truefalse":
                    sb.AppendLine(@"### TrueFalse:
- type: ""TrueFalse""
- maxPoints: 10
- gradingMode: ""Deterministic""
- content: {
    ""context"": ""Reading context paragraph..."",
    ""statements"": [
      { ""id"": ""s-1"", ""text"": ""Statement one."" },
      { ""id"": ""s-2"", ""text"": ""Statement two."" }
    ]
  }
- grading: {
    ""statementAnswers"": {
      ""s-1"": true,
      ""s-2"": false
    }
  }");
                    break;

                case "errorcorrection":
                    sb.AppendLine(@"### ErrorCorrection:
- type: ""ErrorCorrection""
- maxPoints: 10
- gradingMode: ""Deterministic""
- content: {
    ""incorrectSentence"": ""Sentence containing a specific mistake."",
    ""instructions"": ""Identify and correct the grammar error.""
  }
- grading: {
    ""referenceCorrections"": [
      ""Corrected sentence option 1."",
      ""Corrected sentence option 2.""
    ],
    ""targetErrors"": [""Description of target error""]
  }");
                    break;

                case "spellingpractice":
                    sb.AppendLine(@"### SpellingPractice:
- type: ""SpellingPractice""
- maxPoints: 10
- gradingMode: ""Deterministic""
- content: {
    ""prompt"": ""Đọc gợi ý và viết từ tiếng Anh phù hợp."",
    ""definition"": ""Bản kế hoạch các hoạt động và thời gian chúng sẽ diễn ra."",
    ""exampleSentence"": ""I check my work ________ every morning at 8:30 AM."",
    ""hint"": ""Bắt đầu bằng chữ 's' và có 8 chữ cái (s _ _ _ _ _ _ e)."",
    ""letterCount"": 8
  }
- grading: {
    ""targetWord"": ""schedule"",
    ""acceptedSpellings"": [""schedule""],
    ""caseSensitive"": false
  }");
                    break;

                case "speakingpractice":
                    sb.AppendLine(@"### SpeakingPractice:
- type: ""SpeakingPractice""
- maxPoints: 10
- gradingMode: ""AiRubric""
- content: {
    ""topic"": ""Speaking prompt topic"",
    ""instructions"": ""Speak for 30-60 seconds on the topic."",
    ""targetDurationSeconds"": 30,
    ""suggestedPrompts"": [""Guiding question 1"", ""Guiding question 2""]
  }
- grading: {
    ""rubricCode"": ""LESSON_SPEAKING"",
    ""rubricVersion"": 1
  }");
                    break;

                case "readingcomprehension":
                    sb.AppendLine(@"### ReadingComprehension:
- type: ""ReadingComprehension""
- maxPoints: 10
- gradingMode: ""Composite""
- content: {
    ""passage"": ""Passage text..."",
    ""subQuestions"": [
      {
        ""id"": ""q-1"",
        ""questionKind"": ""SingleChoice"",
        ""order"": 1,
        ""maxPoints"": 5,
        ""questionText"": ""Question?"",
        ""options"": [{ ""id"": ""o-1"", ""text"": ""A"" }, { ""id"": ""o-2"", ""text"": ""B"" }],
        ""grading"": { ""correctOptionIds"": [""o-1""], ""explanation"": ""..."" }
      }
    ]
  }
- grading: {}");
                    break;

                case "listeningcomprehension":
                    sb.AppendLine(@"### ListeningComprehension:
- type: ""ListeningComprehension""
- maxPoints: 10
- gradingMode: ""Composite""
- content: {
    ""audioAssetId"": null,
    ""transcript"": ""Audio transcript text..."",
    ""subQuestions"": [
      {
        ""id"": ""q-1"",
        ""questionKind"": ""SingleChoice"",
        ""order"": 1,
        ""maxPoints"": 10,
        ""questionText"": ""Question?"",
        ""options"": [{ ""id"": ""o-1"", ""text"": ""A"" }, { ""id"": ""o-2"", ""text"": ""B"" }],
        ""grading"": { ""correctOptionIds"": [""o-1""], ""explanation"": ""..."" }
      }
    ]
  }
- grading: {}");
                    break;

                case "dictation":
                    sb.AppendLine(@"### Dictation:
- type: ""Dictation""
- maxPoints: 10
- gradingMode: ""Deterministic""
- content: {
    ""audioAssetId"": null,
    ""hint"": ""Listen and type what you hear.""
  }
- grading: {
    ""referenceTranscript"": ""Expected transcript sentence."",
    ""caseSensitive"": false,
    ""ignorePunctuation"": true
  }");
                    break;

                case "shadowing":
                    sb.AppendLine(@"### Shadowing:
- type: ""Shadowing""
- maxPoints: 10
- gradingMode: ""AiRubric""
- content: {
    ""audioAssetId"": null,
    ""transcript"": ""Sentence to speak aloud simultaneously."",
    ""speechRateWpm"": 120
  }
- grading: {
    ""rubricCode"": ""LESSON_SHADOWING"",
    ""rubricVersion"": 1
  }");
                    break;

                case "conversationroleplay":
                    sb.AppendLine(@"### ConversationRolePlay:
- type: ""ConversationRolePlay""
- maxPoints: 10
- gradingMode: ""AiRubric""
- content: {
    ""scenario"": ""Role-play scenario..."",
    ""aiRole"": ""AI Persona"",
    ""learnerRole"": ""Learner Persona"",
    ""initialGreeting"": ""AI greeting"",
    ""minTurns"": 3,
    ""maxTurns"": 6
  }
- grading: {
    ""rubricCode"": ""LESSON_ROLEPLAY"",
    ""rubricVersion"": 1
  }");
                    break;

                case "writingpractice":
                    sb.AppendLine(@"### WritingPractice:
- type: ""WritingPractice""
- maxPoints: 10
- gradingMode: ""AiRubric""
- content: {
    ""prompt"": ""Writing prompt..."",
    ""minWords"": 40,
    ""maxWords"": 100
  }
- grading: {
    ""rubricCode"": ""LESSON_WRITING"",
    ""rubricVersion"": 1,
    ""guidelines"": ""Grammar and vocabulary focus...""
  }");
                    break;
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    public string BuildConcreteExerciseExamples(IReadOnlyList<string> types, string topic, string cefrLevel)
    {
        var sb = new StringBuilder();
        var uniqueTypes = types.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        int order = 1;

        for (int i = 0; i < uniqueTypes.Count; i++)
        {
            string t = uniqueTypes[i].ToLowerInvariant();
            string jsonChunk = t switch
            {
                "sentenceunscramble" => $@"    {{
      ""key"": ""ex_{order:D2}_unscramble"",
      ""type"": ""SentenceUnscramble"",
      ""title"": ""Sentence Unscramble: {topic}"",
      ""instructions"": ""Rearrange the tokens into a grammatically correct sentence."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{
        ""prompt"": ""Rearrange words to form a correct sentence:"",
        ""tokens"": [
          {{ ""id"": ""t-1"", ""text"": ""I"" }},
          {{ ""id"": ""t-2"", ""text"": ""usually"" }},
          {{ ""id"": ""t-3"", ""text"": ""start"" }},
          {{ ""id"": ""t-4"", ""text"": ""work"" }},
          {{ ""id"": ""t-5"", ""text"": ""at"" }},
          {{ ""id"": ""t-6"", ""text"": ""eight."" }}
        ]
      }},
      ""grading"": {{
        ""validSequences"": [
          [""t-1"", ""t-2"", ""t-3"", ""t-4"", ""t-5"", ""t-6""]
        ],
        ""explanation"": ""Subject + frequency adverb + verb phrase + time: 'I usually start work at eight.'""
      }}
    }}",

                "multiplechoice" => $@"    {{
      ""key"": ""ex_{order:D2}_mcq"",
      ""type"": ""MultipleChoice"",
      ""title"": ""Multiple Choice: {topic}"",
      ""instructions"": ""Select the correct option to complete the statement."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{
        ""question"": ""Which sentence correctly describes a good {topic} habit?"",
        ""selectionMode"": ""Single"",
        ""options"": [
          {{ ""id"": ""opt-a"", ""text"": ""I takes a break every hour."" }},
          {{ ""id"": ""opt-b"", ""text"": ""I take a break every hour."" }},
          {{ ""id"": ""opt-c"", ""text"": ""I am take a break every hour."" }},
          {{ ""id"": ""opt-d"", ""text"": ""I took breaks tomorrow."" }}
        ]
      }},
      ""grading"": {{
        ""correctOptionIds"": [""opt-b""],
        ""explanation"": ""'I' takes the base verb form 'take' in the present simple tense.""
      }}
    }}",

                "fillintheblanks" => $@"    {{
      ""key"": ""ex_{order:D2}_fill"",
      ""type"": ""FillInTheBlanks"",
      ""title"": ""Fill in the Blanks: {topic}"",
      ""instructions"": ""Type the appropriate word into each blank."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{
        ""textWithBlanks"": ""Every morning, I [blank-1] up at seven and [blank-2] breakfast before working."",
        ""blanks"": [
          {{ ""id"": ""blank-1"", ""hint"": ""wake / get"", ""maxLength"": 20 }},
          {{ ""id"": ""blank-2"", ""hint"": ""eat / have"", ""maxLength"": 20 }}
        ]
      }},
      ""grading"": {{
        ""isOpenEnded"": false,
        ""blankAnswers"": {{
          ""blank-1"": {{ ""acceptedAnswers"": [""wake"", ""get""], ""caseSensitive"": false }},
          ""blank-2"": {{ ""acceptedAnswers"": [""have"", ""eat"", ""make""], ""caseSensitive"": false }}
        }},
        ""explanation"": ""Common collocations: 'wake up' / 'get up' and 'have/eat breakfast'.""
      }}
    }}",

                "flashcards" => $@"    {{
      ""key"": ""ex_{order:D2}_vocab"",
      ""type"": ""Flashcards"",
      ""title"": ""Vocabulary Flashcards: {topic}"",
      ""instructions"": ""Review key vocabulary, definitions, and IPA pronunciation."",
      ""order"": {order},
      ""maxPoints"": 0,
      ""gradingMode"": ""SelfAssessment"",
      ""content"": {{
        ""cards"": [
          {{
            ""id"": ""c-1"",
            ""term"": ""commute"",
            ""ipa"": ""/kəˈmjuːt/"",
            ""meaningVi"": ""quãng đường đi lại hàng ngày"",
            ""exampleEn"": ""Working from home eliminates daily commute time."",
            ""exampleVi"": ""Làm việc tại nhà giúp giảm thiểu thời gian đi lại mỗi ngày."",
            ""audioAssetId"": null
          }},
          {{
            ""id"": ""c-2"",
            ""term"": ""work-life balance"",
            ""ipa"": ""/ˌwɜːk.laɪf ˈbæl.əns/"",
            ""meaningVi"": ""cân bằng công việc và đời sống"",
            ""exampleEn"": ""Maintaining a consistent routine improves work-life balance."",
            ""exampleVi"": ""Duy trì thói quen đều đặn giúp cải thiện cân bằng giữa công việc và đời sống."",
            ""audioAssetId"": null
          }}
        ]
      }},
      ""grading"": {{}}
    }}",

                "matching" => $@"    {{
      ""key"": ""ex_{order:D2}_matching"",
      ""type"": ""Matching"",
      ""title"": ""Vocabulary Matching: {topic}"",
      ""instructions"": ""Match each action on the left with its definition on the right."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{
        ""leftItems"": [
          {{ ""id"": ""l-1"", ""text"": ""take a break"" }},
          {{ ""id"": ""l-2"", ""text"": ""attend a meeting"" }}
        ],
        ""rightItems"": [
          {{ ""id"": ""r-1"", ""text"": ""stop working briefly to rest"" }},
          {{ ""id"": ""r-2"", ""text"": ""join colleagues for discussion"" }}
        ]
      }},
      ""grading"": {{
        ""correctPairs"": [
          {{ ""leftId"": ""l-1"", ""rightId"": ""r-1"" }},
          {{ ""leftId"": ""l-2"", ""rightId"": ""r-2"" }}
        ],
        ""explanation"": ""'Take a break' means to rest briefly; 'Attend a meeting' means to join colleagues.""
      }}
    }}",

                "translation" => $@"    {{
      ""key"": ""ex_{order:D2}_translation"",
      ""type"": ""Translation"",
      ""title"": ""Translation: {topic}"",
      ""instructions"": ""Translate the Vietnamese sentence into natural English."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""AiRubric"",
      ""content"": {{
        ""direction"": ""ViToEn"",
        ""sourceText"": ""Tôi thường làm việc từ xa ba ngày một tuần để cân bằng cuộc sống."",
        ""context"": ""Talking about weekly schedule in {topic}""
      }},
      ""grading"": {{
        ""rubricCode"": ""LESSON_TRANSLATION"",
        ""rubricVersion"": 1,
        ""referenceTranslations"": [
          ""I usually work remotely three days a week to balance my life."",
          ""I often work from home three days a week for work-life balance.""
        ],
        ""requiredKeyIdeas"": [
          ""Adverb 'usually' / 'often' with present simple"",
          ""Work remotely / work from home"",
          ""Three days a week / three days per week""
        ]
      }}
    }}",

                "truefalse" => $@"    {{
      ""key"": ""ex_{order:D2}_truefalse"",
      ""type"": ""TrueFalse"",
      ""title"": ""True or False: {topic}"",
      ""instructions"": ""Read the context and decide whether each statement is True or False."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{
        ""context"": ""Alex is a software engineer who works remotely. He wakes up at 7:00 AM, exercises for 30 minutes, and starts coding at 8:30 AM. He shuts down his computer by 6:00 PM."",
        ""statements"": [
          {{ ""id"": ""s-1"", ""text"": ""Alex exercises before he starts coding."" }},
          {{ ""id"": ""s-2"", ""text"": ""Alex continues coding until late at night."" }}
        ]
      }},
      ""grading"": {{
        ""statementAnswers"": {{
          ""s-1"": true,
          ""s-2"": false
        }},
        ""explanations"": {{
          ""s-1"": ""True: Alex exercises for 30 minutes before starting coding at 8:30 AM."",
          ""s-2"": ""False: He shuts down his computer by 6:00 PM.""
        }}
      }}
    }}",

                "errorcorrection" => $@"    {{
      ""key"": ""ex_{order:D2}_error"",
      ""type"": ""ErrorCorrection"",
      ""title"": ""Error Correction: {topic}"",
      ""instructions"": ""Find the grammatical error and type the corrected sentence."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{
        ""incorrectSentence"": ""He don't like attending online meetings in the morning."",
        ""instructions"": ""Correct the subject-verb agreement mistake.""
      }},
      ""grading"": {{
        ""referenceCorrections"": [
          ""He doesn't like attending online meetings in the morning."",
          ""He does not like attending online meetings in the morning.""
        ],
        ""targetErrors"": [""Subject-verb agreement: 'don't' -> 'doesn't'""],
        ""rubricCode"": ""LESSON_ERROR_CORRECTION"",
        ""rubricVersion"": 1
      }}
    }}",

                "spellingpractice" => $@"    {{
      ""key"": ""ex_{order:D2}_spelling"",
      ""type"": ""SpellingPractice"",
      ""title"": ""Spelling: {topic}"",
      ""instructions"": ""Đọc gợi ý và viết từ tiếng Anh phù hợp."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{
        ""prompt"": ""Đọc gợi ý và viết từ tiếng Anh phù hợp."",
        ""definition"": ""Bản kế hoạch các hoạt động và thời gian chúng sẽ diễn ra."",
        ""exampleSentence"": ""I check my work ________ every morning at 8:30 AM."",
        ""hint"": ""Bắt đầu bằng chữ 's' và có 8 chữ cái (s _ _ _ _ _ _ e)."",
        ""letterCount"": 8
      }},
      ""grading"": {{
        ""targetWord"": ""schedule"",
        ""acceptedSpellings"": [""schedule""],
        ""caseSensitive"": false
      }}
    }}",

                "speakingpractice" => $@"    {{
      ""key"": ""ex_{order:D2}_speaking"",
      ""type"": ""SpeakingPractice"",
      ""title"": ""Speaking Practice: {topic}"",
      ""instructions"": ""Speak clearly for at least 30 seconds answering the prompts."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""AiRubric"",
      ""content"": {{
        ""topic"": ""Describe your typical daily schedule"",
        ""instructions"": ""Speak for 30 to 60 seconds about your morning habits and workday."",
        ""targetDurationSeconds"": 30,
        ""suggestedPrompts"": [
          ""What time do you usually get up?"",
          ""How do you start your working day?"",
          ""What is your favorite habit to relax in the evening?""
        ]
      }},
      ""grading"": {{
        ""rubricCode"": ""LESSON_SPEAKING"",
        ""rubricVersion"": 1,
        ""taskInstructions"": ""Assess present simple usage, time vocabulary, and clear pronunciation.""
      }}
    }}",

                "readingcomprehension" => $@"    {{
      ""key"": ""ex_{order:D2}_reading"",
      ""type"": ""ReadingComprehension"",
      ""title"": ""Reading Comprehension: {topic}"",
      ""instructions"": ""Read the passage carefully and answer the questions that follow."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Composite"",
      ""content"": {{
        ""passage"": ""Remote work offers flexibility, but structuring your daily routine is essential. Many software engineers begin by establishing regular working hours and taking short breaks to maintain productivity and prevent burnout."",
        ""subQuestions"": [
          {{
            ""id"": ""q-1"",
            ""questionKind"": ""SingleChoice"",
            ""order"": 1,
            ""maxPoints"": 5,
            ""questionText"": ""What helps remote software engineers prevent burnout?"",
            ""options"": [
              {{ ""id"": ""opt-1"", ""text"": ""Taking regular short breaks"" }},
              {{ ""id"": ""opt-2"", ""text"": ""Working non-stop through the evening"" }}
            ],
            ""grading"": {{
              ""correctOptionIds"": [""opt-1""],
              ""explanation"": ""Taking short breaks prevents burnout and maintains productivity.""
            }}
          }}
        ]
      }},
      ""grading"": {{}}
    }}",

                "listeningcomprehension" => $@"    {{
      ""key"": ""ex_{order:D2}_listening"",
      ""type"": ""ListeningComprehension"",
      ""title"": ""Listening Comprehension: {topic}"",
      ""instructions"": ""Listen to the recording and answer the comprehension questions."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Composite"",
      ""content"": {{
        ""audioAssetId"": null,
        ""transcript"": ""Hi team, today we need to coordinate on the new deployment schedule. I suggest we conduct our daily standup at 9:30 AM so everyone has enough time to prepare."",
        ""subQuestions"": [
          {{
            ""id"": ""lq-1"",
            ""questionKind"": ""SingleChoice"",
            ""order"": 1,
            ""maxPoints"": 10,
            ""questionText"": ""What time does the speaker suggest holding the daily standup?"",
            ""options"": [
              {{ ""id"": ""opt-1"", ""text"": ""At 9:30 AM"" }},
              {{ ""id"": ""opt-2"", ""text"": ""At 8:00 AM"" }}
            ],
            ""grading"": {{
              ""correctOptionIds"": [""opt-1""],
              ""explanation"": ""The speaker states: 'I suggest we conduct our daily standup at 9:30 AM.'""
            }}
          }}
        ]
      }},
      ""grading"": {{}}
    }}",

                "dictation" => $@"    {{
      ""key"": ""ex_{order:D2}_dictation"",
      ""type"": ""Dictation"",
      ""title"": ""Dictation: {topic}"",
      ""instructions"": ""Listen to the audio sentence and type exactly what you hear."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{
        ""audioAssetId"": null,
        ""hint"": ""A sentence about daily remote work routines.""
      }},
      ""grading"": {{
        ""referenceTranscript"": ""I usually organize my schedule before starting morning tasks."",
        ""caseSensitive"": false,
        ""ignorePunctuation"": true
      }}
    }}",

                "shadowing" => $@"    {{
      ""key"": ""ex_{order:D2}_shadowing"",
      ""type"": ""Shadowing"",
      ""title"": ""Shadowing: {topic}"",
      ""instructions"": ""Listen carefully and repeat the sentence aloud with natural pronunciation, pacing, and intonation."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""AiRubric"",
      ""content"": {{
        ""audioAssetId"": null,
        ""transcript"": ""I establish clear boundaries between my working hours and personal time."",
        ""speechRateWpm"": 120
      }},
      ""grading"": {{
        ""rubricCode"": ""LESSON_SHADOWING"",
        ""rubricVersion"": 1
      }}
    }}",

                "conversationroleplay" => $@"    {{
      ""key"": ""ex_{order:D2}_roleplay"",
      ""type"": ""ConversationRolePlay"",
      ""title"": ""Role Play: {topic}"",
      ""instructions"": ""Have a conversation with your AI colleague about work-life balance and daily routines."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""AiRubric"",
      ""content"": {{
        ""scenario"": ""You and a remote teammate are discussing daily habits, work schedule, and taking breaks during remote work."",
        ""aiRole"": ""Supportive Senior Colleague"",
        ""learnerRole"": ""Remote Team Member"",
        ""initialGreeting"": ""Hi! How is your daily remote routine going this week? Do you find enough time to take breaks?"",
        ""minTurns"": 3,
        ""maxTurns"": 6
      }},
      ""grading"": {{
        ""rubricCode"": ""LESSON_ROLEPLAY"",
        ""rubricVersion"": 1
      }}
    }}",

                "writingpractice" => $@"    {{
      ""key"": ""ex_{order:D2}_writing"",
      ""type"": ""WritingPractice"",
      ""title"": ""Short Writing: {topic}"",
      ""instructions"": ""Write a short paragraph (40-80 words) describing your daily schedule or remote work habit."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""AiRubric"",
      ""content"": {{
        ""prompt"": ""Describe your typical daily routine when working or studying from home, including when you start and how you relax."",
        ""minWords"": 40,
        ""maxWords"": 80
      }},
      ""grading"": {{
        ""rubricCode"": ""LESSON_WRITING"",
        ""rubricVersion"": 1
      }}
    }}",

                _ => $@"    {{
      ""key"": ""ex_{order:D2}_item"",
      ""type"": ""{(t.Length > 0 ? char.ToUpperInvariant(t[0]) + (t.Length > 1 ? t[1..] : "") : "Exercise")}"",
      ""title"": ""Exercise: {topic}"",
      ""instructions"": ""Complete the exercise."",
      ""order"": {order},
      ""maxPoints"": 10,
      ""gradingMode"": ""Deterministic"",
      ""content"": {{}},
      ""grading"": {{}}
    }}"
            };

            sb.Append(jsonChunk);
            if (i < uniqueTypes.Count - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }

            order++;
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }
}
