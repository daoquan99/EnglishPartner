using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.Vocabulary.Entities;

namespace EnglishLiveTutor.Application.Features.Vocabulary;

internal static class VocabularyAdminEditor
{
    public static VocabularySenseEntity Apply(VocabularyEntity vocab, UpsertVocabularyAdminRequest req, VocabularySenseEntity? existingSense = null)
    {
        if (!string.IsNullOrWhiteSpace(req.UkIpa))
            vocab.AddPronunciation("UK", req.UkIpa.Trim(), stressPattern: req.StressPattern);
        if (!string.IsNullOrWhiteSpace(req.UsIpa))
            vocab.AddPronunciation("US", req.UsIpa.Trim(), stressPattern: req.StressPattern);

        if (req.WordFamilies is not null)
        {
            foreach (var wf in req.WordFamilies.Where(x => !string.IsNullOrWhiteSpace(x.WordForm)))
                vocab.AddWordFamily(wf.WordForm.Trim(), wf.PartOfSpeech);
        }

        var sense = existingSense ?? vocab.AddSense(
            partOfSpeech: string.IsNullOrWhiteSpace(req.PartOfSpeech) ? "noun" : req.PartOfSpeech.Trim(),
            definitionEn: string.IsNullOrWhiteSpace(req.DefinitionEn) ? req.Word.Trim() : req.DefinitionEn.Trim(),
            meaningVi: string.IsNullOrWhiteSpace(req.MeaningVi) ? req.Word.Trim() : req.MeaningVi.Trim(),
            cefrLevel: string.IsNullOrWhiteSpace(req.CefrLevel) ? "B1" : req.CefrLevel.Trim().ToUpperInvariant(),
            descriptionEn: req.DescriptionEn ?? string.Empty,
            isPrimary: true,
            displayOrder: 1);

        if (existingSense is not null)
        {
            existingSense.UpdateDetails(req.PartOfSpeech,
                string.IsNullOrWhiteSpace(req.DefinitionEn) ? req.Word : req.DefinitionEn,
                string.IsNullOrWhiteSpace(req.MeaningVi) ? req.Word : req.MeaningVi,
                req.CefrLevel, req.DescriptionEn);
        }

        if (req.Synonyms is not null)
        {
            int order = 1;
            foreach (var syn in req.Synonyms.Where(x => !string.IsNullOrWhiteSpace(x)))
                sense.AddRelation("Synonym", syn.Trim(), order++);
        }

        if (req.Antonyms is not null)
        {
            int order = 1;
            foreach (var ant in req.Antonyms.Where(x => !string.IsNullOrWhiteSpace(x)))
                sense.AddRelation("Antonym", ant.Trim(), order++);
        }

        if (req.Collocations is not null)
        {
            int order = 1;
            foreach (var col in req.Collocations.Where(x => !string.IsNullOrWhiteSpace(x.Phrase)))
                sense.AddCollocation(col.Phrase.Trim(), col.MeaningVi, order++);
        }

        if (req.Examples is not null)
        {
            int order = 1;
            foreach (var ex in req.Examples.Where(x => !string.IsNullOrWhiteSpace(x.SentenceEn)))
                sense.AddExample(ex.SentenceEn.Trim(), ex.TranslationVi, ex.CefrLevel ?? req.CefrLevel, order++);
        }

        if (req.GrammarPatterns is not null)
        {
            foreach (var gp in req.GrammarPatterns.Where(x => !string.IsNullOrWhiteSpace(x.Pattern)))
                sense.AddGrammarPattern(gp.Pattern.Trim(), gp.ExplanationVi, gp.SampleSentence);
        }
        return sense;
    }
}
