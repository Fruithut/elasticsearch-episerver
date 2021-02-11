using ElasticEpiserver.Module.Business.Data;
using ElasticEpiserver.Module.Engine.Languages;
using Nest;
using Language = ElasticEpiserver.Module.Engine.Languages.Language;

// ReSharper disable MemberHidesStaticFromOuterClass

namespace ElasticEpiserver.Module.Engine.Indexing
{
    public static class IndexSettingsFactory
    {
        public static class Names
        {
            public static class Analyzers
            {
                public const string Language = "epi_language_analyzer";
                public const string Ngram = "epi_ngram_analyzer";

                public static class Shorthand
                {
                    public const string Language = "language";
                    public const string Ngram = "ngram";
                }
            }

            public static class TokenFilters
            {
                public const string EditorSynonyms = "epi_editor_synonyms";
            }
        }

        public static IndexSettingsDescriptor GetIndexSettingByLanguageName(string languageName)
        {
            var language = ElasticEpiLanguageHelper.Resolve(languageName);

            switch (language)
            {
                case Language.English:
                    return GetEnglishIndexSettings();
                case Language.Bokmal:
                case Language.Nynorsk:
                    return GetNorwegianIndexSettings(language);
                default:
                    return GetEnglishIndexSettings();
            }
        }

        private static IndexSettingsDescriptor GetEnglishIndexSettings()
        {
            var settingsDescriptor = new IndexSettingsDescriptor()
                .Setting("index.max_ngram_diff", 2)
                .Analysis(a => a
                    .Tokenizers(t => t
                        .NGram("epi_ngram_tokenizer", ngram => ngram
                            .MaxGram(5)
                            .MinGram(3)
                        ))
                    .TokenFilters(tf => tf
                        .Stop("epi_english_stopwords", stop => stop.StopWords("_english_"))
                        .Stemmer("epi_english_stemmer", stem => stem.Language("english"))
                        .Synonym(Names.TokenFilters.EditorSynonyms, syn => syn
                            .Expand()
                            .Tokenizer("keyword")
                            .Synonyms(SynonymHelper.ResolveSynonymsForLanguage("en"))
                        ))
                    .Analyzers(an => an
                        .Custom(Names.Analyzers.Ngram, customNgram => customNgram
                            .CharFilters("html_strip")
                            .Tokenizer("epi_ngram_tokenizer")
                            .Filters("lowercase", "epi_english_stopwords", "epi_english_stemmer"))
                        .Custom(Names.Analyzers.Language, customLanguage => customLanguage
                            .CharFilters("html_strip")
                            .Tokenizer("standard")
                            .Filters("lowercase", Names.TokenFilters.EditorSynonyms, "epi_english_stopwords", "epi_english_stemmer"))
                    )
                );

            return settingsDescriptor;
        }

        private static IndexSettingsDescriptor GetNorwegianIndexSettings(Language variant)
        {
            var norwegianCultures = ElasticEpiLanguageHelper.GetNorwegianCultures();
            
            var settingsDescriptor = new IndexSettingsDescriptor()
                .Setting("index.max_ngram_diff", 2)
                .Analysis(a => a
                    .Tokenizers(t => t
                        .NGram("epi_ngram_tokenizer", ngram => ngram
                            .MaxGram(5)
                            .MinGram(3)
                        ))
                    .TokenFilters(tf => tf
                        .Stop("epi_norwegian_stopwords", stop => stop.StopWordsPath("norwegian_stop.txt"))
                        .Stemmer("epi_norwegian_stemmer", stem => stem.Language(variant == Language.Bokmal ? "light_norwegian" : "light_nynorsk"))
                        .Synonym("epi_norwegian_synonyms", syn => syn.SynonymsPath("nynorsk.txt"))
                        .Synonym(Names.TokenFilters.EditorSynonyms, syn => syn
                            .Expand()
                            .Tokenizer("keyword")
                            .Synonyms(SynonymHelper.ResolveSynonymsForLanguage(variant == Language.Bokmal ? norwegianCultures.Bokmal.Name : norwegianCultures.Nynorsk.Name))
                        ))
                    .Analyzers(an => an
                        .Custom(Names.Analyzers.Ngram, customNgram => customNgram
                            .CharFilters("html_strip")
                            .Tokenizer("epi_ngram_tokenizer")
                            .Filters("lowercase", "epi_norwegian_stopwords", "epi_norwegian_stemmer"))
                        .Custom(Names.Analyzers.Language, customLanguage => customLanguage
                            .CharFilters("html_strip")
                            .Tokenizer("standard")
                            .Filters("lowercase", "epi_norwegian_synonyms", Names.TokenFilters.EditorSynonyms, "epi_norwegian_stopwords", "epi_norwegian_stemmer"))
                    )
                );

            return settingsDescriptor;
        }
    }
}