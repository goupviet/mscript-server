using System;
using System.Net;
using System.Threading.Tasks;

namespace metascript
{
    /// <summary>
    /// Every page must take state and handle the request.
    /// </summary>
    interface IPage
    {
        Task HandleRequestAsync(HttpState state);
    }
}
