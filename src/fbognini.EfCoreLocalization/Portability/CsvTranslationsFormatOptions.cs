using System.Text;

namespace fbognini.EfCoreLocalization.Portability;

public class CsvTranslationsFormatOptions
{
    public char Separator { get; set; } = ';';

    /// <summary>
    /// Writes a leading "sep=" line. Excel honours it regardless of the machine locale, so the file opens already split into columns.
    /// </summary>
    public bool WriteSeparatorHint { get; set; } = true;

    /// <summary>
    /// UTF-8 with BOM by default: without the BOM Excel on Windows falls back to the ANSI codepage and mangles accented characters.
    /// </summary>
    public Encoding Encoding { get; set; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
}
