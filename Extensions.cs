using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.Resources;

namespace FEApp
{
	public static class Extensions
	{
		/// <summary>
		/// Convert the current object to Boolean.
		/// </summary>
		/// <param name="obj">The object to convert [Must be really a boolean].</param>
		/// <returns>Returns a boolean.</returns>
		public static bool ToBoolean(this object obj)
        {
			return (bool)obj;
        }

		/// <summary>
		/// Replace null by the wanted value.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array">The array to analyze.</param>
		/// <param name="replace">The value that will replace null.</param>
		/// <returns></returns>
		public static T[] ReplaceNullBy<T>(this T[] array, T replace, int defaultLength = 3)
		{
			if (array == null)
			{
				array = new T[defaultLength];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = replace;
				}
				return array;
			}
			else
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i] == null)
					{
						array[i] = replace;
					}
					else
					{
						array[i] = array[i];
					}
				}
				return array;
			}
		}

		public static string ReplaceBooleanBy(this bool boolean, string trueReplacement, string falseReplacement)
		{
			if (boolean)
			{
				return trueReplacement;
			}
			else
			{
				return falseReplacement;
			}
		}

		/// <summary>
		/// Get a string localized by it's key.
		/// </summary>
		/// <param name="strToLocalize">The key of the string to localize.</param>
		/// <returns>Returns the localized string.</returns>
		public static string GetLocalized(this string strToLocalize)
		{
			ResourceLoader rl = new ResourceLoader();
			string localized = rl.GetString(strToLocalize);
			if (!string.IsNullOrEmpty(localized))
			{
				return localized;
			}
			else
			{
				return strToLocalize;
			}
		}

		/// <summary>
		/// Sanitize a path.
		/// </summary>
		/// <param name="fileName">The path to sanitize.</param>
		/// <returns>Returns sanitized string (No special characters or unauthorized).</returns>
		public static string Sanitize(this string fileName, bool isArchiveEntry = false)
		{
			// Remove spaces in archive entry
			string result = new string(fileName.Where(c => !char.IsControl(c)).ToArray());
			if (isArchiveEntry)
			{
				string pattern = @"\s*/\s*";
				string replacement = "/";
				Regex rgx = new Regex(pattern);
				result = rgx.Replace(result, replacement);
			}

			foreach (string character in new List<string> { ":", "?", "<", ">", "|", "\"", "*" })
			{
				result = result.Replace(Convert.ToChar(character), Convert.ToChar("_"));
			}
			return result.Trim();
		}

		/// <summary>
		/// Capitilize the first letter of the provided string.
		/// </summary>
		/// <param name="str">The string to capitalize.</param>
		/// <returns>Returns the provded string with first letter uppercased.</returns>
		public static string CapitalizeFirstLetter(this string str)
		{
			return char.ToUpper(str[0]) + str.Substring(1);
		}
	}
}
