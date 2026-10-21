namespace UShort.Utils
{
    public static class Base58
    {
        private const string _alphabet = "123456789abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";

        public static int Decode(string s)
        {
            if (string.IsNullOrEmpty(s)) throw new NullReferenceException();

            int decoded = 0;

            for (int i = 0; i < s.Length; i++)
            {
                int alphabetPosition = _alphabet.IndexOf(s[i]);
                if (alphabetPosition < 0)
                    throw new ArgumentException(
                        $"Can't find \"{s[i]}\" in the alphabet: \"{_alphabet}\"");

                int powerOf = s.Length - i - 1;
                decoded += alphabetPosition * (int)Math.Pow(_alphabet.Length, powerOf);
            }

            return decoded;
        }
    }
}
