﻿using System.Text.RegularExpressions;
using Amry.Gst.Properties;

namespace Amry.Gst
{
    public static class GstInputValidator
    {
        static readonly Regex GstNumberRegex = new Regex(@"^\d{12}$", RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled);
        static readonly Regex BusinessRegNumberRegex = new Regex(@"^[A-Za-z0-9\-]+$", RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled);

        public static void ValidateInput(GstLookupInputType inputType, string input)
        {
            if (inputType == GstLookupInputType.GstNumber && !GstNumberRegex.IsMatch(input)) {
                throw new InvalidGstInputException(Resources.InvalidGstNumberValidationMessage);
            }

            if (inputType == GstLookupInputType.BusinessRegNumber && !BusinessRegNumberRegex.IsMatch(input)) {
                throw new InvalidGstInputException(Resources.InvalidBusinessRegNumberValidationMessage);
            }

            if (inputType == GstLookupInputType.BusinessName && input.Length <= 3) {
                throw new InvalidGstInputException(Resources.BusinessNameTooShortValidationMessage);
            }
        }
    }
}