using System.Text.RegularExpressions;

namespace ArchSitePlan.UI.Helpers
{
    internal class InputHelpers
    {
        //regex that matches disallowed text
        private static readonly Regex _regex = new Regex(@"^\d+(\.\d+)?$"); 
        public static bool IsNumber(string text)
        {
            return !_regex.IsMatch(text);
        }
    }
}
