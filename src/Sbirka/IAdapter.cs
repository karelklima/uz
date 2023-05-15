using System;
using System.Collections.Generic;
using System.Text;
using UZ.PDF;
using System.Text.RegularExpressions;

namespace UZ.Sbirka
{
    interface IAdapter
    {
        string Typ { get; }

        int HlavniSekce { get; }

        Regex NazevRegex { get; }

        Regex UvodRegex { get; }

        Regex ZaverRegex { get; }

        Text PostInterpret(Text text);

    }
}
