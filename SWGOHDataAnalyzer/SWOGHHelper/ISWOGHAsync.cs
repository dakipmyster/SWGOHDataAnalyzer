using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SWOGHHelper
{
    public interface ISWGOHAsync
    {
        Task InvokeAsyncTask(string methodToInvoke, string classToInvoke = null);
    }
}
