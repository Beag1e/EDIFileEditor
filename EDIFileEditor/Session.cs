using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Xml;
using System.Text.RegularExpressions;

namespace EDIFileEditor;
public partial class Session
{
    private string _ReplacementText = "9566876206";
    private string _InputFileDirectory = @"..\..\..\Input";
    private string _OutputFileDirectory = @"..\..\..\Output";
    private string _WithheldFileDirectory = @"..\..\..\Withheld";
    private string _ArchiveFileDirectory = @"..\..\..\Archive";

    public Session()
    {
        Workflow();
    }

    public void Workflow()
    {
        List<string> files = [.. Directory.GetFiles(_InputFileDirectory)];

        foreach (string file in files)
        {
            FileWorkflow(file);
        }
    }

    public void FileWorkflow(string file)
    {
        List<string> lines = [.. File.ReadLines(file)];
        bool withhold = false;
        string fileName = Path.GetFileName(file);

        Regex isaReg = ISA08Regex();
        Regex gsReg = GS03Regex();
        Regex n1Reg = N104Regex();

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];

            Match match = isaReg.Match(line);
            if (match.Success)
            {
                lines[i] = match.Groups[1].Value + _ReplacementText.PadRight(15) + match.Groups[2].Value;
            }

            match = gsReg.Match(line);
            if (match.Success)
            {
                lines[i] = match.Groups[1].Value + _ReplacementText + match.Groups[2].Value;
            }

            match = n1Reg.Match(line);
            if (match.Success)
            {
                string n104secondHalf = match.Groups[3].Value;

                switch (n104secondHalf)
                {
                    case "1036":
                        break;
                    case "1241":
                        lines[i] = match.Groups[1].Value + match.Groups[2].Value + "1534" + match.Groups[4].Value;
                        break;
                    case "1455":
                        lines[i] = match.Groups[1].Value + match.Groups[2].Value + "1189" + match.Groups[4].Value;
                        break;
                    default:
                        Debug.WriteLine($"File {fileName} has an unregistered" +
                            $"N104 value: {match.Groups[2].Value + match.Groups[3].Value}");
                        withhold = true;
                        break;
                }
            }
        }

        if (!withhold)
        {
            File.WriteAllLines(Path.Combine(_OutputFileDirectory, fileName), lines);
            File.Move(file, Path.Combine(_ArchiveFileDirectory, fileName));
            //File.Delete(file);
        }
        else
        {
            File.WriteAllLines(Path.Combine(_WithheldFileDirectory, fileName), lines);
            File.Move(file, Path.Combine(_ArchiveFileDirectory, fileName));
        }
    }

    [GeneratedRegex("^(ISA\\*.*\\*.*\\*.*\\*.*\\*.*\\*.*\\*.*\\*).*(\\*.*\\*.*\\*.*\\*.*\\*.*\\*.*\\*.*\\*.*>~$)")]
    private static partial Regex ISA08Regex();

    [GeneratedRegex("^(GS\\*.*\\*.*\\*).*(\\*.*\\*.*\\*.*\\*.*\\*.*~$)")]
    private static partial Regex GS03Regex();

    [GeneratedRegex("^(N1\\*ST\\*.*\\*.*\\*)(.{3})(.*)(~$)")]
    private static partial Regex N104Regex();
}