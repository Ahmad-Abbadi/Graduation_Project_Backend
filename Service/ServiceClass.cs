using System;
using System.Security.Cryptography;
namespace Graduation_Project_Backend.Service {
    public sealed class ServiceClass
    {
        private static readonly Lazy<ServiceClass> _instance =
            new Lazy<ServiceClass>(() => new ServiceClass());

        public static ServiceClass Instance => _instance.Value;

        // Private constructor
        private ServiceClass()
        {
        }

        public string GenerateSerialNumber()
        {
            int length = 8;

            Span<byte> bytes = stackalloc byte[length];
            RandomNumberGenerator.Fill(bytes);

            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = (char)('0' + (bytes[i] % 10));
            }

            return new string(result);
        }
    }

}



