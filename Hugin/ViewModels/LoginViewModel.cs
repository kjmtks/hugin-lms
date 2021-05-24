using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Hugin.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Account is required.")]
        public string Account { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }

        public string ErrorMessage { get; set; }
    }
}
