namespace Graduation_Project_Backend.Service.Common
{
    public sealed class PhoneNumberService : IPhoneNumberService
    {
        public string Normalize(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number cannot be null or empty.", nameof(phoneNumber));

            string cleaned = phoneNumber.Trim()
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("(", string.Empty)
                .Replace(")", string.Empty)
                .Replace(".", string.Empty);

            bool hasPlus = cleaned.StartsWith('+');

            if (hasPlus)
                cleaned = cleaned[1..];

            if (!cleaned.All(char.IsDigit))
                throw new ArgumentException("Phone number contains invalid characters.", nameof(phoneNumber));

            if (string.IsNullOrEmpty(cleaned))
                throw new ArgumentException("Phone number must contain digits.", nameof(phoneNumber));

            if (hasPlus)
            {
                if (!cleaned.StartsWith("962"))
                    throw new ArgumentException("Only Jordanian phone numbers are accepted. Expected format: +9627XXXXXXXX", nameof(phoneNumber));

                if (cleaned.Length != 12)
                    throw new ArgumentException($"Invalid Jordanian phone number length. Expected 12 digits (9627XXXXXXXX), got {cleaned.Length}.", nameof(phoneNumber));

                if (cleaned[3] != '7')
                    throw new ArgumentException("Invalid Jordanian mobile number. Expected format: +9627XXXXXXXX", nameof(phoneNumber));

                return "+" + cleaned;
            }

            if (cleaned.StartsWith("07"))
            {
                if (cleaned.Length != 10)
                    throw new ArgumentException($"Invalid Jordanian mobile number. Expected 10 digits (07XXXXXXXX), got {cleaned.Length}.", nameof(phoneNumber));

                return "+962" + cleaned[1..];
            }

            if (cleaned.StartsWith("962"))
            {
                if (cleaned.Length != 12)
                    throw new ArgumentException($"Invalid phone number. Expected 12 digits (9627XXXXXXXX), got {cleaned.Length}.", nameof(phoneNumber));

                if (cleaned[3] != '7')
                    throw new ArgumentException("Invalid Jordanian mobile number. Expected format: 9627XXXXXXXX", nameof(phoneNumber));

                return "+" + cleaned;
            }

            throw new ArgumentException("Invalid phone number format. Expected Jordanian format: 07XXXXXXXX or +9627XXXXXXXX", nameof(phoneNumber));
        }
    }
}
