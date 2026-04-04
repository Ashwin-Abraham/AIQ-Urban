using System;
using System.IO;
using AIQ.GH.Exports.Models;

namespace AIQ.GH.Exports.Helpers;
public static class FileHelpers
{
    public static bool TryGetFilePath(string outputFolder, string fileName, string extension, ref Logger log, out string filePath)
    {
        filePath = "";

        try
        {
            Directory.CreateDirectory(outputFolder);
            filePath = Path.Combine(outputFolder, $"{fileName}{extension}");
            return true;
        }
        catch (Exception ex)
        {
            log.AddError($"Cannot access output folder: {ex.Message}");
            return false;
        }
    }
}
