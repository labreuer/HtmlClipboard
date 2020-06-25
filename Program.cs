using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace HtmlClipboard
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var plainText = string.Format("Thinking Clearly {0:HH:mm:ss}", DateTime.Now);
            var htmlFormat = string.Format("<a href=\"http://labreuer.wordpress.com\">{0}</a>", plainText);
            //var dataObject = new DataObject();
            //dataObject.SetData(DataFormats.Html, htmlFormat);
            //dataObject.SetData(DataFormats.Text, plainText);
            //dataObject.SetData(DataFormats.UnicodeText, plainText);
            //Clipboard.SetDataObject(dataObject);

            var html = Clipboard.GetData(DataFormats.Html) as string;

            if (args.FirstOrDefault() == "vc") {
                Clipboard.SetText(GetStrippedHtmlFragmentInnerContent(html));
            }
            if (args.FirstOrDefault() == "v") {
                Console.InputEncoding = Console.OutputEncoding = Encoding.UTF8;
                Console.WriteLine(html);
            }
            if (args.FirstOrDefault() == "u") {
                var text = Clipboard.GetText();

                AddBorder(args.Skip(1).ToArray(), html, text, "border-bottom");
            }
            if (args.FirstOrDefault() == "b")
            {
                var text = Clipboard.GetText();

                // HACK: not having additional style parameter
                //   (reason being: want a better way of doing padding, which makes hilighting look good)
                AddBorder(args.Skip(1).ToArray(), html, text, "padding: 0 2px; border");
            } if (args.FirstOrDefault() == "a")
            {
                const string DisqusUserRegex = @"https://disqus\.com/by/([^/]+)/";
                var text = Clipboard.GetText();

                if (Regex.IsMatch(text, @"^https?://en.wikipedia.org/wiki/"))
                {
                    WikipediafyHyperlink(text);
                }
                else if (Regex.IsMatch(text, DisqusUserRegex))
                {
                    var m = Regex.Match(text, DisqusUserRegex);
                    var s = string.Format("@{0}:disqus", m.Groups[1].Value);
                    ClipboardHelper.CopyToClipboard(s, s);
                }
                else if (html != null &&
                    Regex.IsMatch(text, @"^([\r\n]*[^\r\n]+){1,2}[\r\n]*$") &&
                    Regex.IsMatch(html, @"(?m)^SourceURL:(?!https://mail\.google\.com)"))
                {
                    var url = Regex.Match(html, @"(?m)(?<=^SourceURL:)\S+").Value;
                    var inside = Regex.Replace(text.Trim(), @"\s*[\r\n]+\s*", ": ");
                    // System.Web.HttpUtility.UrlEncode()
                    var a = string.Format("<a href=\"{0}\">{1}</a>", url, inside);
                    ClipboardHelper.CopyToClipboard(a, inside);
                }
                else
                {
                    // Evernote ~6.7.5 killed this by stripping out <code>...</code>
                    //text = Regex.Replace(text, @"`(\w+.*?\w+)`", "<code>$1</code>");
                    text = Regex.Replace(text, @"`(\w+.*?\w+)`", "<span style='font-family: consolas,monospace;'>$1</span>");

                    if (Regex.IsMatch(text, @"<a href=""[^""]+"">((?!</a>).)+$"))
                        text += "</a>";

                    ClipboardHelper.CopyToClipboard(text, text);
                }
            }
            if (args.FirstOrDefault() == "vv") {
                var o = Clipboard.GetDataObject();

                foreach (var f in o.GetFormats())
                {
                    if (Array.IndexOf(new string[] { "Locale", "OEMText" }, f) >= 0)
                        continue;

                    Console.WriteLine("---------------------\n{0}:\n{1}\n", f, o.GetData(f));
                }
            }
            if (args.FirstOrDefault() == "rtf") {
                var rtf = Clipboard.GetData(DataFormats.Rtf) as string;

                if (args.Length == 1)
                    Console.WriteLine(rtf);
                else
                    System.IO.File.WriteAllText(args[1], rtf);
            }
        }

        private static void AddBorder(string[] args, string html, string text, string borderStyleProperty)
        {
            Func<bool, string, bool> failIfTrue = (b, s) =>
            {
                if (b)
                    Console.WriteLine(s);
                return b;
            };

            if (failIfTrue(string.IsNullOrEmpty(html), "No HTML found on clipboard."))
                return;

            var insideFragment = GetStrippedHtmlFragmentInnerContent(html);

            if (failIfTrue(insideFragment == null, "No fragments found."))
                return;

            //Console.WriteLine(insideFragment);
            var underlineOpenFormat = "<span style=\"{2}: {0}px solid {1}\">";
            var size = args.Length >= 1 ? args[0] : "1";
            var color = args.Length >= 2 ? args[1] : "red";
            var underlineOpen = string.Format(underlineOpenFormat, size, color, borderStyleProperty);
            var underlineClose = "</span>";
            var underlineOpenRegex = string.Format(underlineOpenFormat, @"\d", @"\w+", borderStyleProperty);
            var ropts = RegexOptions.IgnoreCase;
            var alreadyUnderline = Regex.IsMatch(insideFragment, underlineOpenRegex, ropts);
            var newFragment = alreadyUnderline
                ? Regex.Replace(insideFragment, underlineOpenRegex, underlineOpen, ropts)
                : underlineOpen + insideFragment + underlineClose;
            Console.WriteLine(newFragment);

            if (failIfTrue(insideFragment.Equals(newFragment, StringComparison.OrdinalIgnoreCase), "already contains underline"))
                return;

            ClipboardHelper.CopyToClipboard(newFragment, text);
        }

        private static string GetStrippedHtmlFragmentInnerContent(string html)
        {
            // BUG: <span>foo</span><span>bar</span> -> foo</span><span>bar
            Func<string, string> stripEmptySpans = null; stripEmptySpans = s =>
            {
                var span = Regex.Match(s, "^<span>(.*)</span>$", RegexOptions.Singleline);

                return span.Success ? stripEmptySpans(span.Groups[1].Value) : s;
            };
            Func<string, string> replaceWithHtmlEntities = s =>
            {
                return s.Replace("\u00a0", "&nbsp;");
            };

            var pattern = string.Format("{0}(.*){1}", ClipboardHelper.StartFragment, ClipboardHelper.EndFragment);
            var m = Regex.Match(html, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return m.Success
                ? replaceWithHtmlEntities(stripEmptySpans(m.Groups[1].Value.Trim()))
                : null;
        }

        private static void WikipediafyHyperlink(string url)
        {
            var m = Regex.Match(url, @"^https?://en.wikipedia.org/wiki/(.*)");

            if (!m.Success)
                throw new ArgumentException("Hyperlink was not a proper English wikipedia link.", "url");

            var href = string.Format("<a href='{0}'>WP: {1}</a>",
                url, // no System.Net.WebUtility.UrlEncode?
                //excluding System.Net.WebUtility.HtmlEncode(
                System.Net.WebUtility.UrlDecode(
                    m.Groups[1].Value.Replace("_", " ")).Replace("#", " § "));
                //.Replace("&#167;", "§")

            ClipboardHelper.CopyToClipboard(href, href);
        }
    }
}
