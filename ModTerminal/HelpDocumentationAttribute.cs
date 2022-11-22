using System;

namespace ModTerminal
{
    /// <summary>
    /// An attribute which provides help documentation on a terminal command or command parameter.
    /// Documentation SHOULD be complete sentences, starting with a capital letter and ending in a period.
    /// Documentation SHOULD NOT include line breaks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false)]
    public class HelpDocumentationAttribute : Attribute
    {
        public readonly string Docs;

        public HelpDocumentationAttribute(string docs)
        {
            Docs = docs;
        }
    }
}
