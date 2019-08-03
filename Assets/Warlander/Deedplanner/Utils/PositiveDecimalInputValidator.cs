using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Warlander.Deedplanner.Utils
{
    [CreateAssetMenu(fileName = "Positive Decimal Input Validator", menuName = "TextMeshPro/Positive Decimal Input Validator", order = 51)]
    public class PositiveDecimalInputValidator : TMP_InputValidator
    {
        public override char Validate(ref string text, ref int pos, char ch)
        {
            bool validChar = char.IsDigit(ch) || ch == '.';
            if (validChar)
            {
                string newText = text.Insert(pos, ch.ToString());
                bool validString = newText.Count(c => c == '.') <= 1;
                bool startsWithZero = newText.StartsWith("0");
                bool endsWithZero = newText.EndsWith("0");
                newText = newText.Trim('0');
                if (startsWithZero)
                {
                    newText = "0" + newText;
                }
                
                if (endsWithZero)
                {
                    newText += "0";
                }

                if (validString)
                {
                    text = newText;
                    pos++;
                }

                return validString ? ch : '\0';
            }
            
            return '\0';
        }
    }
}