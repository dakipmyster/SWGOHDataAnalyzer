using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWGOHDataAnalyzerHelper
{
    public static class Helper
    {
        public static async Task RunTaskAsync(Task task)
        {
            if (task.IsCompleted)
                return;            

            await task;
        }
    }
}
