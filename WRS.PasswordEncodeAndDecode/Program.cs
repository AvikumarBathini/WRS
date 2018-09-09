using System;

namespace WRS.PasswordEncodeAndDecode
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Please Choose Encode or Decode,input 1:Encode 2:Decode:");
            var type = Console.ReadLine();
            if (type == "1")
            {
                Console.Write("Please input Encode text:");
                var encodeText = Console.ReadLine();
                if (!string.IsNullOrEmpty(encodeText))
                {
                    var inputBytes = System.Text.Encoding.UTF8.GetBytes(encodeText);
                    var afterEncodeStr = Convert.ToBase64String(inputBytes);
                    Console.Write("after Encode text: "+ afterEncodeStr);
                    Console.Read();
                }
            }
            else if (type == "2")
            {
                Console.Write("Please input Decode text:");
                var decodeText = Console.ReadLine();
                if (!string.IsNullOrEmpty(decodeText))
                {
                    var getBase64Str = Convert.FromBase64String(decodeText);
                    var getPassword = System.Text.Encoding.UTF8.GetString(getBase64Str);
                    Console.Write("after decode text: " + getPassword);
                    Console.Read();
                }
            }
        }
    }
}
