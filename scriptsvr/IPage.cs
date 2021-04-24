using System;
using System.Net;
using System.Threading.Tasks;

namespace metascript
{
    interface IPage
    {
        Task HandleRequestAsync(HttpState state);
    }
}
