using System;

namespace SWGOHMessage
{
    /// <summary>
    /// Class to essentally output messages to the user
    /// </summary>
    public static class SWGOHMessageSystem
    {
        public static void OutputMessage(string message)
        {
            Console.WriteLine(message);
        }

        public static string InputMessage(string message)
        {
            Console.WriteLine(message);
            return Console.ReadLine();
        }
    }
}
