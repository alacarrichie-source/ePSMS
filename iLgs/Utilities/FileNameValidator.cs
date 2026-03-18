using iLgs.Exceptions;
using System.IO;

namespace iLgs.Utilities
{
    public static class FileNameValidator
    {
        private static readonly char[] _blockedChars = { '#' };

        public static void Validate(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new InvalidValueException("Filename cannot be empty.");

            string name = Path.GetFileName(fileName);

            // Windows invalid characters
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidValueException($"Filename '{name}' contains invalid characters.");

            // Custom blocked characters
            if (name.IndexOfAny(_blockedChars) >= 0)
                throw new InvalidValueException($"Filename '{name}' cannot contain '#'.");
        }
    }
}