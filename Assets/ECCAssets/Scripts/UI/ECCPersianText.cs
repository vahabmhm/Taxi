using System.Collections.Generic;
using System.Text;

namespace EndlessCarChase
{
    /// <summary>
    /// Prepares Persian text for Unity's legacy UI Text, which can't join Arabic-script letters or render right-to-left.
    /// Each letter is replaced with its joined presentation form ( isolated / initial / medial / final ),
    /// and the line is reversed while keeping numbers and latin words in their original order.
    /// Usage: myText.text = ECCPersianText.Fix("امتیاز: 120");
    /// </summary>
    public static class ECCPersianText
    {
        // Presentation forms of a letter. Right-joining letters ( like ا د ر و ) only have isolated and final forms
        struct LetterForms
        {
            public char isolated, final, initial, medial;
            public bool joinsNext, joinsPrevious;

            public LetterForms(int isolated, int final, int initial = 0, int medial = 0)
            {
                this.isolated = (char)isolated;
                this.final = (char)final;
                this.initial = (char)initial;
                this.medial = (char)medial;
                joinsNext = initial != 0;
                joinsPrevious = isolated != final;
            }
        }

        const char ZeroWidthNonJoiner = '‌';
        const char Lam = 'ل';

        static readonly Dictionary<char, LetterForms> letters = new Dictionary<char, LetterForms>
        {
            { 'ء', new LetterForms(0xFE80, 0xFE80) },                 // ء
            { 'آ', new LetterForms(0xFE81, 0xFE82) },                 // آ
            { 'أ', new LetterForms(0xFE83, 0xFE84) },                 // أ
            { 'ؤ', new LetterForms(0xFE85, 0xFE86) },                 // ؤ
            { 'إ', new LetterForms(0xFE87, 0xFE88) },                 // إ
            { 'ئ', new LetterForms(0xFE89, 0xFE8A, 0xFE8B, 0xFE8C) }, // ئ
            { 'ا', new LetterForms(0xFE8D, 0xFE8E) },                 // ا
            { 'ب', new LetterForms(0xFE8F, 0xFE90, 0xFE91, 0xFE92) }, // ب
            { 'ة', new LetterForms(0xFE93, 0xFE94) },                 // ة
            { 'ت', new LetterForms(0xFE95, 0xFE96, 0xFE97, 0xFE98) }, // ت
            { 'ث', new LetterForms(0xFE99, 0xFE9A, 0xFE9B, 0xFE9C) }, // ث
            { 'ج', new LetterForms(0xFE9D, 0xFE9E, 0xFE9F, 0xFEA0) }, // ج
            { 'ح', new LetterForms(0xFEA1, 0xFEA2, 0xFEA3, 0xFEA4) }, // ح
            { 'خ', new LetterForms(0xFEA5, 0xFEA6, 0xFEA7, 0xFEA8) }, // خ
            { 'د', new LetterForms(0xFEA9, 0xFEAA) },                 // د
            { 'ذ', new LetterForms(0xFEAB, 0xFEAC) },                 // ذ
            { 'ر', new LetterForms(0xFEAD, 0xFEAE) },                 // ر
            { 'ز', new LetterForms(0xFEAF, 0xFEB0) },                 // ز
            { 'س', new LetterForms(0xFEB1, 0xFEB2, 0xFEB3, 0xFEB4) }, // س
            { 'ش', new LetterForms(0xFEB5, 0xFEB6, 0xFEB7, 0xFEB8) }, // ش
            { 'ص', new LetterForms(0xFEB9, 0xFEBA, 0xFEBB, 0xFEBC) }, // ص
            { 'ض', new LetterForms(0xFEBD, 0xFEBE, 0xFEBF, 0xFEC0) }, // ض
            { 'ط', new LetterForms(0xFEC1, 0xFEC2, 0xFEC3, 0xFEC4) }, // ط
            { 'ظ', new LetterForms(0xFEC5, 0xFEC6, 0xFEC7, 0xFEC8) }, // ظ
            { 'ع', new LetterForms(0xFEC9, 0xFECA, 0xFECB, 0xFECC) }, // ع
            { 'غ', new LetterForms(0xFECD, 0xFECE, 0xFECF, 0xFED0) }, // غ
            { 'ف', new LetterForms(0xFED1, 0xFED2, 0xFED3, 0xFED4) }, // ف
            { 'ق', new LetterForms(0xFED5, 0xFED6, 0xFED7, 0xFED8) }, // ق
            { 'ك', new LetterForms(0xFED9, 0xFEDA, 0xFEDB, 0xFEDC) }, // ك
            { 'ل', new LetterForms(0xFEDD, 0xFEDE, 0xFEDF, 0xFEE0) }, // ل
            { 'م', new LetterForms(0xFEE1, 0xFEE2, 0xFEE3, 0xFEE4) }, // م
            { 'ن', new LetterForms(0xFEE5, 0xFEE6, 0xFEE7, 0xFEE8) }, // ن
            { 'ه', new LetterForms(0xFEE9, 0xFEEA, 0xFEEB, 0xFEEC) }, // ه
            { 'و', new LetterForms(0xFEED, 0xFEEE) },                 // و
            { 'ى', new LetterForms(0xFEEF, 0xFEF0) },                 // ى
            { 'ي', new LetterForms(0xFEF1, 0xFEF2, 0xFEF3, 0xFEF4) }, // ي
            { 'پ', new LetterForms(0xFB56, 0xFB57, 0xFB58, 0xFB59) }, // پ
            { 'چ', new LetterForms(0xFB7A, 0xFB7B, 0xFB7C, 0xFB7D) }, // چ
            { 'ژ', new LetterForms(0xFB8A, 0xFB8B) },                 // ژ
            { 'ک', new LetterForms(0xFB8E, 0xFB8F, 0xFB90, 0xFB91) }, // ک
            { 'گ', new LetterForms(0xFB92, 0xFB93, 0xFB94, 0xFB95) }, // گ
            { 'ی', new LetterForms(0xFBFC, 0xFBFD, 0xFBFE, 0xFBFF) }, // ی
        };

        // Lam + Alef ligatures: alef variant -> ( isolated, final )
        static readonly Dictionary<char, LetterForms> lamAlef = new Dictionary<char, LetterForms>
        {
            { 'ا', new LetterForms(0xFEFB, 0xFEFC) }, // لا
            { 'آ', new LetterForms(0xFEF5, 0xFEF6) }, // لآ
            { 'أ', new LetterForms(0xFEF7, 0xFEF8) }, // لأ
            { 'إ', new LetterForms(0xFEF9, 0xFEFA) }, // لإ
        };

        /// <summary>
        /// Returns the text ready to be shown in a legacy UI Text. Each line is processed separately
        /// </summary>
        public static string Fix(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string[] lines = text.Split('\n');

            for (int index = 0; index < lines.Length; index++) lines[index] = Reorder(Shape(lines[index]));

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Breaks a sentence into lines of at most maxChars characters ( at spaces ), before it's shaped with Fix.
        /// Unity's own wrapping would break the reordered text in the wrong places
        /// </summary>
        public static string Wrap(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxChars) return text;

            StringBuilder result = new StringBuilder(text.Length + 8);

            int lineLength = 0;

            foreach (string word in text.Split(' '))
            {
                if (lineLength > 0 && lineLength + 1 + word.Length > maxChars)
                {
                    result.Append('\n');
                    lineLength = 0;
                }
                else if (lineLength > 0)
                {
                    result.Append(' ');
                    lineLength++;
                }

                result.Append(word);
                lineLength += word.Length;
            }

            return result.ToString();
        }

        /// <summary>
        /// Replaces each Persian letter with the presentation form that matches its neighbours
        /// </summary>
        static string Shape(string line)
        {
            StringBuilder result = new StringBuilder(line.Length);

            for (int index = 0; index < line.Length; index++)
            {
                char current = line[index];

                LetterForms forms;

                if (letters.TryGetValue(current, out forms) == false)
                {
                    // The zero width non-joiner only breaks joining, it isn't drawn
                    if (current != ZeroWidthNonJoiner) result.Append(current);

                    continue;
                }

                bool joinsPrevious = forms.joinsPrevious && index > 0 && JoinsNext(line[index - 1]);

                // Lam followed by alef becomes a single ligature
                LetterForms ligature;

                if (current == Lam && index + 1 < line.Length && lamAlef.TryGetValue(line[index + 1], out ligature))
                {
                    result.Append(joinsPrevious ? ligature.final : ligature.isolated);

                    index++;

                    continue;
                }

                bool joinsNext = forms.joinsNext && index + 1 < line.Length && JoinsPrevious(line[index + 1]);

                if (joinsPrevious && joinsNext) result.Append(forms.medial);
                else if (joinsPrevious) result.Append(forms.final);
                else if (joinsNext) result.Append(forms.initial);
                else result.Append(forms.isolated);
            }

            return result.ToString();
        }

        static bool JoinsPrevious(char character)
        {
            LetterForms forms;

            return letters.TryGetValue(character, out forms) && forms.joinsPrevious;
        }

        static bool JoinsNext(char character)
        {
            LetterForms forms;

            return letters.TryGetValue(character, out forms) && forms.joinsNext;
        }

        /// <summary>
        /// Reverses the line for right-to-left display, while keeping runs of numbers and latin letters left-to-right
        /// </summary>
        static string Reorder(string line)
        {
            List<string> runs = new List<string>();

            int index = 0;

            while (index < line.Length)
            {
                int start = index;

                if (IsLeftToRight(line[index]))
                {
                    // A left-to-right run may contain inner separators like "1,000" or "x2.5", but doesn't end with one
                    while (index < line.Length && (IsLeftToRight(line[index]) || (IsRunSeparator(line[index]) && index + 1 < line.Length && IsLeftToRight(line[index + 1])))) index++;

                    runs.Add(line.Substring(start, index - start));
                }
                else
                {
                    runs.Add(MirrorBracket(line[index]).ToString());

                    index++;
                }
            }

            runs.Reverse();

            return string.Concat(runs.ToArray());
        }

        static bool IsLeftToRight(char character)
        {
            return (character >= '0' && character <= '9') || (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z') || (character >= '۰' && character <= '۹');
        }

        static bool IsRunSeparator(char character)
        {
            return character == '.' || character == ',' || character == ':' || character == '/';
        }

        static char MirrorBracket(char character)
        {
            switch (character)
            {
                case '(': return ')';
                case ')': return '(';
                case '[': return ']';
                case ']': return '[';
                case '<': return '>';
                case '>': return '<';
                default: return character;
            }
        }
    }
}
