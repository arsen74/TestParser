using System;

namespace TestParser.Parsing
{
    public static class HtmlTerm
    {
        /// <summary>
        /// Закрывающий тег
        /// </summary>
        public const string TAG_END = "/>";

        /// <summary>
        /// Закрывающий тег
        /// </summary>
        public const string TAG_END_ALTERNATIVE = ">";

        /// <summary>
        /// Открывающий тег комментария
        /// </summary>
        public const string COMMENT_TAG_BEGIN = "<!--";

        /// <summary>
        /// Закрывающий тег комментария
        /// </summary>
        public const string COMMENT_TAG_END = "-->";

        /// <summary>
        /// Открывающий тег noindex
        /// </summary>
        public const string NOINDEX_TAG_BEGIN = "<noindex>";

        /// <summary>
        /// Закрывающий тег noindex
        /// </summary>
        public const string NOINDEX_TAG_END = "</noindex>";

        /// <summary>
        /// Открывающий тег noindex
        /// </summary>
        public const string NOINDEX_TAG_BEGIN_ALTERNATIVE = "<!--noindex-->";

        /// <summary>
        /// Закрывающий тег noindex
        /// </summary>
        public const string NOINDEX_TAG_END_ALTERNATIVE = "<!--/noindex-->";

        /// <summary>
        /// Открывающий тег a
        /// </summary>
        public const string A_TAG_BEGIN = "<a";

        /// <summary>
        /// Закрывающий тег a
        /// </summary>
        public const string A_TAG_END = "</a>";

        /// <summary>
        /// Атрибут href тега 
        /// </summary>
        public const string HREF_ATTRIBUTE = "href";

        /// <summary>
        /// Атрибут target тега 
        /// </summary>
        public const string TARGET_ATTRIBUTE = "target";

        /// <summary>
        /// Атрибут download тега 
        /// </summary>
        public const string DOWNLOAD_ATTRIBUTE = "download";

        /// <summary>
        /// Атрибут rel тега 
        /// </summary>
        public const string REL_ATTRIBUTE = "rel";

        /// <summary>
        /// Атрибут rev тега 
        /// </summary>
        public const string REV_ATTRIBUTE = "rev";

        /// <summary>
        /// Атрибут hreflang тега 
        /// </summary>
        public const string HREFLANG_ATTRIBUTE = "hreflang";

        /// <summary>
        /// Атрибут type тега 
        /// </summary>
        public const string TYPE_ATTRIBUTE = "type";

        /// <summary>
        /// Атрибут name тега 
        /// </summary>
        public const string NAME_ATTRIBUTE = "name";
    }
}
