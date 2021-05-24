using Hugin.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Shared
{
    public abstract class LoginedLayoutBase : LayoutComponentBase
    {
        protected Data.User LoginUser { get; set; }

        [Inject]
        protected AuthenticationStateProvider Auth { get; set; }
        [Inject]
        protected UserHandleService UserHandler { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var auth = await Auth.GetAuthenticationStateAsync();
            LoginUser = UserHandler.Set.Where(x => x.Account == auth.User.Identity.Name)
                .Include(x => x.Lectures)
                .Include(x => x.LectureUserRelationships).ThenInclude(x => x.Lecture)
                .AsNoTracking().FirstOrDefault();
        }
    }
}
