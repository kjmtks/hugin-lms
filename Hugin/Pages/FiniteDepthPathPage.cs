using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hugin.Pages
{
    public class FiniteDepthPathPage : ComponentBase
    {
        public string Path { get; set; }
        [Parameter] public string Path1 { get; set; }
        [Parameter] public string Path2 { get; set; }
        [Parameter] public string Path3 { get; set; }
        [Parameter] public string Path4 { get; set; }
        [Parameter] public string Path5 { get; set; }
        [Parameter] public string Path6 { get; set; }
        [Parameter] public string Path7 { get; set; }
        [Parameter] public string Path8 { get; set; }
        [Parameter] public string Path9 { get; set; }
        [Parameter] public string Path10 { get; set; }
        [Parameter] public string Path11 { get; set; }
        [Parameter] public string Path12 { get; set; }
        [Parameter] public string Path13 { get; set; }
        [Parameter] public string Path14 { get; set; }
        [Parameter] public string Path15 { get; set; }
        [Parameter] public string Path16 { get; set; }
        [Parameter] public string Path17 { get; set; }
        [Parameter] public string Path18 { get; set; }
        [Parameter] public string Path19 { get; set; }
        [Parameter] public string Path20 { get; set; }
        [Parameter] public string Path21 { get; set; }
        [Parameter] public string Path22 { get; set; }
        [Parameter] public string Path23 { get; set; }
        [Parameter] public string Path24 { get; set; }
        [Parameter] public string Path25 { get; set; }
        [Parameter] public string Path26 { get; set; }
        [Parameter] public string Path27 { get; set; }
        [Parameter] public string Path28 { get; set; }


        protected override void OnInitialized()
        {
            var paths = new string[] {
                Path1, Path2, Path3, Path4, Path5, Path6, Path7, Path8, Path9, Path10, Path11, Path12, Path13, Path14, Path15, Path16,
                Path17, Path18, Path19, Path20, Path21, Path22, Path23, Path24, Path25, Path26, Path27, Path28
            }.Where(x => !string.IsNullOrEmpty(x));
            Path = string.Join("/", paths);
        }

    }
}
