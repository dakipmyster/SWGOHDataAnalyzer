using System;

namespace SWGOHMessage
{
    /// <summary>
    /// Class to essentally output messages to the user
    /// </summary>
    public static class SWGOHMessageSystem
    {
        /// <summary>
        /// Method used to output a message to the console user
        /// </summary>
        /// <param name="message">Text to display</param>
        public static void OutputMessage(string message)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Method used to prompt the console user for input
        /// </summary>
        /// <param name="message">Text to display</param>
        /// <returns>User inputed text</returns>
        public static string InputMessage(string message)
        {
            Console.WriteLine(message);
            return Console.ReadLine();
        }
    }
}
