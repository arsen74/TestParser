using System;
using System.Linq;
using TestParser.Sys;

namespace TestParser.Parsing
{
    public class SearchEngineResult<T> where T : BaseTagResult
    {
        public T[] Data { get; protected set; }
        
        public virtual bool Success
        {
            get
            {
                return string.IsNullOrWhiteSpace(ErrorMessage);
            }
        }
        
        public string ErrorMessage { get; set; }

        public SearchEngineResult()
        { }

        public SearchEngineResult(T[] result)
        {
            Data = result;
        }

        public SearchEngineResult(string errorMessage)
        {
            Guard.ArgumentNotEmpty(errorMessage, nameof(errorMessage));

            ErrorMessage = errorMessage;
        }

        public void Union(SearchEngineResult<T> anotherResult)
        {
            if ((anotherResult == null) ||
                ((anotherResult != null) && (anotherResult.Data == null)))
            {
                return;
            }

            Data = (Data ?? new T[0]).Union(anotherResult.Data, StartIndexComparer<T>.Default).ToArray();

            if (!string.IsNullOrWhiteSpace(anotherResult.ErrorMessage))
            {
                ErrorMessage = !string.IsNullOrWhiteSpace(ErrorMessage) ?
                    string.Concat(ErrorMessage, ". ", anotherResult.ErrorMessage) :
                    anotherResult.ErrorMessage;
            }
        }
    }
}
