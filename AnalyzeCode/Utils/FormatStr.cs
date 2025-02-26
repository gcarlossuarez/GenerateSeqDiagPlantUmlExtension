using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyzeCode.Utils
{
    public class FormaterStr
    {

        /// <summary>
        /// Remove comments and delete Enter and Carriage Return characters from a string.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="allocatingOnStack"></param>
        /// <param name="truncanteLongLine"></param>
        /// <returns></returns>
        public static string FormatStr(string str, bool allocatingOnStack = false, bool truncanteLongLine = false)
        {
            string strFormatted = DeleteEnterAndCarriageReturnCharacters(Utils.CommentRemover.RemoveComments(str), allocatingOnStack);
            strFormatted = strFormatted.Replace(":", " ");
            if (truncanteLongLine)
            {
                strFormatted = TruncateLongLine(strFormatted);
            }

            return strFormatted;
        }

        private static string TruncateLongLine(string str)
        {
            int lineLimit = 400;
            string final = "...\\n";
            if (str.Length > lineLimit)
            {
                str = str.Substring(0, lineLimit - final.Length) + final;
            }

            return str;
        }

        /// <summary>
        /// Delete Enter and Carriage Return characters from a string.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="allocatingOnStack"></param>
        /// <returns></returns>
        public static string DeleteEnterAndCarriageReturnCharacters(string str, bool allocatingOnStack = false)
        {
            const int NULL_POSITION = -1;
            int position = NULL_POSITION;
            List<Tuple<int, int>> listPositions = new List<Tuple<int, int>>();
            if (allocatingOnStack)
            {
                position = str.IndexOf("//", StringComparison.Ordinal);
                char[] arr = str.ToCharArray();
                while (NULL_POSITION != position && position < arr.Length)
                {
                    int i = position;
                    while (i < arr.Length && arr[i] != '\n')
                    {
                        ++i;
                    }

                    if (i < str.Length)
                    {
                        listPositions.Add(Tuple.Create(position, i));
                        position = str.IndexOf("//", position + 1, StringComparison.Ordinal);
                    }
                    else
                    {
                        // The RETURN was not found
                        break;
                    }
                }

                for (int j = 0; j < arr.Length; ++j)
                {
                    if ((arr[j] == '\n' || arr[j] == '\r') && !listPositions.Exists(x => x.Item2 == j))
                    {
                        arr[j] = ' ';
                    }
                }

                str = string.Concat(arr);
            }
            else
            {
                RemoveCharacter('\r');
                RemoveCharacter('\n');
            }

            return str;

            void RemoveCharacter(char character)
            {
                position = str.IndexOf(character);
                while (NULL_POSITION != position)
                {
                    str = str.Substring(0, position) + " " + str.Substring(position + 1).Trim();

                    position = str.IndexOf(character);
                }
            }
        }
    }
}
