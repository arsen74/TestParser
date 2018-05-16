using System;

namespace TestParser.Sys
{
    public static class Guard
    {
        public static void ArgumentNotNull<T>(T argument, string argumentName)
        {
            if (Equals(argument, default(T)))
                throw new ArgumentNullException(
                    nameof(argument),
                    string.Concat(argumentName, " isn't defined"));
        }

        public static void ArgumentNotEmpty(string argument, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                if (argument == null)
                {
                    throw new ArgumentNullException(!string.IsNullOrWhiteSpace(argumentName) ? argumentName : string.Concat(nameof(argument), " is null"));
                }
                else
                {
                    throw new ArgumentException(!string.IsNullOrWhiteSpace(argumentName) ? argumentName : string.Concat(nameof(argument), " is empty"));
                }
            }
        }
    }
}
