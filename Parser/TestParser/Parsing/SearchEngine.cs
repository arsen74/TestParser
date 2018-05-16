using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TestParser.Sys;

namespace TestParser.Parsing
{
    public static class SearchEngine
    {
        private static Dictionary<int, int> _symbolHash = new Dictionary<int, int>(_romanAlphabetCapacity + _сyrillicAlphabetCapacity);

        private static ConcurrentDictionary<string, int[]> _bmSuffixTables = new ConcurrentDictionary<string, int[]>();
        private static ConcurrentDictionary<string, int[]> _bmGoodShiftTables = new ConcurrentDictionary<string, int[]>();
        private static ConcurrentDictionary<string, int[]> _bmBadCharacterTables = new ConcurrentDictionary<string, int[]>();

        private static readonly int _romanAlphabetCapacity = 95;
        private static readonly int _сyrillicAlphabetCapacity = 255;

        static SearchEngine()
        {
            // Инициализация словаря символов алфавита
            int i = 0;
            for (int j = 0x0020; j < 0x007f; j++) //Символы латиницы
            {
                _symbolHash.Add(j, i);

                i++;
            }

            for (int j = 0x0400; j < 0x04ff; j++) //Символы кириллицы
            {
                _symbolHash.Add(j, i);

                i++;
            }
        }

        public static ATagResult[] FindLinks(string html)
        {
            Guard.ArgumentNotEmpty(html, "html");

            var stopSpaces = InnerFindCommonStopSpaces(html);

            return InnerFindATags(html, stopSpaces).Data;
        }

        private static SearchEngineResult<ATagResult> InnerFindATags(string html, Tuple<int, int>[] stopSpaces)
        {
            Guard.ArgumentNotEmpty(html, "html");

            SearchEngineResult<ATagResult> result = null;

            try
            {
                var raw = InnerFindTag(html, HtmlTerm.A_TAG_BEGIN, HtmlTerm.A_TAG_END, stopSpaces: stopSpaces);

                //Возможно, тег a написан заглавными буквами
                raw.Union(InnerFindTag(html, HtmlTerm.A_TAG_BEGIN.ToUpperInvariant(), HtmlTerm.A_TAG_END.ToUpperInvariant(), stopSpaces: stopSpaces));

                if (raw.Data != null)
                {
                    var tags = new List<ATagResult>();
                    ATagResult tmp;
                    for (int i = 0; i < raw.Data.Length; i++)
                    {
                        tmp = new ATagResult
                        {
                            Html = raw.Data[i].Html
                        };
                        foreach (var attr in raw.Data[i].Attributes)
                        {
                            switch (attr.Key)
                            {
                                case HtmlTerm.HREF_ATTRIBUTE:
                                    tmp.Href = attr.Value;
                                    tmp.IsExternal = !tmp.Href.StartsWith("/");
                                    break;
                                case HtmlTerm.HREFLANG_ATTRIBUTE:
                                    tmp.HrefLang = attr.Value;
                                    break;
                                case HtmlTerm.TARGET_ATTRIBUTE:
                                    tmp.Target = attr.Value;
                                    break;
                                case HtmlTerm.DOWNLOAD_ATTRIBUTE:
                                    tmp.Download = attr.Value;
                                    break;
                                case HtmlTerm.REL_ATTRIBUTE:
                                    tmp.Rel = attr.Value;
                                    break;
                                case HtmlTerm.TYPE_ATTRIBUTE:
                                    tmp.Type = attr.Value;
                                    break;
                            }
                        }

                        tags.Add(tmp);
                    }

                    result = new SearchEngineResult<ATagResult>(tags.ToArray())
                    {
                        ErrorMessage = raw.ErrorMessage
                    };
                }
                else
                {
                    result = new SearchEngineResult<ATagResult>(raw.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                result = new SearchEngineResult<ATagResult>(ex.Message);
            }

            return result;
        }

        private static Tuple<int, int>[] InnerFindCommonStopSpaces(string html)
        {
            return InnerFindHtmlComments(html)
                .Concat(InnerFindHtmlNoIndex(html))
                .ToArray();
        }

        /// <summary>
        /// Все что внутри комментария <!--текст--> игнорируется
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static Tuple<int, int>[] InnerFindHtmlComments(string html)
        {
            return InnerFindHtmlIgnore(html, HtmlTerm.COMMENT_TAG_BEGIN, HtmlTerm.COMMENT_TAG_END);
        }

        /// <summary>
        /// Все что внутри <noindex></noindex> или <!--noindex--><!--/noindex--> игнорируется
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static Tuple<int, int>[] InnerFindHtmlNoIndex(string html)
        {
            return InnerFindHtmlIgnore(html, HtmlTerm.NOINDEX_TAG_BEGIN, HtmlTerm.NOINDEX_TAG_END)
                .Concat(InnerFindHtmlIgnore(html, HtmlTerm.NOINDEX_TAG_BEGIN_ALTERNATIVE, HtmlTerm.NOINDEX_TAG_END_ALTERNATIVE))
                .ToArray();
        }

        /// <summary>
        /// Все что внутри игнорируемых тегов отбрасывается
        /// </summary>
        /// <param name="html"></param>
        /// <param name="beginTag"></param>
        /// <param name="endTag"></param>
        /// <returns></returns>
        private static Tuple<int, int>[] InnerFindHtmlIgnore(string html, string beginTag, string endTag)
        {
            Tuple<int, int>[] result = null;

            var startTermEntry = TurboBoyerMoore(beginTag, html, true).Select(p => p.Value).ToArray();
            var endTermEntry = TurboBoyerMoore(endTag, html, false).Select(p => p.Value).ToArray();

            if (startTermEntry.Length == endTermEntry.Length)
            {
                result = new Tuple<int, int>[startTermEntry.Length];

                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = Tuple.Create(startTermEntry[i], endTermEntry[i]);
                }
            }

            return result ?? new Tuple<int, int>[0];
        }

        private static SearchEngineResult<GenericTagResult> InnerFindTag(string html, string startTag, string endTag, Tuple<int, int>[] stopSpaces = null, bool errorIfNotValid = false)
        {
            var startTermEntry = TurboBoyerMoore(startTag, html, true);

            if (startTermEntry.Length == 0)
            {
                return new SearchEngineResult<GenericTagResult>(new GenericTagResult[0]);
            }

            var endTermEntry = TurboBoyerMoore(endTag, html, false);

            if ((stopSpaces != null) && (stopSpaces.Length > 0))
            {
                //Выкидываем индексы, которые входят в промежуток "стоп" отрезков
                for (int i = 0; i < stopSpaces.Length; i++)
                {
                    startTermEntry = startTermEntry.Where(p => (p.Value < stopSpaces[i].Item1) || (p.Value > stopSpaces[i].Item2)).ToArray();
                    endTermEntry = endTermEntry.Where(p => (p.Value < stopSpaces[i].Item1) || (p.Value > stopSpaces[i].Item2)).ToArray();
                }
            }

            if ((startTermEntry.Length != endTermEntry.Length) && (endTag.Equals(HtmlTerm.TAG_END) || endTag.Equals(HtmlTerm.TAG_END_ALTERNATIVE)))
            {
                var tmp = new List<TagSearchIndex>();
                int startIndex = 0;
                for (int i = 0; i < endTermEntry.Length; i++)
                {
                    if ((startIndex < startTermEntry.Length))
                    {
                        if (endTermEntry[i].Value > startTermEntry[startIndex].Value)
                        {
                            tmp.Add(endTermEntry[i]);

                            startIndex++;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                endTermEntry = tmp.ToArray();
            }

            if (errorIfNotValid && (startTermEntry.Length != endTermEntry.Length))
            {
                return new SearchEngineResult<GenericTagResult>("Невалидный html");
            }

            bool isStartTagClosed = startTag.Contains(HtmlTerm.TAG_END_ALTERNATIVE);

            var termEntries = startTermEntry.Union(endTermEntry, TagSearchIndex.DefaultComparer).ToArray();

            HybridSort<int>.Sort(termEntries);

            var result = new List<GenericTagResult>(startTermEntry.Length);

            var tagStack = new Stack<GenericTagResult>(startTermEntry.Length);

            Action<GenericTagResult, int, string> attributeResolver =
                (tag, searchLength, term) =>
                {
                    int tagEndIndex = tag.Html.IndexOf(term);
                    if (tagEndIndex > 0)
                    {
                        tag.RawAttributesHtml = tag.Html.Substring(0, tagEndIndex);
                    }
                    else
                    {
                        tag.RawAttributesHtml = tag.Html;
                    }

                    var attributes = tag.RawAttributesHtml.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var tmpAttributes = new List<string>();
                    bool attrClosed = false;
                    for (int j = 0; j < attributes.Length; j++)
                    {
                        if ((tmpAttributes.Count > 0) && !attributes[j].Contains("="))
                        {
                            if (!attrClosed)
                            {
                                tmpAttributes[tmpAttributes.Count - 1] = string.Concat(tmpAttributes[tmpAttributes.Count - 1], " ", attributes[j]);
                            }
                            else
                            {
                                tmpAttributes.Add(attributes[j]);
                            }
                            attrClosed = attributes[j].EndsWith(@"""");
                        }
                        else
                        {
                            tmpAttributes.Add(attributes[j]);
                        }
                    }
                    attributes = tmpAttributes.ToArray();

                    int attributeSeparatorIndex;
                    for (int j = 0; j < attributes.Length; j++)
                    {
                        attributeSeparatorIndex = attributes[j].IndexOf("=");
                        if (attributeSeparatorIndex > 0)
                        {
                            tag.AddAttributePair(attributes[j].Substring(0, attributeSeparatorIndex), attributes[j].Substring(attributeSeparatorIndex + 1).Trim(new[] { '"' }));
                        }
                        else if (!string.IsNullOrWhiteSpace(attributes[j]))
                        {
                            tag.AddAttributePair(attributes[j], attributes[j]);
                        }
                    }

                    tag.Html = (tagEndIndex + 1 < tag.Html.Length) && (tagEndIndex + 2 < searchLength) ?
                        tag.Html.Substring(tagEndIndex + 1, searchLength - tagEndIndex - 1) :
                        string.Empty;
                };

            int shiftLength = 0;
            GenericTagResult current;
            for (int i = 0; i < termEntries.Length; i++)
            {
                if (termEntries[i].IsOpeningTag)
                {
                    tagStack.Push(
                        new GenericTagResult
                        {
                            StartIndex = termEntries[i].Value
                        });
                }
                else
                {
                    current = tagStack.Pop();

                    current.EndIndex = termEntries[i].Value;

                    shiftLength = termEntries[i].Value - current.StartIndex - startTag.Length - (!isStartTagClosed ? 1 : 0);
                    current.Html = html.Substring(current.StartIndex + startTag.Length + (!isStartTagClosed ? 1 : 0), shiftLength);

                    if (!isStartTagClosed)
                    {
                        attributeResolver(current, shiftLength, HtmlTerm.TAG_END_ALTERNATIVE);
                    }

                    result.Add(current);
                }
            }

            return new SearchEngineResult<GenericTagResult>(result.ToArray());
        }

        #region "Турбо" Boyer - Moore алгоритм

        /// <summary>
        /// Поиск строки в тексте по алгоритму Boyer - Moore в "турбо" модификации
        /// </summary>
        /// <param name="pattern">Искомая строка</param>
        /// <param name="text">Текст, в котором ищется строка</param>
        /// <returns>Набор индексов начала вхождения строки в тексте - находятся все вхождения</returns>
        private static TagSearchIndex[] TurboBoyerMoore(string pattern, string text, bool isOpeningTag)
        {
            var result = new List<TagSearchIndex>();

            int m = pattern.Length;
            int n = text.Length;

            if (m <= n)
            {
                //Preprocessing
                var bmgs = GetBoyerMooreGoodShiftTable(pattern, m);

                var bmbc = GetBoyerMooreBadCharacterTable(pattern, m);

                // Searching
                int bcShift, i, j, shift, k1, k2, turboShift;

                j = k1 = 0;
                shift = m;
                while (j <= n - m)
                {
                    i = m - 1;
                    while ((i >= 0) && (pattern[i] == text[i + j]))
                    {
                        i--;

                        if ((k1 != 0) && (i == m - 1 - shift))
                        {
                            i -= k1;
                        }
                    }

                    if (i < 0)
                    {
                        result.Add(new TagSearchIndex { IsOpeningTag = isOpeningTag, Value = j });

                        shift = bmgs[0];
                        k1 = m - shift;
                    }
                    else
                    {
                        k2 = m - 1 - i;
                        turboShift = k1 - k2;

                        if (!_symbolHash.ContainsKey(text[i + j]))
                        {
                            bcShift = i + 1;
                        }
                        else
                        {
                            bcShift = bmbc[_symbolHash[text[i + j]]] - m + 1 + i;
                        }

                        shift = Math.Max(turboShift, bcShift);
                        shift = Math.Max(shift, bmgs[i]);

                        if (shift == bmgs[i])
                        {
                            k1 = Math.Min(m - shift, k2);
                        }
                        else
                        {
                            if (turboShift < bcShift)
                            {
                                shift = Math.Max(shift, k1 + 1);
                            }

                            k1 = 0;
                        }
                    }

                    j += shift;
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Создание таблицы "стоп" символов для искомой строки по алгоритму Boyer - Moore
        /// </summary>
        /// <param name="pattern">Искомая строка</param>
        /// <param name="patternLength">Длина искомой строки</param>
        /// <returns></returns>
        private static int[] GetBoyerMooreBadCharacterTable(string pattern, int patternLength)
        {
            return _bmBadCharacterTables.GetOrAdd(pattern,
                p =>
                {
                    int[] result = new int[_romanAlphabetCapacity + _сyrillicAlphabetCapacity];

                    int i;
                    for (i = 0; i < _romanAlphabetCapacity + _сyrillicAlphabetCapacity; i++)
                    {
                        result[i] = patternLength;
                    }

                    for (i = 0; i < patternLength - 1; i++)
                    {
                        if (_symbolHash.ContainsKey(p[i]))
                        {
                            result[_symbolHash[p[i]]] = patternLength - i - 1;
                        }
                    }

                    return result;
                });
        }

        /// <summary>
        /// Создание таблицы смещения для искомой строки по алгоритму Boyer - Moore
        /// </summary>
        /// <param name="pattern">Искомая строка</param>
        /// <param name="patternLength">Длина искомой строки</param>
        /// <returns></returns>
        private static int[] GetBoyerMooreGoodShiftTable(string pattern, int patternLength)
        {
            return _bmGoodShiftTables.GetOrAdd(pattern,
                p =>
                {
                    int[] result = new int[patternLength];

                    int i, j;

                    var suffix = GetBoyerMooreSuffixTable(p, patternLength);

                    for (i = 0; i < patternLength; i++)
                    {
                        result[i] = patternLength;
                    }

                    j = 0;
                    for (i = patternLength - 1; i >= 0; i--)
                    {
                        if (suffix[i] == i + 1)
                        {
                            for (; j < patternLength - 1 - i; j++)
                            {
                                if (result[j] == patternLength)
                                {
                                    result[j] = patternLength - 1 - i;
                                }
                            }
                        }
                    }

                    for (i = 0; i <= patternLength - 2; i++)
                    {
                        result[patternLength - 1 - suffix[i]] = patternLength - 1 - i;
                    }

                    return result;
                });
        }

        /// <summary>
        /// Создание суффиксной таблицы для искомой строки по алгоритму Boyer - Moore
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="patternLength"></param>
        /// <returns></returns>
        private static int[] GetBoyerMooreSuffixTable(string pattern, int patternLength)
        {
            return _bmSuffixTables.GetOrAdd(pattern,
                p =>
                {
                    int[] result = new int[patternLength];

                    result[patternLength - 1] = patternLength;

                    int k1 = 0, k2 = patternLength - 1;
                    for (int i = patternLength - 2; i >= 0; i--)
                    {
                        if ((i > k2) && (result[i + patternLength - 1 - k1] < i - k2))
                        {
                            result[i] = result[i + patternLength - 1 - k1];
                        }
                        else
                        {
                            if (i < k2)
                            {
                                k2 = i;
                            }

                            k1 = i;
                            while ((k2 >= 0) && (p[k2] == p[k2 + patternLength - 1 - k1]))
                            {
                                k2--;
                            }

                            result[i] = k1 - k2;
                        }
                    }

                    return result;
                });
        }

        #endregion
    }
}
